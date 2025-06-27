CREATE TABLE [dbo].[ClassificationModel]
(
    id INT NOT NULL,
    modelFile varchar(MAX) NULL
    PRIMARY KEY(id),
);

CREATE TABLE [dbo].[Team]
(
    id INT NOT NULL,
    abbreviation VARCHAR(MAX) NOT NULL,
    locationName VARCHAR(MAX) NOT NULL,
    teamName VARCHAR(MAX) NOT NULL,
    logoUri VARCHAR(MAX) NOT NULL,
    PRIMARY KEY(id),
);

CREATE TABLE [dbo].[SeasonGameCount]
(
	[seasonId] INT NOT NULL,
	[gameCount] INT NOT NULL,
    PRIMARY KEY(seasonId),
);

CREATE TABLE [dbo].[GameRaw]
(
    id INT NOT NULL,
    homeTeamId INT NOT NULL,
    awayTeamId INT NOT NULL,
    seasonStartYear INT NOT NULL,
    gameDateUTC DATETIME2 NOT NULL,
    homeGoals INT NOT NULL,
    awayGoals INT NOT NULL,
    homeSOG INT NOT NULL,
    awaySOG INT NOT NULL,
    homePPG INT NOT NULL,
    awayPPG INT NOT NULL,
    homePIM INT NOT NULL,
    awayPIM INT NOT NULL,
    homeFaceOffWinPercent FLOAT NOT NULL,
    awayFaceOffWinPercent FLOAT NOT NULL,
    homeBlockedShots INT NOT NULL,
    awayBlockedShots INT NOT NULL,
    homeHits INT NOT NULL,
    awayHits INT NOT NULL,
    homeTakeaways INT NOT NULL,
    awayTakeaways INT NOT NULL,
    homeGiveaways INT NOT NULL,
    awayGiveaways INT NOT NULL,
    winner INT NOT NULL,
    hasBeenPlayed BIT NOT NULL,
    gameSummary VARCHAR(MAX) NULL,
    eventSummary VARCHAR(MAX) NULL,
    playByPlaySummary VARCHAR(MAX) NULL,
    faceoffSummary VARCHAR(MAX) NULL,
    faceoffComparisonSummary VARCHAR(MAX) NULL,
    rosterSummary VARCHAR(MAX) NULL,
    shotSummary VARCHAR(MAX) NULL,
    shiftChartSummary VARCHAR(MAX) NULL,
    toiAwaySummary VARCHAR(MAX) NULL,
    toiHomeSummary VARCHAR(MAX) NULL,
    threeMinuteRecapVideoId INT NOT NULL,
    condensedGameVideoId INT NOT NULL,
    venueName VARCHAR(MAX) NOT NULL,
    venueLocation VARCHAR(MAX) NOT NULL,
    PRIMARY KEY(id),
    FOREIGN KEY (homeTeamId) REFERENCES Team(id),
    FOREIGN KEY (awayTeamId) REFERENCES Team(id),
);

CREATE TABLE [dbo].[GameOfficial]
(
    gameId INT NOT NULL,
    [name] VARCHAR(250) NOT NULL,
    [role] INT NOT NULL,
    CONSTRAINT PK_GameOfficial PRIMARY KEY(gameId, [name]),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
);

CREATE TABLE [dbo].[GameCleaned]
(
    gameId INT NOT NULL,
    homeWinRatio FLOAT NOT NULL,
    homeRecentWinRatio FLOAT NOT NULL,
    homeRecentGoalsAvg FLOAT NOT NULL,
    homeRecentConcededGoalsAvg FLOAT NOT NULL,
    homeRecentSogAvg FLOAT NOT NULL,
    homeRecentPpgAvg FLOAT NOT NULL,
    homeRecentHitsAvg FLOAT NOT NULL,
    homeRecentPimAvg FLOAT NOT NULL,
    homeRecentBlockedShotsAvg FLOAT NOT NULL,
    homeRecentTakeawaysAvg FLOAT NOT NULL,
    homeRecentGiveawaysAvg FLOAT NOT NULL,
    homeGoalsAvg FLOAT NOT NULL,
    homeGoalsAvgAtHome FLOAT NOT NULL,
    homeRecentGoalsAvgAtHome FLOAT NOT NULL,
    homeConcededGoalsAvg FLOAT NOT NULL,
    homeConcededGoalsAvgAtHome FLOAT NOT NULL,
    homeRecentConcededGoalsAvgAtHome FLOAT NOT NULL,
    homeHoursSinceLastGame FLOAT NOT NULL,
    awayWinRatio FLOAT NOT NULL,
    awayRecentWinRatio FLOAT NOT NULL,
    awayRecentGoalsAvg FLOAT NOT NULL,
    awayRecentConcededGoalsAvg FLOAT NOT NULL,
    awayRecentSogAvg FLOAT NOT NULL,
    awayRecentPpgAvg FLOAT NOT NULL,
    awayRecentHitsAvg FLOAT NOT NULL,
    awayRecentPimAvg FLOAT NOT NULL,
    awayRecentBlockedShotsAvg FLOAT NOT NULL,
    awayRecentTakeawaysAvg FLOAT NOT NULL,
    awayRecentGiveawaysAvg FLOAT NOT NULL,
    awayGoalsAvg FLOAT NOT NULL,
    awayGoalsAvgAtAway FLOAT NOT NULL,
    awayRecentGoalsAvgAtAway FLOAT NOT NULL,
    awayConcededGoalsAvg FLOAT NOT NULL,
    awayConcededGoalsAvgAtAway FLOAT NOT NULL,
    awayRecentConcededGoalsAvgAtAway FLOAT NOT NULL,
    homeRosterOffenseValue FLOAT NOT NULL,
    homeRosterDefenseValue FLOAT NOT NULL,
    homeRosterGoalieValue FLOAT NOT NULL,
    awayRosterOffenseValue FLOAT NOT NULL,
    awayRosterDefenseValue FLOAT NOT NULL,
    awayRosterGoalieValue FLOAT NOT NULL,
    awayHoursSinceLastGame FLOAT NOT NULL,
    PRIMARY KEY(gameId),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
);

CREATE TABLE [dbo].[GameOdds]
(
    gameId INT NOT NULL,
    modelName INT NOT NULL,
    runDateUTC DATETIME2 NOT NULL,
    homeOdds FLOAT NOT NULL,
    awayOdds FLOAT NOT NULL,
    logLoss FLOAT NOT NULL DEFAULT 0,
    notes VARCHAR(MAX),
    CONSTRAINT PK_GameOdds PRIMARY KEY(gameId, modelName, runDateUTC),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
);

CREATE TABLE [dbo].[Player]
(
    id INT NOT NULL,
    firstName VARCHAR(MAX) NOT NULL,
    lastName VARCHAR(MAX) NOT NULL,
    isActive BIT NOT NULL,
    currentTeamId INT NOT NULL,
    headShot VARCHAR(MAX) NOT NULL,
    heroImage VARCHAR(MAX) NOT NULL,
    heightInInches INT NOT NULL,
    weightInPounds INT NOT NULL,
    birthDate VARCHAR(MAX),
    birthCity VARCHAR(MAX),
    birthStateProvince VARCHAR(MAX),
    birthCountry VARCHAR(MAX),
    isInTopOneHundredAllTime BIT NOT NULL,
    isInHallOfFame BIT NOT NULL,
    shopLink VARCHAR(MAX),
    twitterLink VARCHAR(MAX),
    watchLink VARCHAR(MAX),
    playerSlug VARCHAR(MAX),
    CONSTRAINT PK_Player PRIMARY KEY(id),
    FOREIGN KEY (currentTeamId) REFERENCES Team(id),
);

CREATE TABLE [dbo].[PlayerDraftDetails]
(
    playerId INT NOT NULL,
    [year] INT NOT NULL,
    teamAbbrev VARCHAR(MAX) NOT NULL,
    round INT NOT NULL,
    pickInRound INT NOT NULL,
    overallPick INT NOT NULL,
    CONSTRAINT PK_PlayerDraftDetails PRIMARY KEY(playerId),
    FOREIGN KEY (playerId) REFERENCES Player(id),
);

CREATE TABLE [dbo].[GameSkaterStats]
(
    gameId INT NOT NULL,
    playerId INT NOT NULL,
    teamId INT NOT NULL,
    goals INT NOT NULL,
    assists INT NOT NULL,
    plusMinus INT NOT NULL,
    penaltyMinutes INT NOT NULL,
    hits INT NOT NULL,
    powerPlayGoals INT NOT NULL,
    shotsOnGoal INT NOT NULL,
    faceoffWinningPctg FLOAT NOT NULL,
    blockedShots INT NOT NULL,
    giveaways INT NOT NULL,
    takeaways INT NOT NULL,
    timeOnIceSeconds FLOAT NOT NULL,
    position INT NOT NULL,
    CONSTRAINT PK_GameSkaterStats PRIMARY KEY(gameId,playerId),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (playerId) REFERENCES Player(id),
    FOREIGN KEY (teamId) REFERENCES Team(id)
);

CREATE TABLE [dbo].[GameGoalieStats]
(
    gameId INT NOT NULL,
    playerId INT NOT NULL,
    teamId INT NOT NULL,
    evenStrengthShotsSaved INT NOT NULL,
    powerPlayShotsSaved INT NOT NULL,
    shortHandedShotsSaved INT NOT NULL,
    evenStrengthGoalsAllowed INT NOT NULL,
    powerPlayGoalsAllowed INT NOT NULL,
    shortHandedGoalsAllowed INT NOT NULL,
    timeOnIceSeconds FLOAT NOT NULL,
    isStarter BIT NOT NULL,
    position INT NOT NULL,
    CONSTRAINT PK_GameGoalieStats PRIMARY KEY(gameId,playerId),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (playerId) REFERENCES Player(id),
    FOREIGN KEY (teamId) REFERENCES Team(id)
);

CREATE TABLE [dbo].[TvBroadcaster]
(
    id INT NOT NULL,
    networkName VARCHAR(MAX) NOT NULL,
    marketAbbreviation VARCHAR(MAX) NOT NULL,
    sequenceNumber INT NOT NULL,
    countryCode VARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_TvBroadcaster PRIMARY KEY(id),
);

CREATE TABLE [dbo].[GameTvBroadcaster]
(
    gameId INT NOT NULL,
    broadcasterId INT NOT NULL,
    CONSTRAINT PK_GameTvBroadcaster PRIMARY KEY(gameId, broadcasterId),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (broadcasterId) REFERENCES TvBroadcaster(id)
);

-- Add Game Event Tables
CREATE TABLE [dbo].[GameBlockedShotEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    blockingPlayerTeamId INT NOT NULL,
    blockingPlayerId INT NOT NULL,
    shooterPlayerId INT NOT NULL,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    blockType INT NOT NULL,
    CONSTRAINT PK_BlockedShotEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (blockingPlayerTeamId) REFERENCES Team(id),
    FOREIGN KEY (blockingPlayerId) REFERENCES Player(id),
    FOREIGN KEY (shooterPlayerId) REFERENCES Player(id)
);

CREATE TABLE [dbo].[GameDelayedPenaltyEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    penaltyTeamId INT NOT NULL,
    CONSTRAINT PK_GameDelayedPenaltyEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (penaltyTeamId) REFERENCES Team(id)
);

CREATE TABLE [dbo].[GameFaceoffEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    winningTeamId INT NOT NULL,
    winningPlayerId INT NOT NULL,
    losingPlayerId INT NOT NULL,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    CONSTRAINT PK_GameFaceoffEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (winningTeamId) REFERENCES Team(id),
    FOREIGN KEY (winningPlayerId) REFERENCES Player(id),
    FOREIGN KEY (losingPlayerId) REFERENCES Player(id)
);

CREATE TABLE [dbo].[GameGameEndEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    CONSTRAINT PK_GameGameEndEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id)
);

CREATE TABLE [dbo].[GameGiveawayEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    giveawayPlayerTeamId INT NOT NULL,
    giveawayPlayerId INT NOT NULL,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    CONSTRAINT PK_GameGiveawayEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (giveawayPlayerTeamId) REFERENCES Team(id),
    FOREIGN KEY (giveawayPlayerId) REFERENCES Player(id)
);

CREATE TABLE [dbo].[GameGoalEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    shotType INT NOT NULL,
    scoringPlayerTeamId INT NOT NULL,
    assistOnePlayerId INT,
    assistTwoPlayerId INT,
    scoringPlayerId INT NOT NULL,
    goalieId INT,
    highlightClipSharingUrl VARCHAR(255) NOT NULL,
    highlightClipId INT NOT NULL,
    discreetClipId INT NOT NULL,
    pptReplayUrl VARCHAR(255) NOT NULL,
    CONSTRAINT PK_GameGoalEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (scoringPlayerTeamId) REFERENCES Team(id),
    FOREIGN KEY (scoringPlayerId) REFERENCES Player(id),
    FOREIGN KEY (assistOnePlayerId) REFERENCES Player(id),
    FOREIGN KEY (assistTwoPlayerId) REFERENCES Player(id),
    FOREIGN KEY (goalieId) REFERENCES Player(id)
);

CREATE TABLE [dbo].[GameHitEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    hittingPlayerTeamId INT NOT NULL,
    hittingPlayerId INT NOT NULL,
    hitteePlayerId INT NOT NULL,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    CONSTRAINT PK_GameHitEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (hittingPlayerTeamId) REFERENCES Team(id),
    FOREIGN KEY (hittingPlayerId) REFERENCES Player(id),
    FOREIGN KEY (hitteePlayerId) REFERENCES Player(id)
);

CREATE TABLE [dbo].[GameMissedShotEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    shotType INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    shootingTeamId INT NOT NULL,
    shootingPlayerId INT NOT NULL,
    goalieId INT,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    missType INT NOT NULL,
    CONSTRAINT PK_GameMissedShotEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (shootingTeamId) REFERENCES Team(id),
    FOREIGN KEY (shootingPlayerId) REFERENCES Player(id),
    FOREIGN KEY (goalieId) REFERENCES Player(id)
);

CREATE TABLE [dbo].[GamePenaltyEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    committedByPlayerTeamId INT NOT NULL,
    servedByPlayerId INT NOT NULL,
    drawnByPlayerId INT,
    committedByPlayerId INT,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    duration INT NOT NULL,
    penaltyType INT NOT NULL,
    penaltySeverity INT NOT NULL,
    CONSTRAINT PK_GamePenaltyEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (committedByPlayerTeamId) REFERENCES Team(id),
    FOREIGN KEY (drawnByPlayerId) REFERENCES Player(id),
    FOREIGN KEY (committedByPlayerId) REFERENCES Player(id),
    FOREIGN KEY (servedByPlayerId) REFERENCES Player(id)
);

CREATE TABLE [dbo].[GamePeriodStartEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    CONSTRAINT PK_GamePeriodStartEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id)
);

CREATE TABLE [dbo].[GamePeriodEndEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    CONSTRAINT PK_GamePeriodEndEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id)
);

CREATE TABLE [dbo].[GameShotEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    shootingTeamId INT NOT NULL,
    shootingPlayerId INT NOT NULL,
    goalieId INT NOT NULL,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    shotType INT NOT NULL,
    CONSTRAINT PK_ShotEvent PRIMARY KEY(gameId,id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (shootingTeamId) REFERENCES Team(id),
    FOREIGN KEY (shootingPlayerId) REFERENCES Player(id),
    FOREIGN KEY (goalieId) REFERENCES Player(id)
);

CREATE TABLE [dbo].[GameStoppageEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    stoppageType INT NOT NULL,
    stoppageDetails INT NOT NULL,
    CONSTRAINT PK_GameStoppageEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id)
);

CREATE TABLE [dbo].[GameTakeawayEvent]
(
    id INT NOT NULL,
    gameId INT NOT NULL,
    typeCode INT NOT NULL,
    sortOrder INT NOT NULL,
    situationCode INT NOT NULL,
    periodNumber INT NOT NULL,
    periodType INT NOT NULL,
    eventTypeName VARCHAR(100) NOT NULL,
    homeTeamDefendingSide INT NOT NULL,
    secondsIntoPeriod INT NOT NULL,
    secondsLeftInPeriod INT NOT NULL,
    takeawayPlayerTeamId INT NOT NULL,
    takeawayPlayerId INT NOT NULL,
    xCoordinate INT NOT NULL,
    yCoordinate INT NOT NULL,
    [zone] INT NOT NULL,
    CONSTRAINT PK_GameTakeawayEvent PRIMARY KEY(gameId, id),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (takeawayPlayerTeamId) REFERENCES Team(id),
    FOREIGN KEY (takeawayPlayerId) REFERENCES Player(id)
);

GO