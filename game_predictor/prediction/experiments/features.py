# ============================================================================
# EDIT THIS FILE to configure the features the predictor computes itself, and which features the models see.
# The win, spread and total models all share them. Columns from GameCleaned (the C# cleaner) are listed in
# db/queries.py; changing how those are computed needs a CleanAll.
#
# XG_WINDOWS         — game windows for the expected-goals features (one LastN…Advantage column per stat per window)
# XG_STATS           — which expected-goals stats to build (any of the names in features/expected_goals.py)
# ELO                — margin-of-victory Elo over regular season and playoff games:
#     k                  update size per game
#     home_advantage     Elo points added to the home team
#     season_regression  share of each rating pulled back to 1500 between seasons (0 = full carry-over)
#     overtime_weight    update multiplier for games decided in overtime or a shootout
# EXCLUDED_FEATURES  — any feature column to leave out of every model (e.g. to test a model without it)
# ============================================================================

XG_WINDOWS: tuple[int, ...] = (10, 82)  # a last-5 window did clearly worse (#107)

XG_STATS: tuple[str, ...] = (
    "FenwickPct5v5",
    "XgPct5v5",
    "XgAvg",
    "ConcededXgAvg",
    "GoalsSavedAboveXgAvg",
    "GoalsAboveXgAvg",
    "PpXgAvg",
    "PkConcededXgAvg",
)

ELO = {
    "k": 6.0,
    "home_advantage": 35.0,
    "season_regression": 1 / 3,
    "overtime_weight": 0.75,
}

EXCLUDED_FEATURES: list[str] = []
