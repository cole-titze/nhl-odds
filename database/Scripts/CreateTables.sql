CREATE TABLE [dbo].[Team]
(
    id INT NOT NULL,
    abbreviation VARCHAR(MAX) NOT NULL,
    locationName VARCHAR(MAX) NOT NULL,
    teamName VARCHAR(MAX) NOT NULL,
    logoUri VARCHAR(MAX) NOT NULL,
    PRIMARY KEY(id),
);

CREATE TABLE [dbo].[Game]
(
    id INT NOT NULL,
    homeTeamId INT NOT NULL,
    awayTeamId INT NOT NULL,
    seasonStartYear INT NOT NULL,
    gameDate DATETIME2 NOT NULL,
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
    PRIMARY KEY(id),
    FOREIGN KEY (homeTeamId) REFERENCES Team(id),
    FOREIGN KEY (awayTeamId) REFERENCES Team(id),
)

CREATE TABLE [dbo].[LogLossGame]
(
    gameId INT NOT NULL,
    draftKingsLogLoss FLOAT NOT NULL DEFAULT 0,
    myBookieLogLoss FLOAT NOT NULL DEFAULT 0,
    betMgmLogLoss FLOAT NOT NULL DEFAULT 0,
    modelLogLoss FLOAT NOT NULL DEFAULT 0,
    PRIMARY KEY(gameId),
    FOREIGN KEY (gameId) REFERENCES Game(id),
);

CREATE TABLE [dbo].[GameOdds]
(
    gameId INT NOT NULL,
    draftKingsHomeOdds FLOAT NOT NULL DEFAULT 0,
    draftKingsAwayOdds FLOAT NOT NULL DEFAULT 0,
    bovadaHomeOdds FLOAT NOT NULL DEFAULT 0,
    bovadaAwayOdds FLOAT NOT NULL DEFAULT 0,
    betMgmHomeOdds FLOAT NOT NULL DEFAULT 0,
    betMgmAwayOdds FLOAT NOT NULL DEFAULT 0,
    barstoolHomeOdds FLOAT NOT NULL DEFAULT 0,
    barstoolAwayOdds FLOAT NOT NULL DEFAULT 0,
    modelHomeOdds FLOAT NOT NULL DEFAULT 0,
    modelAwayOdds FLOAT NOT NULL DEFAULT 0,
    PRIMARY KEY(gameId),
    FOREIGN KEY (gameId) REFERENCES Game(id),
);

CREATE TABLE [dbo].[CleanedGame]
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
    FOREIGN KEY (gameId) REFERENCES Game(id),
);

CREATE TABLE [dbo].[PlayerValue]
(
	[id] INT NOT NULL,
	[name] VARCHAR(MAX) NOT NULL,
	[value] FLOAT NOT NULL,
	[seasonStartYear] INT NOT NULL,
	[position] varchar(MAX) NOT NULL
	CONSTRAINT PK_PlayerValue PRIMARY KEY (id, seasonStartYear),
)

CREATE TABLE [dbo].[GamePlayer]
(
    gameId INT NOT NULL,
    teamId INT NOT NULL,
    playerId INT NOT NULL,
    seasonStartYear INT NOT NULL
    CONSTRAINT PK_GamePlayer PRIMARY KEY(gameId,playerId),
    FOREIGN KEY (gameId) REFERENCES Game(id),
    FOREIGN KEY (teamId) REFERENCES Team(id),
);

CREATE TABLE [dbo].[SeasonGameCount]
(
	[seasonId] INT NOT NULL PRIMARY KEY,
	[gameCount] INT NOT NULL,
)

CREATE TABLE [dbo].[ClassificationModel]
(
    id INT NOT NULL,
    modelFile varchar(MAX) NULL
    PRIMARY KEY(id),
)

GO