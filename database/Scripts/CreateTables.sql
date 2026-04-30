CREATE TABLE "ClassificationModel"
(
    "Id" INTEGER NOT NULL,
    "ModelFile" TEXT NOT NULL,
    CONSTRAINT "PK_ClassificationModel" PRIMARY KEY("Id")
);

CREATE TABLE "Team"
(
    "Id" INTEGER NOT NULL,
    "Abbreviation" TEXT NOT NULL,
    "FranchiseId" INTEGER NOT NULL,
    "LeagueId" INTEGER NOT NULL,
    CONSTRAINT "PK_Team" PRIMARY KEY("Id")
);

CREATE TABLE "SeasonTeam"
(
    "TeamId" INTEGER NOT NULL,
    "Abbreviation" TEXT NOT NULL,
    "SeasonStartYear" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "CommonName" TEXT NOT NULL,
    "PlaceName" TEXT NOT NULL,
    "LogoUri" TEXT NOT NULL,
    "Division" TEXT NOT NULL,
    "Conference" TEXT NOT NULL,
    "DivisionAbbreviation" VARCHAR(5) NOT NULL,
    "ConferenceAbbreviation" VARCHAR(5) NOT NULL,
    FOREIGN KEY("TeamId") REFERENCES "Team"("Id"),
    CONSTRAINT "PK_SeasonTeam" PRIMARY KEY("TeamId", "SeasonStartYear")
);

CREATE TABLE "SeasonGameCount"
(
    "SeasonId" INTEGER NOT NULL,
    "GameCount" INTEGER NOT NULL,
    CONSTRAINT "PK_SeasonGameCount" PRIMARY KEY("SeasonId")
);

CREATE TABLE "GameRaw"
(
    "Id" INTEGER NOT NULL,
    "HomeTeamId" INTEGER NOT NULL,
    "AwayTeamId" INTEGER NOT NULL,
    "SeasonStartYear" INTEGER NOT NULL,
    "GameDateUTC" TIMESTAMP NOT NULL,
    "HomeGoals" INTEGER NOT NULL,
    "AwayGoals" INTEGER NOT NULL,
    "HomeSOG" INTEGER NOT NULL,
    "AwaySOG" INTEGER NOT NULL,
    "HomePPG" INTEGER NOT NULL,
    "AwayPPG" INTEGER NOT NULL,
    "HomePIM" INTEGER NOT NULL,
    "AwayPIM" INTEGER NOT NULL,
    "HomeFaceOffWinPercent" DOUBLE PRECISION NOT NULL,
    "AwayFaceOffWinPercent" DOUBLE PRECISION NOT NULL,
    "HomeBlockedShots" INTEGER NOT NULL,
    "AwayBlockedShots" INTEGER NOT NULL,
    "HomeHits" INTEGER NOT NULL,
    "AwayHits" INTEGER NOT NULL,
    "HomeTakeaways" INTEGER NOT NULL,
    "AwayTakeaways" INTEGER NOT NULL,
    "HomeGiveaways" INTEGER NOT NULL,
    "AwayGiveaways" INTEGER NOT NULL,
    "Winner" INTEGER NOT NULL,
    "EndPeriod" INTEGER NOT NULL,
    "HasBeenPlayed" BOOLEAN NOT NULL,
    "GameType" INTEGER NOT NULL DEFAULT 2,
    "GameSummary" TEXT NULL,
    "EventSummary" TEXT NULL,
    "PlayByPlaySummary" TEXT NULL,
    "FaceoffSummary" TEXT NULL,
    "FaceoffComparisonSummary" TEXT NULL,
    "RosterSummary" TEXT NULL,
    "ShotSummary" TEXT NULL,
    "ShiftChartSummary" TEXT NULL,
    "ToiAwaySummary" TEXT NULL,
    "ToiHomeSummary" TEXT NULL,
    "ThreeMinuteRecapVideoId" INTEGER NOT NULL,
    "CondensedGameVideoId" INTEGER NOT NULL,
    "VenueName" TEXT NOT NULL,
    "VenueLocation" TEXT NOT NULL,
    CONSTRAINT "PK_GameRaw" PRIMARY KEY("Id"),
    FOREIGN KEY ("HomeTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("AwayTeamId") REFERENCES "Team"("Id")
);

CREATE TABLE "GameOfficial"
(
    "GameId" INTEGER NOT NULL,
    "Name" VARCHAR(250) NOT NULL,
    "Role" INTEGER NOT NULL,
    CONSTRAINT "PK_GameOfficial" PRIMARY KEY("GameId", "Name"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "GameCleaned"
(
    "GameId" INTEGER NOT NULL,
    "HomeWinRatio" DOUBLE PRECISION NOT NULL,
    "HomeRecentWinRatio" DOUBLE PRECISION NOT NULL,
    "HomeRecentGoalsAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentConcededGoalsAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentSogAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentPpgAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentHitsAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentPimAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentBlockedShotsAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentTakeawaysAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentGiveawaysAvg" DOUBLE PRECISION NOT NULL,
    "HomeGoalsAvg" DOUBLE PRECISION NOT NULL,
    "HomeGoalsAvgAtHome" DOUBLE PRECISION NOT NULL,
    "HomeRecentGoalsAvgAtHome" DOUBLE PRECISION NOT NULL,
    "HomeConcededGoalsAvg" DOUBLE PRECISION NOT NULL,
    "HomeConcededGoalsAvgAtHome" DOUBLE PRECISION NOT NULL,
    "HomeRecentConcededGoalsAvgAtHome" DOUBLE PRECISION NOT NULL,
    "HomeHoursSinceLastGame" DOUBLE PRECISION NOT NULL,
    "AwayWinRatio" DOUBLE PRECISION NOT NULL,
    "AwayRecentWinRatio" DOUBLE PRECISION NOT NULL,
    "AwayRecentGoalsAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentConcededGoalsAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentSogAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentPpgAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentHitsAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentPimAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentBlockedShotsAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentTakeawaysAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentGiveawaysAvg" DOUBLE PRECISION NOT NULL,
    "AwayGoalsAvg" DOUBLE PRECISION NOT NULL,
    "AwayGoalsAvgAtAway" DOUBLE PRECISION NOT NULL,
    "AwayRecentGoalsAvgAtAway" DOUBLE PRECISION NOT NULL,
    "AwayConcededGoalsAvg" DOUBLE PRECISION NOT NULL,
    "AwayConcededGoalsAvgAtAway" DOUBLE PRECISION NOT NULL,
    "AwayRecentConcededGoalsAvgAtAway" DOUBLE PRECISION NOT NULL,
    "HomeRosterOffenseValue" DOUBLE PRECISION NOT NULL,
    "HomeRosterDefenseValue" DOUBLE PRECISION NOT NULL,
    "HomeRosterGoalieValue" DOUBLE PRECISION NOT NULL,
    "AwayRosterOffenseValue" DOUBLE PRECISION NOT NULL,
    "AwayRosterDefenseValue" DOUBLE PRECISION NOT NULL,
    "AwayRosterGoalieValue" DOUBLE PRECISION NOT NULL,
    "AwayHoursSinceLastGame" DOUBLE PRECISION NOT NULL,
    "HomeRecentRosterOffenseValue" DOUBLE PRECISION NOT NULL,
    "HomeRecentRosterDefenseValue" DOUBLE PRECISION NOT NULL,
    "HomeRecentRosterGoalieValue" DOUBLE PRECISION NOT NULL,
    "AwayRecentRosterOffenseValue" DOUBLE PRECISION NOT NULL,
    "AwayRecentRosterDefenseValue" DOUBLE PRECISION NOT NULL,
    "AwayRecentRosterGoalieValue" DOUBLE PRECISION NOT NULL,
    "HomeIsBackToBack" DOUBLE PRECISION NOT NULL,
    "AwayIsBackToBack" DOUBLE PRECISION NOT NULL,
    "RestAdvantage" DOUBLE PRECISION NOT NULL,
    "HomeRecentShotAttemptsAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentShotAttemptsAvg" DOUBLE PRECISION NOT NULL,
    "HomeGoalDiffAvg" DOUBLE PRECISION NOT NULL,
    "AwayGoalDiffAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentGoalDiffAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentGoalDiffAvg" DOUBLE PRECISION NOT NULL,
    "HomeStreak" DOUBLE PRECISION NOT NULL,
    "AwayStreak" DOUBLE PRECISION NOT NULL,
    "HomeWinRatioAtHome" DOUBLE PRECISION NOT NULL,
    "AwayWinRatioAtAway" DOUBLE PRECISION NOT NULL,
    "HeadToHeadWinRatio" DOUBLE PRECISION NOT NULL,
    "HomeSavePct" DOUBLE PRECISION NOT NULL,
    "AwaySavePct" DOUBLE PRECISION NOT NULL,
    "HomeRecentSavePct" DOUBLE PRECISION NOT NULL,
    "AwayRecentSavePct" DOUBLE PRECISION NOT NULL,
    "HomeSogAvg" DOUBLE PRECISION NOT NULL,
    "AwaySogAvg" DOUBLE PRECISION NOT NULL,
    "HomePpgAvg" DOUBLE PRECISION NOT NULL,
    "AwayPpgAvg" DOUBLE PRECISION NOT NULL,
    "HomeHitsAvg" DOUBLE PRECISION NOT NULL,
    "AwayHitsAvg" DOUBLE PRECISION NOT NULL,
    "HomePimAvg" DOUBLE PRECISION NOT NULL,
    "AwayPimAvg" DOUBLE PRECISION NOT NULL,
    "HomeBlockedShotsAvg" DOUBLE PRECISION NOT NULL,
    "AwayBlockedShotsAvg" DOUBLE PRECISION NOT NULL,
    "HomeTakeawaysAvg" DOUBLE PRECISION NOT NULL,
    "AwayTakeawaysAvg" DOUBLE PRECISION NOT NULL,
    "HomeGiveawaysAvg" DOUBLE PRECISION NOT NULL,
    "AwayGiveawaysAvg" DOUBLE PRECISION NOT NULL,
    "HomeFaceOffWinPctAvg" DOUBLE PRECISION NOT NULL,
    "AwayFaceOffWinPctAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentFaceOffWinPctAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentFaceOffWinPctAvg" DOUBLE PRECISION NOT NULL,
    "HomeOvertimeRatio" DOUBLE PRECISION NOT NULL,
    "AwayOvertimeRatio" DOUBLE PRECISION NOT NULL,
    "HomeRecentOvertimeRatio" DOUBLE PRECISION NOT NULL,
    "AwayRecentOvertimeRatio" DOUBLE PRECISION NOT NULL,
    "HomeRegulationWinRatio" DOUBLE PRECISION NOT NULL,
    "AwayRegulationWinRatio" DOUBLE PRECISION NOT NULL,
    "HomeRecentRegulationWinRatio" DOUBLE PRECISION NOT NULL,
    "AwayRecentRegulationWinRatio" DOUBLE PRECISION NOT NULL,
    "HomePpEfficiency" DOUBLE PRECISION NOT NULL,
    "AwayPpEfficiency" DOUBLE PRECISION NOT NULL,
    "HomeRecentPpEfficiency" DOUBLE PRECISION NOT NULL,
    "AwayRecentPpEfficiency" DOUBLE PRECISION NOT NULL,
    "HomePkEfficiency" DOUBLE PRECISION NOT NULL,
    "AwayPkEfficiency" DOUBLE PRECISION NOT NULL,
    "HomeRecentPkEfficiency" DOUBLE PRECISION NOT NULL,
    "AwayRecentPkEfficiency" DOUBLE PRECISION NOT NULL,
    "HomeGoalsPerGamePeriod1" DOUBLE PRECISION NOT NULL,
    "AwayGoalsPerGamePeriod1" DOUBLE PRECISION NOT NULL,
    "HomeGoalsPerGamePeriod2" DOUBLE PRECISION NOT NULL,
    "AwayGoalsPerGamePeriod2" DOUBLE PRECISION NOT NULL,
    "HomeGoalsPerGamePeriod3" DOUBLE PRECISION NOT NULL,
    "AwayGoalsPerGamePeriod3" DOUBLE PRECISION NOT NULL,
    "HomeRecentGoalsPerGamePeriod1" DOUBLE PRECISION NOT NULL,
    "AwayRecentGoalsPerGamePeriod1" DOUBLE PRECISION NOT NULL,
    "HomeRecentGoalsPerGamePeriod2" DOUBLE PRECISION NOT NULL,
    "AwayRecentGoalsPerGamePeriod2" DOUBLE PRECISION NOT NULL,
    "HomeRecentGoalsPerGamePeriod3" DOUBLE PRECISION NOT NULL,
    "AwayRecentGoalsPerGamePeriod3" DOUBLE PRECISION NOT NULL,
    "HomeOffensiveZoneFaceoffWinPct" DOUBLE PRECISION NOT NULL,
    "AwayOffensiveZoneFaceoffWinPct" DOUBLE PRECISION NOT NULL,
    "HomeRecentOffensiveZoneFaceoffWinPct" DOUBLE PRECISION NOT NULL,
    "AwayRecentOffensiveZoneFaceoffWinPct" DOUBLE PRECISION NOT NULL,
    "HomePenaltyDifferentialAvg" DOUBLE PRECISION NOT NULL,
    "AwayPenaltyDifferentialAvg" DOUBLE PRECISION NOT NULL,
    "HomeRecentPenaltyDifferentialAvg" DOUBLE PRECISION NOT NULL,
    "AwayRecentPenaltyDifferentialAvg" DOUBLE PRECISION NOT NULL,
    "HomeShootingPct" DOUBLE PRECISION NOT NULL,
    "AwayShootingPct" DOUBLE PRECISION NOT NULL,
    "HomeRecentShootingPct" DOUBLE PRECISION NOT NULL,
    "AwayRecentShootingPct" DOUBLE PRECISION NOT NULL,
    "HomeCorsiPct" DOUBLE PRECISION NOT NULL,
    "AwayCorsiPct" DOUBLE PRECISION NOT NULL,
    "HomeRecentCorsiPct" DOUBLE PRECISION NOT NULL,
    "AwayRecentCorsiPct" DOUBLE PRECISION NOT NULL,
    "HomeStrengthOfSchedule" DOUBLE PRECISION NOT NULL,
    "AwayStrengthOfSchedule" DOUBLE PRECISION NOT NULL,
    "HomeRecentStrengthOfSchedule" DOUBLE PRECISION NOT NULL,
    "AwayRecentStrengthOfSchedule" DOUBLE PRECISION NOT NULL,
    CONSTRAINT "PK_GameCleaned" PRIMARY KEY("GameId"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "GameOdds"
(
    "GameId" INTEGER NOT NULL,
    "ModelId" INTEGER NOT NULL,
    "RunDateUTC" TIMESTAMP NOT NULL,
    "HomeOdds" DOUBLE PRECISION NOT NULL,
    "AwayOdds" DOUBLE PRECISION NOT NULL,
    "LogLoss" DOUBLE PRECISION NOT NULL DEFAULT 0,
    "Notes" TEXT,
    CONSTRAINT "PK_GameOdds" PRIMARY KEY("GameId", "ModelId", "RunDateUTC"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "GameSpreadTotalOdds"
(
    "GameId" INTEGER NOT NULL,
    "ModelId" INTEGER NOT NULL,
    "RunDateUTC" TIMESTAMP NOT NULL,
    "PredictedValue" DOUBLE PRECISION NOT NULL,
    "ResidualStd" DOUBLE PRECISION NOT NULL,
    "Line" DOUBLE PRECISION NULL,
    "CoverProbability" DOUBLE PRECISION NULL,
    "Notes" TEXT,
    CONSTRAINT "PK_GameSpreadTotalOdds" PRIMARY KEY("GameId", "ModelId", "RunDateUTC"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "Player"
(
    "Id" INTEGER NOT NULL,
    "FirstName" TEXT NOT NULL,
    "LastName" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL,
    "CurrentTeamId" INTEGER NOT NULL,
    "HeadShot" TEXT NOT NULL,
    "HeroImage" TEXT NOT NULL,
    "HeightInInches" INTEGER NOT NULL,
    "WeightInPounds" INTEGER NOT NULL,
    "BirthDate" TIMESTAMP,
    "BirthCity" TEXT,
    "BirthStateProvince" TEXT,
    "BirthCountry" TEXT,
    "IsInTopOneHundredAllTime" BOOLEAN NOT NULL,
    "IsInHallOfFame" BOOLEAN NOT NULL,
    "ShopLink" TEXT,
    "TwitterLink" TEXT,
    "WatchLink" TEXT,
    "PlayerSlug" TEXT,
    CONSTRAINT "PK_Player" PRIMARY KEY("Id"),
    FOREIGN KEY ("CurrentTeamId") REFERENCES "Team"("Id")
);

CREATE TABLE "PlayerDraftDetails"
(
    "PlayerId" INTEGER NOT NULL,
    "Year" INTEGER NOT NULL,
    "TeamAbbrev" TEXT NOT NULL,
    "Round" INTEGER NOT NULL,
    "PickInRound" INTEGER NOT NULL,
    "OverallPick" INTEGER NOT NULL,
    CONSTRAINT "PK_PlayerDraftDetails" PRIMARY KEY("PlayerId"),
    FOREIGN KEY ("PlayerId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameSkaterStats"
(
    "GameId" INTEGER NOT NULL,
    "PlayerId" INTEGER NOT NULL,
    "TeamId" INTEGER NOT NULL,
    "Goals" INTEGER NOT NULL,
    "Assists" INTEGER NOT NULL,
    "PlusMinus" INTEGER NOT NULL,
    "PenaltyMinutes" INTEGER NOT NULL,
    "Hits" INTEGER NOT NULL,
    "PowerPlayGoals" INTEGER NOT NULL,
    "ShotsOnGoal" INTEGER NOT NULL,
    "FaceOffWinningPctg" DOUBLE PRECISION NOT NULL,
    "BlockedShots" INTEGER NOT NULL,
    "Giveaways" INTEGER NOT NULL,
    "Takeaways" INTEGER NOT NULL,
    "TimeOnIceSeconds" INTEGER NOT NULL,
    "Position" INTEGER NOT NULL,
    CONSTRAINT "PK_GameSkaterStats" PRIMARY KEY("GameId", "PlayerId"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("PlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("TeamId") REFERENCES "Team"("Id")
);

CREATE TABLE "GameGoalieStats"
(
    "GameId" INTEGER NOT NULL,
    "PlayerId" INTEGER NOT NULL,
    "TeamId" INTEGER NOT NULL,
    "EvenStrengthShotsSaved" INTEGER NOT NULL,
    "PowerPlayShotsSaved" INTEGER NOT NULL,
    "ShortHandedShotsSaved" INTEGER NOT NULL,
    "EvenStrengthGoalsAllowed" INTEGER NOT NULL,
    "PowerPlayGoalsAllowed" INTEGER NOT NULL,
    "ShortHandedGoalsAllowed" INTEGER NOT NULL,
    "TimeOnIceSeconds" INTEGER NOT NULL,
    "IsStarter" BOOLEAN NOT NULL,
    "Position" INTEGER NOT NULL,
    CONSTRAINT "PK_GameGoalieStats" PRIMARY KEY("GameId", "PlayerId"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("PlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("TeamId") REFERENCES "Team"("Id")
);

CREATE TABLE "TvBroadcaster"
(
    "Id" INTEGER NOT NULL,
    "NetworkName" TEXT NOT NULL,
    "MarketAbbreviation" TEXT NOT NULL,
    "SequenceNumber" INTEGER NOT NULL,
    "CountryCode" TEXT NOT NULL,
    CONSTRAINT "PK_TvBroadcaster" PRIMARY KEY("Id")
);

CREATE TABLE "GameTvBroadcaster"
(
    "GameId" INTEGER NOT NULL,
    "BroadcasterId" INTEGER NOT NULL,
    CONSTRAINT "PK_GameTvBroadcaster" PRIMARY KEY("GameId", "BroadcasterId"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("BroadcasterId") REFERENCES "TvBroadcaster"("Id")
);

CREATE TABLE "GameCoach"
(
    "GameId" INTEGER NOT NULL,
    "Name" VARCHAR(250) NOT NULL,
    "TeamId" INTEGER NOT NULL,
    CONSTRAINT "PK_GameCoach" PRIMARY KEY("GameId", "Name"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("TeamId") REFERENCES "Team"("Id")
);

-- Game Event Tables
CREATE TABLE "GameBlockedShotEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "BlockingPlayerTeamId" INTEGER NOT NULL,
    "BlockingPlayerId" INTEGER,
    "ShooterPlayerId" INTEGER,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    "BlockType" INTEGER NOT NULL,
    CONSTRAINT "PK_BlockedShotEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("BlockingPlayerTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("BlockingPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("ShooterPlayerId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameDelayedPenaltyEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "PenaltyTeamId" INTEGER NOT NULL,
    CONSTRAINT "PK_GameDelayedPenaltyEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("PenaltyTeamId") REFERENCES "Team"("Id")
);

CREATE TABLE "GameFailedShotAttemptEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "ShotType" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "ShootingTeamId" INTEGER NOT NULL,
    "ShootingPlayerId" INTEGER NOT NULL,
    "GoalieId" INTEGER,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    CONSTRAINT "PK_GameFailedShotAttemptEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("ShootingTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("ShootingPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("GoalieId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameFaceoffEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "WinningTeamId" INTEGER NOT NULL,
    "WinningPlayerId" INTEGER NOT NULL,
    "LosingPlayerId" INTEGER NOT NULL,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    CONSTRAINT "PK_GameFaceoffEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("WinningTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("WinningPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("LosingPlayerId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameGameEndEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    CONSTRAINT "PK_GameGameEndEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "GameGiveawayEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "GiveawayPlayerTeamId" INTEGER NOT NULL,
    "GiveawayPlayerId" INTEGER NOT NULL,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    CONSTRAINT "PK_GameGiveawayEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("GiveawayPlayerTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("GiveawayPlayerId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameGoalEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    "ShotType" INTEGER NOT NULL,
    "ScoringPlayerTeamId" INTEGER NOT NULL,
    "AssistOnePlayerId" INTEGER,
    "AssistTwoPlayerId" INTEGER,
    "ScoringPlayerId" INTEGER NOT NULL,
    "GoalieId" INTEGER,
    "HighlightClipSharingUrl" VARCHAR(255) NOT NULL,
    "HighlightClipId" BIGINT NOT NULL,
    "DiscreetClipId" BIGINT NOT NULL,
    "PptReplayUrl" VARCHAR(255) NOT NULL,
    CONSTRAINT "PK_GameGoalEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("ScoringPlayerTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("ScoringPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("AssistOnePlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("AssistTwoPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("GoalieId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameHitEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "HittingPlayerTeamId" INTEGER NOT NULL,
    "HittingPlayerId" INTEGER NOT NULL,
    "HitteePlayerId" INTEGER NOT NULL,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    CONSTRAINT "PK_GameHitEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("HittingPlayerTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("HittingPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("HitteePlayerId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameMissedShotEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "ShotType" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "ShootingTeamId" INTEGER NOT NULL,
    "ShootingPlayerId" INTEGER NOT NULL,
    "GoalieId" INTEGER,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    "MissType" INTEGER NOT NULL,
    CONSTRAINT "PK_GameMissedShotEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("ShootingTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("ShootingPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("GoalieId") REFERENCES "Player"("Id")
);

CREATE TABLE "GamePenaltyEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "CommittedByPlayerTeamId" INTEGER NOT NULL,
    "DrawnByPlayerId" INTEGER,
    "CommittedByPlayerId" INTEGER,
    "ServedByPlayerId" INTEGER,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    "Duration" INTEGER NOT NULL,
    "PenaltyType" INTEGER NOT NULL,
    "PenaltySeverity" INTEGER NOT NULL,
    CONSTRAINT "PK_GamePenaltyEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("CommittedByPlayerTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("DrawnByPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("CommittedByPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("ServedByPlayerId") REFERENCES "Player"("Id")
);

CREATE TABLE "GamePeriodStartEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    CONSTRAINT "PK_GamePeriodStartEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "GamePeriodEndEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    CONSTRAINT "PK_GamePeriodEndEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "GameShotEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "ShootingTeamId" INTEGER NOT NULL,
    "ShootingPlayerId" INTEGER NOT NULL,
    "GoalieId" INTEGER,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    "ShotType" INTEGER NOT NULL,
    CONSTRAINT "PK_ShotEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("ShootingTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("ShootingPlayerId") REFERENCES "Player"("Id"),
    FOREIGN KEY ("GoalieId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameStoppageEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "StoppageType" INTEGER NOT NULL,
    "StoppageDetails" INTEGER NOT NULL,
    CONSTRAINT "PK_GameStoppageEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "GameTakeawayEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    "TakeawayPlayerTeamId" INTEGER NOT NULL,
    "TakeawayPlayerId" INTEGER NOT NULL,
    "XCoordinate" INTEGER,
    "YCoordinate" INTEGER,
    "Zone" INTEGER NOT NULL,
    CONSTRAINT "PK_GameTakeawayEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id"),
    FOREIGN KEY ("TakeawayPlayerTeamId") REFERENCES "Team"("Id"),
    FOREIGN KEY ("TakeawayPlayerId") REFERENCES "Player"("Id")
);

CREATE TABLE "GameShootoutCompleteEvent"
(
    "Id" INTEGER NOT NULL,
    "GameId" INTEGER NOT NULL,
    "TypeCode" INTEGER NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "SituationCode" INTEGER NOT NULL,
    "PeriodNumber" INTEGER NOT NULL,
    "PeriodType" INTEGER NOT NULL,
    "EventTypeName" VARCHAR(100) NOT NULL,
    "HomeTeamDefendingSide" INTEGER NOT NULL,
    "SecondsIntoPeriod" INTEGER NOT NULL,
    "SecondsLeftInPeriod" INTEGER NOT NULL,
    CONSTRAINT "PK_GameShootoutEvent" PRIMARY KEY("GameId", "Id"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "BookmakerOddsResponse"
(
    "Id" SERIAL NOT NULL,
    "FetchedDateUTC" TIMESTAMP NOT NULL,
    "QueryDateUTC" TIMESTAMP NULL,
    "RawJson" TEXT NOT NULL,
    CONSTRAINT "PK_BookmakerOddsResponse" PRIMARY KEY("Id")
);

CREATE TABLE "BookmakerOdds"
(
    "GameId" INTEGER NOT NULL,
    "BookmakerName" VARCHAR(100) NOT NULL,
    "BookmakerKey" VARCHAR(100) NOT NULL DEFAULT '',
    "HomeOdds" DOUBLE PRECISION NOT NULL,
    "AwayOdds" DOUBLE PRECISION NOT NULL,
    "FetchedDateUTC" TIMESTAMP NOT NULL,
    "BookmakerLastUpdate" TIMESTAMP NOT NULL DEFAULT '0001-01-01',
    "MarketLastUpdate" TIMESTAMP NOT NULL DEFAULT '0001-01-01',
    "OddsApiGameId" VARCHAR(100) NOT NULL DEFAULT '',
    CONSTRAINT "PK_BookmakerOdds" PRIMARY KEY("GameId", "BookmakerName"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "BookmakerSpreads"
(
    "GameId" INTEGER NOT NULL,
    "BookmakerName" VARCHAR(100) NOT NULL,
    "BookmakerKey" VARCHAR(100) NOT NULL DEFAULT '',
    "HomePoint" DOUBLE PRECISION NOT NULL,
    "HomePrice" INTEGER NOT NULL,
    "AwayPoint" DOUBLE PRECISION NOT NULL,
    "AwayPrice" INTEGER NOT NULL,
    "FetchedDateUTC" TIMESTAMP NOT NULL,
    "BookmakerLastUpdate" TIMESTAMP NOT NULL DEFAULT '0001-01-01',
    "MarketLastUpdate" TIMESTAMP NOT NULL DEFAULT '0001-01-01',
    "OddsApiGameId" VARCHAR(100) NOT NULL DEFAULT '',
    CONSTRAINT "PK_BookmakerSpreads" PRIMARY KEY("GameId", "BookmakerName"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "BookmakerTotals"
(
    "GameId" INTEGER NOT NULL,
    "BookmakerName" VARCHAR(100) NOT NULL,
    "BookmakerKey" VARCHAR(100) NOT NULL DEFAULT '',
    "OverUnderPoint" DOUBLE PRECISION NOT NULL,
    "OverPrice" INTEGER NOT NULL,
    "UnderPrice" INTEGER NOT NULL,
    "FetchedDateUTC" TIMESTAMP NOT NULL,
    "BookmakerLastUpdate" TIMESTAMP NOT NULL DEFAULT '0001-01-01',
    "MarketLastUpdate" TIMESTAMP NOT NULL DEFAULT '0001-01-01',
    "OddsApiGameId" VARCHAR(100) NOT NULL DEFAULT '',
    CONSTRAINT "PK_BookmakerTotals" PRIMARY KEY("GameId", "BookmakerName"),
    FOREIGN KEY ("GameId") REFERENCES "GameRaw"("Id")
);

CREATE TABLE "JobStatus"
(
    "JobName" VARCHAR(100) NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'idle',
    "StartedAt" TIMESTAMP NULL,
    "FinishedAt" TIMESTAMP NULL,
    "Error" TEXT NULL,
    CONSTRAINT "PK_JobStatus" PRIMARY KEY("JobName")
);

CREATE TABLE "ErrorLog"
(
    "Id" SERIAL NOT NULL,
    "TimestampUTC" TIMESTAMP NOT NULL,
    "GameId" INTEGER NULL,
    "SeasonStartYear" INTEGER NULL,
    "ExceptionType" VARCHAR(500) NOT NULL,
    "Message" TEXT NOT NULL,
    "StackTrace" TEXT NOT NULL,
    "Source" VARCHAR(250) NOT NULL,
    CONSTRAINT "PK_ErrorLog" PRIMARY KEY("Id")
);
