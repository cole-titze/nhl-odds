"""Readable factor groups for explaining predictions (#155).

Every feature column belongs to one group, the first whose pattern matches its name. The explainer
measures each group's effect on the home team's win probability, and `detail` describes the game's
values for it in words. This is the one place to name features for people.

Importing this module fails if a column in FEATURE_COLUMNS matches no group, so a new feature
can't ship unexplained (the predictor image's smoke test imports it).
"""

import re
from collections.abc import Callable, Mapping
from dataclasses import dataclass

from ..db.queries import FEATURE_COLUMNS

Row = Mapping[str, float]


def _pct(v: float) -> str:
    # Shares are stored as fractions (0.52); allow for a 0-100 column too
    return f"{v * 100 if abs(v) <= 1.5 else v:.1f}%"


def _pts(v: float) -> str:
    """A difference of two shares, in percentage points, signed"""
    return f"{v * 100 if abs(v) <= 1.5 else v:+.1f} pts"


def _save(v: float) -> str:
    return f"{v:.3f}".removeprefix("0") if 0 < v < 1 else f"{v:.1f}"


def _rest(hours: float, back_to_back: float) -> str:
    if back_to_back >= 0.5:
        return "back-to-back"
    # The cleaner uses 100 hours when a team has no earlier game
    if hours >= 100:
        return "4+ days off"
    days = round(hours / 24)
    return f"{days} day{'s' if days != 1 else ''} since last game"


def _home_away(label: str, home: str, away: str, fmt: Callable[[float], str]) -> Callable[[Row], str]:
    return lambda r: f"{label}: home {fmt(r[home])}, away {fmt(r[away])}"


def _travel(r: Row) -> str:
    km = r["TravelKmAdvantage"]
    parts = []
    if abs(km) >= 50:
        parts.append(f"{'away' if km > 0 else 'home'} team travelled {abs(km):,.0f} km more since its last game")
    shift = r["AwayTimeZoneShift"]
    if shift:
        parts.append(f"away team {abs(shift):.0f} time zone{'s' if abs(shift) != 1 else ''} from home")
    if r["AwayTripLength"] > 1:
        parts.append(f"away road trip of {r['AwayTripLength']:.0f} games")
    return "; ".join(parts).capitalize() if parts else "Little travel either way"


def _xg(r: Row) -> str:
    long, short = r.get("Last82XgPct5v5Advantage"), r.get("Last10XgPct5v5Advantage")
    if long is None or short is None:
        return "Expected-goals share for and against"
    return f"Home 5v5 expected-goals share edge: {_pts(long)} over 82 games, {_pts(short)} over the last 10"


def _last_season(r: Row) -> str:
    return f"Last season's points % edge for home: {_pts(r['PrevPointsPctAdvantage'])}"


def _roster(r: Row) -> str:
    def team(side: str) -> str:
        return f"{side.lower()} {r[side + 'RosterOffenseValue']:.2f} / {r[side + 'RosterDefenseValue']:.2f}"

    return f"Skater ratings (offense / defense): {team('Home')}, {team('Away')}"


@dataclass(frozen=True)
class FactorGroup:
    name: str
    pattern: str
    detail: Callable[[Row], str]


# First match wins, so the narrower patterns come first
FACTOR_GROUPS: list[FactorGroup] = [
    FactorGroup(
        "Rest",
        r"HoursSinceLastGame|IsBackToBack|RestAdvantage|Games7d",
        lambda r: (
            f"Home {_rest(r['HomeHoursSinceLastGame'], r['HomeIsBackToBack'])},"
            f" away {_rest(r['AwayHoursSinceLastGame'], r['AwayIsBackToBack'])}"
        ),
    ),
    FactorGroup("Travel", r"TravelKm|Km7d|TripLength|TimeZoneShift|BodyClock", _travel),
    FactorGroup(
        "Goaltending",
        r"Goalie|SavePct|GoalsSavedAboveXg",
        _home_away("Save % this season", "HomeSavePct", "AwaySavePct", _save),
    ),
    FactorGroup("Expected goals", r"Xg|Fenwick", _xg),
    FactorGroup("Last season", r"^Prev|^Blend", _last_season),
    FactorGroup(
        "Special teams",
        r"Pp|Pk|Penalty|Pim",
        lambda r: (
            f"Power play / penalty kill: home {_pct(r['HomePpEfficiency'])} / {_pct(r['HomePkEfficiency'])},"
            f" away {_pct(r['AwayPpEfficiency'])} / {_pct(r['AwayPkEfficiency'])}"
        ),
    ),
    FactorGroup("Shot share", r"Corsi|ShotAttempts|Sog", _home_away("Corsi %", "HomeCorsiPct", "AwayCorsiPct", _pct)),
    FactorGroup(
        "Faceoffs",
        r"Face[Oo]ff",
        _home_away("Faceoff wins", "HomeFaceOffWinPctAvg", "AwayFaceOffWinPctAvg", _pct),
    ),
    FactorGroup(
        "Physical play",
        r"Hits|Blocked|Takeaways|Giveaways",
        _home_away("Hits per game", "HomeHitsAvg", "AwayHitsAvg", lambda v: f"{v:.1f}"),
    ),
    FactorGroup(
        "Strength of schedule",
        r"StrengthOfSchedule",
        _home_away("Opponents' strength", "HomeStrengthOfSchedule", "AwayStrengthOfSchedule", lambda v: f"{v:.3f}"),
    ),
    FactorGroup("Games played", r"GamesPlayed", lambda r: f"Fewest games played: {r['MinGamesPlayed']:.0f}"),
    FactorGroup(
        "Scoring",
        r"Goals|GoalDiff|ShootingPct",
        _home_away("Goal difference per game", "HomeGoalDiffAvg", "AwayGoalDiffAvg", lambda v: f"{v:+.2f}"),
    ),
    FactorGroup(
        "Results",
        r"WinRatio|Streak|Overtime|Regulation|HeadToHead",
        _home_away("Win % this season", "HomeWinRatio", "AwayWinRatio", _pct),
    ),
    FactorGroup("Elo rating", r"Elo", _home_away("Elo", "HomeElo", "AwayElo", lambda v: f"{v:.0f}")),
    FactorGroup("Roster", r"RosterOffense|RosterDefense", _roster),
]


def group_columns(columns: list[str]) -> dict[str, list[int]]:
    """Indexes of `columns` in each group (groups with no columns left out). Raises if a column fits no group."""
    by_group: dict[str, list[int]] = {}
    missing = []
    for i, col in enumerate(columns):
        group = next((g for g in FACTOR_GROUPS if re.search(g.pattern, col)), None)
        if group is None:
            missing.append(col)
        else:
            by_group.setdefault(group.name, []).append(i)
    if missing:
        raise ValueError(f"Feature columns with no factor group in explain/factors.py: {', '.join(missing)}")
    return by_group


FEATURE_GROUPS = group_columns(FEATURE_COLUMNS)
_DETAILS = {g.name: g.detail for g in FACTOR_GROUPS}


def describe(group: str, row: Row) -> str:
    """The game's values for a group, in words; empty if a column the text needs is missing (e.g. excluded)."""
    try:
        return _DETAILS[group](row)
    except KeyError:
        return ""
