FEATURE_COLUMNS = [
    "HomeWinRatio",
    "HomeRecentWinRatio",
    "HomeRecentGoalsAvg",
    "HomeRecentConcededGoalsAvg",
    "HomeRecentSogAvg",
    "HomeRecentPpgAvg",
    "HomeRecentHitsAvg",
    "HomeRecentPimAvg",
    "HomeRecentBlockedShotsAvg",
    "HomeRecentTakeawaysAvg",
    "HomeRecentGiveawaysAvg",
    "HomeGoalsAvg",
    "HomeGoalsAvgAtHome",
    "HomeRecentGoalsAvgAtHome",
    "HomeConcededGoalsAvg",
    "HomeConcededGoalsAvgAtHome",
    "HomeRecentConcededGoalsAvgAtHome",
    "HomeHoursSinceLastGame",
    "AwayWinRatio",
    "AwayRecentWinRatio",
    "AwayRecentGoalsAvg",
    "AwayRecentConcededGoalsAvg",
    "AwayRecentSogAvg",
    "AwayRecentPpgAvg",
    "AwayRecentHitsAvg",
    "AwayRecentPimAvg",
    "AwayRecentBlockedShotsAvg",
    "AwayRecentTakeawaysAvg",
    "AwayRecentGiveawaysAvg",
    "AwayGoalsAvg",
    "AwayGoalsAvgAtAway",
    "AwayRecentGoalsAvgAtAway",
    "AwayConcededGoalsAvg",
    "AwayConcededGoalsAvgAtAway",
    "AwayRecentConcededGoalsAvgAtAway",
    "HomeRosterOffenseValue",
    "HomeRosterDefenseValue",
    "HomeRosterGoalieValue",
    "AwayRosterOffenseValue",
    "AwayRosterDefenseValue",
    "AwayRosterGoalieValue",
    "AwayHoursSinceLastGame",
    "HomeIsBackToBack",
    "AwayIsBackToBack",
    "RestAdvantage",
    "HomeRecentShotAttemptsAvg",
    "AwayRecentShotAttemptsAvg",
    "HomeGoalDiffAvg",
    "AwayGoalDiffAvg",
    "HomeRecentGoalDiffAvg",
    "AwayRecentGoalDiffAvg",
    "HomeStreak",
    "AwayStreak",
    "HomeWinRatioAtHome",
    "AwayWinRatioAtAway",
    "HeadToHeadWinRatio",
    "HomeSavePct",
    "AwaySavePct",
    "HomeRecentSavePct",
    "AwayRecentSavePct",
    "HomeSogAvg",
    "AwaySogAvg",
    "HomePpgAvg",
    "AwayPpgAvg",
    "HomeHitsAvg",
    "AwayHitsAvg",
    "HomePimAvg",
    "AwayPimAvg",
    "HomeBlockedShotsAvg",
    "AwayBlockedShotsAvg",
    "HomeTakeawaysAvg",
    "AwayTakeawaysAvg",
    "HomeGiveawaysAvg",
    "AwayGiveawaysAvg",
    "HomeFaceOffWinPctAvg",
    "AwayFaceOffWinPctAvg",
    "HomeRecentFaceOffWinPctAvg",
    "AwayRecentFaceOffWinPctAvg",
    "HomeOvertimeRatio",
    "AwayOvertimeRatio",
    "HomeRecentOvertimeRatio",
    "AwayRecentOvertimeRatio",
    "HomeRegulationWinRatio",
    "AwayRegulationWinRatio",
    "HomeRecentRegulationWinRatio",
    "AwayRecentRegulationWinRatio",
    "HomePpEfficiency",
    "AwayPpEfficiency",
    "HomeRecentPpEfficiency",
    "AwayRecentPpEfficiency",
    "HomePkEfficiency",
    "AwayPkEfficiency",
    "HomeRecentPkEfficiency",
    "AwayRecentPkEfficiency",
    "HomeGoalsPerGamePeriod1",
    "AwayGoalsPerGamePeriod1",
    "HomeGoalsPerGamePeriod2",
    "AwayGoalsPerGamePeriod2",
    "HomeGoalsPerGamePeriod3",
    "AwayGoalsPerGamePeriod3",
    "HomeRecentGoalsPerGamePeriod1",
    "AwayRecentGoalsPerGamePeriod1",
    "HomeRecentGoalsPerGamePeriod2",
    "AwayRecentGoalsPerGamePeriod2",
    "HomeRecentGoalsPerGamePeriod3",
    "AwayRecentGoalsPerGamePeriod3",
    "HomeOffensiveZoneFaceoffWinPct",
    "AwayOffensiveZoneFaceoffWinPct",
    "HomeRecentOffensiveZoneFaceoffWinPct",
    "AwayRecentOffensiveZoneFaceoffWinPct",
    "HomePenaltyDifferentialAvg",
    "AwayPenaltyDifferentialAvg",
    "HomeRecentPenaltyDifferentialAvg",
    "AwayRecentPenaltyDifferentialAvg",
    "HomeShootingPct",
    "AwayShootingPct",
    "HomeRecentShootingPct",
    "AwayRecentShootingPct",
    "HomeCorsiPct",
    "AwayCorsiPct",
    "HomeRecentCorsiPct",
    "AwayRecentCorsiPct",
    "HomeStrengthOfSchedule",
    "AwayStrengthOfSchedule",
    "HomeRecentStrengthOfSchedule",
    "AwayRecentStrengthOfSchedule",
]

_feature_cols_sql = ", ".join(f"gc.{col}" for col in FEATURE_COLUMNS)

TRAINING_DATA_QUERY = f"""
SELECT {_feature_cols_sql},
       gr.Winner, gr.SeasonStartYear
FROM GameCleaned gc
JOIN GameRaw gr ON gc.GameId = gr.Id
WHERE gr.HasBeenPlayed = 1
"""

UNPLAYED_GAMES_QUERY = f"""
SELECT {_feature_cols_sql},
       gr.Id AS GameId, gr.HomeTeamId, gr.AwayTeamId, gr.GameDateUTC
FROM GameCleaned gc
JOIN GameRaw gr ON gc.GameId = gr.Id
WHERE gr.HasBeenPlayed = 0
"""

CURRENT_SEASON_GAMES_QUERY = f"""
SELECT {_feature_cols_sql},
       gr.Id AS GameId, gr.HomeTeamId, gr.AwayTeamId, gr.GameDateUTC,
       gr.Winner, gr.HasBeenPlayed
FROM GameCleaned gc
JOIN GameRaw gr ON gc.GameId = gr.Id
WHERE gr.SeasonStartYear = (SELECT MAX(SeasonStartYear) FROM GameRaw)
"""

TEAM_NAMES_QUERY = """
SELECT st.TeamId, st.Name
FROM SeasonTeam st
INNER JOIN (
    SELECT TeamId, MAX(SeasonStartYear) AS MaxSeason
    FROM SeasonTeam
    GROUP BY TeamId
) latest ON st.TeamId = latest.TeamId AND st.SeasonStartYear = latest.MaxSeason
"""

UPSERT_GAME_ODDS = """
MERGE GameOdds AS target
USING (VALUES (%s, %s, %s, %s, %s, %s, %s))
    AS source (GameId, ModelId, RunDateUTC, HomeOdds, AwayOdds, LogLoss, Notes)
ON target.GameId = source.GameId
   AND target.ModelId = source.ModelId
   AND target.RunDateUTC = source.RunDateUTC
WHEN MATCHED THEN
    UPDATE SET HomeOdds = source.HomeOdds,
               AwayOdds = source.AwayOdds,
               LogLoss = source.LogLoss,
               Notes = source.Notes
WHEN NOT MATCHED THEN
    INSERT (GameId, ModelId, RunDateUTC, HomeOdds, AwayOdds, LogLoss, Notes)
    VALUES (source.GameId, source.ModelId, source.RunDateUTC,
            source.HomeOdds, source.AwayOdds, source.LogLoss, source.Notes);
"""
