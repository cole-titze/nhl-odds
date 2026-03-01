CREATE TABLE [dbo].[ClassificationModel]
(
    Id INT NOT NULL,
    ModelFile varchar(MAX) NOT NULL,
    CONSTRAINT PK_ClassificationModel PRIMARY KEY(Id),
);

CREATE TABLE [dbo].[Team]
(
    Id INT NOT NULL,
    Abbreviation VARCHAR(MAX) NOT NULL,
    FranchiseId INT NOT NULL,
    LeagueId INT NOT NULL,
    CONSTRAINT PK_Team PRIMARY KEY(Id),
);

CREATE TABLE [dbo].[SeasonTeam]
(
    TeamId INT NOT NULL,
    Abbreviation VARCHAR(MAX) NOT NULL,
    SeasonStartYear INT NOT NULL,
    [Name] VARCHAR(MAX) NOT NULL,
    CommonName VARCHAR(MAX) NOT NULL,
    PlaceName VARCHAR(MAX) NOT NULL,
    LogoUri VARCHAR(MAX) NOT NULL,
    Division VARCHAR(MAX) NOT NULL,
    Conference VARCHAR(MAX) NOT NULL,
    DivisionAbbreviation VARCHAR(5) NOT NULL,
    ConferenceAbbreviation VARCHAR(5) NOT NULL,
    FOREIGN KEY(TeamId) REFERENCES Team(Id),
    CONSTRAINT PK_SeasonTeam PRIMARY KEY(TeamId, SeasonStartYear),
);

CREATE TABLE [dbo].[SeasonGameCount]
(
    SeasonId INT NOT NULL,
    GameCount INT NOT NULL,
    CONSTRAINT PK_SeasonGameCount PRIMARY KEY(SeasonId),
);

CREATE TABLE [dbo].[GameRaw]
(
    Id INT NOT NULL,
    HomeTeamId INT NOT NULL,
    AwayTeamId INT NOT NULL,
    SeasonStartYear INT NOT NULL,
    GameDateUTC DATETIME2 NOT NULL,
    HomeGoals INT NOT NULL,
    AwayGoals INT NOT NULL,
    HomeSOG INT NOT NULL,
    AwaySOG INT NOT NULL,
    HomePPG INT NOT NULL,
    AwayPPG INT NOT NULL,
    HomePIM INT NOT NULL,
    AwayPIM INT NOT NULL,
    HomeFaceOffWinPercent FLOAT NOT NULL,
    AwayFaceOffWinPercent FLOAT NOT NULL,
    HomeBlockedShots INT NOT NULL,
    AwayBlockedShots INT NOT NULL,
    HomeHits INT NOT NULL,
    AwayHits INT NOT NULL,
    HomeTakeaways INT NOT NULL,
    AwayTakeaways INT NOT NULL,
    HomeGiveaways INT NOT NULL,
    AwayGiveaways INT NOT NULL,
    Winner INT NOT NULL,
    EndPeriod INT NOT NULL,
    HasBeenPlayed BIT NOT NULL,
    GameSummary VARCHAR(MAX) NULL,
    EventSummary VARCHAR(MAX) NULL,
    PlayByPlaySummary VARCHAR(MAX) NULL,
    FaceoffSummary VARCHAR(MAX) NULL,
    FaceoffComparisonSummary VARCHAR(MAX) NULL,
    RosterSummary VARCHAR(MAX) NULL,
    ShotSummary VARCHAR(MAX) NULL,
    ShiftChartSummary VARCHAR(MAX) NULL,
    ToiAwaySummary VARCHAR(MAX) NULL,
    ToiHomeSummary VARCHAR(MAX) NULL,
    ThreeMinuteRecapVideoId INT NOT NULL,
    CondensedGameVideoId INT NOT NULL,
    VenueName VARCHAR(MAX) NOT NULL,
    VenueLocation VARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_GameRaw PRIMARY KEY(Id),
    FOREIGN KEY (HomeTeamId) REFERENCES Team(Id),
    FOREIGN KEY (AwayTeamId) REFERENCES Team(Id),
);

CREATE TABLE [dbo].[GameOfficial]
(
    GameId INT NOT NULL,
    [Name] VARCHAR(250) NOT NULL,
    [Role] INT NOT NULL,
    CONSTRAINT PK_GameOfficial PRIMARY KEY(GameId, [Name]),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
);

CREATE TABLE [dbo].[GameCleaned]
(
    GameId INT NOT NULL,
    HomeWinRatio FLOAT NOT NULL,
    HomeRecentWinRatio FLOAT NOT NULL,
    HomeRecentGoalsAvg FLOAT NOT NULL,
    HomeRecentConcededGoalsAvg FLOAT NOT NULL,
    HomeRecentSogAvg FLOAT NOT NULL,
    HomeRecentPpgAvg FLOAT NOT NULL,
    HomeRecentHitsAvg FLOAT NOT NULL,
    HomeRecentPimAvg FLOAT NOT NULL,
    HomeRecentBlockedShotsAvg FLOAT NOT NULL,
    HomeRecentTakeawaysAvg FLOAT NOT NULL,
    HomeRecentGiveawaysAvg FLOAT NOT NULL,
    HomeGoalsAvg FLOAT NOT NULL,
    HomeGoalsAvgAtHome FLOAT NOT NULL,
    HomeRecentGoalsAvgAtHome FLOAT NOT NULL,
    HomeConcededGoalsAvg FLOAT NOT NULL,
    HomeConcededGoalsAvgAtHome FLOAT NOT NULL,
    HomeRecentConcededGoalsAvgAtHome FLOAT NOT NULL,
    HomeHoursSinceLastGame FLOAT NOT NULL,
    AwayWinRatio FLOAT NOT NULL,
    AwayRecentWinRatio FLOAT NOT NULL,
    AwayRecentGoalsAvg FLOAT NOT NULL,
    AwayRecentConcededGoalsAvg FLOAT NOT NULL,
    AwayRecentSogAvg FLOAT NOT NULL,
    AwayRecentPpgAvg FLOAT NOT NULL,
    AwayRecentHitsAvg FLOAT NOT NULL,
    AwayRecentPimAvg FLOAT NOT NULL,
    AwayRecentBlockedShotsAvg FLOAT NOT NULL,
    AwayRecentTakeawaysAvg FLOAT NOT NULL,
    AwayRecentGiveawaysAvg FLOAT NOT NULL,
    AwayGoalsAvg FLOAT NOT NULL,
    AwayGoalsAvgAtAway FLOAT NOT NULL,
    AwayRecentGoalsAvgAtAway FLOAT NOT NULL,
    AwayConcededGoalsAvg FLOAT NOT NULL,
    AwayConcededGoalsAvgAtAway FLOAT NOT NULL,
    AwayRecentConcededGoalsAvgAtAway FLOAT NOT NULL,
    HomeRosterOffenseValue FLOAT NOT NULL,
    HomeRosterDefenseValue FLOAT NOT NULL,
    HomeRosterGoalieValue FLOAT NOT NULL,
    AwayRosterOffenseValue FLOAT NOT NULL,
    AwayRosterDefenseValue FLOAT NOT NULL,
    AwayRosterGoalieValue FLOAT NOT NULL,
    AwayHoursSinceLastGame FLOAT NOT NULL,
    CONSTRAINT PK_GameCleaned PRIMARY KEY(GameId),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
);

CREATE TABLE [dbo].[GameOdds]
(
    GameId INT NOT NULL,
    ModelName INT NOT NULL,
    RunDateUTC DATETIME2 NOT NULL,
    HomeOdds FLOAT NOT NULL,
    AwayOdds FLOAT NOT NULL,
    LogLoss FLOAT NOT NULL DEFAULT 0,
    Notes VARCHAR(MAX),
    CONSTRAINT PK_GameOdds PRIMARY KEY(GameId, ModelName, RunDateUTC),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
);

CREATE TABLE [dbo].[Player]
(
    Id INT NOT NULL,
    FirstName VARCHAR(MAX) NOT NULL,
    LastName VARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL,
    CurrentTeamId INT NOT NULL,
    HeadShot VARCHAR(MAX) NOT NULL,
    HeroImage VARCHAR(MAX) NOT NULL,
    HeightInInches INT NOT NULL,
    WeightInPounds INT NOT NULL,
    BirthDate DATETIME2,
    BirthCity VARCHAR(MAX),
    BirthStateProvince VARCHAR(MAX),
    BirthCountry VARCHAR(MAX),
    IsInTopOneHundredAllTime BIT NOT NULL,
    IsInHallOfFame BIT NOT NULL,
    ShopLink VARCHAR(MAX),
    TwitterLink VARCHAR(MAX),
    WatchLink VARCHAR(MAX),
    PlayerSlug VARCHAR(MAX),
    CONSTRAINT PK_Player PRIMARY KEY(Id),
    FOREIGN KEY (CurrentTeamId) REFERENCES Team(Id),
);

CREATE TABLE [dbo].[PlayerDraftDetails]
(
    PlayerId INT NOT NULL,
    [Year] INT NOT NULL,
    TeamAbbrev VARCHAR(MAX) NOT NULL,
    Round INT NOT NULL,
    PickInRound INT NOT NULL,
    OverallPick INT NOT NULL,
    CONSTRAINT PK_PlayerDraftDetails PRIMARY KEY(PlayerId),
    FOREIGN KEY (PlayerId) REFERENCES Player(Id),
);

CREATE TABLE [dbo].[GameSkaterStats]
(
    GameId INT NOT NULL,
    PlayerId INT NOT NULL,
    TeamId INT NOT NULL,
    Goals INT NOT NULL,
    Assists INT NOT NULL,
    PlusMinus INT NOT NULL,
    PenaltyMinutes INT NOT NULL,
    Hits INT NOT NULL,
    PowerPlayGoals INT NOT NULL,
    ShotsOnGoal INT NOT NULL,
    FaceoffWinningPctg FLOAT NOT NULL,
    BlockedShots INT NOT NULL,
    Giveaways INT NOT NULL,
    Takeaways INT NOT NULL,
    TimeOnIceSeconds INT NOT NULL,
    Position INT NOT NULL,
    CONSTRAINT PK_GameSkaterStats PRIMARY KEY(GameId,PlayerId),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (PlayerId) REFERENCES Player(Id),
    FOREIGN KEY (TeamId) REFERENCES Team(Id)
);

CREATE TABLE [dbo].[GameGoalieStats]
(
    GameId INT NOT NULL,
    PlayerId INT NOT NULL,
    TeamId INT NOT NULL,
    EvenStrengthShotsSaved INT NOT NULL,
    PowerPlayShotsSaved INT NOT NULL,
    ShortHandedShotsSaved INT NOT NULL,
    EvenStrengthGoalsAllowed INT NOT NULL,
    PowerPlayGoalsAllowed INT NOT NULL,
    ShortHandedGoalsAllowed INT NOT NULL,
    TimeOnIceSeconds INT NOT NULL,
    IsStarter BIT NOT NULL,
    Position INT NOT NULL,
    CONSTRAINT PK_GameGoalieStats PRIMARY KEY(GameId,PlayerId),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (PlayerId) REFERENCES Player(Id),
    FOREIGN KEY (TeamId) REFERENCES Team(Id)
);

CREATE TABLE [dbo].[TvBroadcaster]
(
    Id INT NOT NULL,
    NetworkName VARCHAR(MAX) NOT NULL,
    MarketAbbreviation VARCHAR(MAX) NOT NULL,
    SequenceNumber INT NOT NULL,
    CountryCode VARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_TvBroadcaster PRIMARY KEY(Id),
);

CREATE TABLE [dbo].[GameTvBroadcaster]
(
    GameId INT NOT NULL,
    BroadcasterId INT NOT NULL,
    CONSTRAINT PK_GameTvBroadcaster PRIMARY KEY(GameId, BroadcasterId),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (BroadcasterId) REFERENCES TvBroadcaster(Id)
);

CREATE TABLE [dbo].[GameCoach]
(
    GameId INT NOT NULL,
    [Name] VARCHAR(250) NOT NULL,
    [TeamId] INT NOT NULL,
    CONSTRAINT PK_GameCoach PRIMARY KEY(GameId, [Name]),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (TeamId) REFERENCES Team(Id)
);

-- Add Game Event Tables
CREATE TABLE [dbo].[GameBlockedShotEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    BlockingPlayerTeamId INT NOT NULL,
    BlockingPlayerId INT NOT NULL,
    ShooterPlayerId INT NOT NULL,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    BlockType INT NOT NULL,
    CONSTRAINT PK_BlockedShotEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (BlockingPlayerTeamId) REFERENCES Team(Id),
    FOREIGN KEY (BlockingPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (ShooterPlayerId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GameDelayedPenaltyEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    PenaltyTeamId INT NOT NULL,
    CONSTRAINT PK_GameDelayedPenaltyEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (PenaltyTeamId) REFERENCES Team(Id)
);

CREATE TABLE [dbo].[GameFailedShotAttemptEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    ShotType INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    ShootingTeamId INT NOT NULL,
    ShootingPlayerId INT NOT NULL,
    GoalieId INT,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    CONSTRAINT PK_GameFailedShotAttemptEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (ShootingTeamId) REFERENCES Team(Id),
    FOREIGN KEY (ShootingPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (GoalieId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GameFaceoffEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    WinningTeamId INT NOT NULL,
    WinningPlayerId INT NOT NULL,
    LosingPlayerId INT NOT NULL,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    CONSTRAINT PK_GameFaceoffEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (WinningTeamId) REFERENCES Team(Id),
    FOREIGN KEY (WinningPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (LosingPlayerId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GameGameEndEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    CONSTRAINT PK_GameGameEndEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id)
);

CREATE TABLE [dbo].[GameGiveawayEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    GiveawayPlayerTeamId INT NOT NULL,
    GiveawayPlayerId INT NOT NULL,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    CONSTRAINT PK_GameGiveawayEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (GiveawayPlayerTeamId) REFERENCES Team(Id),
    FOREIGN KEY (GiveawayPlayerId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GameGoalEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    ShotType INT NOT NULL,
    ScoringPlayerTeamId INT NOT NULL,
    AssistOnePlayerId INT,
    AssistTwoPlayerId INT,
    ScoringPlayerId INT NOT NULL,
    GoalieId INT,
    HighlightClipSharingUrl VARCHAR(255) NOT NULL,
    HighlightClipId INT NOT NULL,
    DiscreetClipId FLOAT NOT NULL,
    PptReplayUrl VARCHAR(255) NOT NULL,
    CONSTRAINT PK_GameGoalEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (ScoringPlayerTeamId) REFERENCES Team(Id),
    FOREIGN KEY (ScoringPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (AssistOnePlayerId) REFERENCES Player(Id),
    FOREIGN KEY (AssistTwoPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (GoalieId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GameHitEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    HittingPlayerTeamId INT NOT NULL,
    HittingPlayerId INT NOT NULL,
    HitteePlayerId INT NOT NULL,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    CONSTRAINT PK_GameHitEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (HittingPlayerTeamId) REFERENCES Team(Id),
    FOREIGN KEY (HittingPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (HitteePlayerId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GameMissedShotEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    ShotType INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    ShootingTeamId INT NOT NULL,
    ShootingPlayerId INT NOT NULL,
    GoalieId INT,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    MissType INT NOT NULL,
    CONSTRAINT PK_GameMissedShotEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (ShootingTeamId) REFERENCES Team(Id),
    FOREIGN KEY (ShootingPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (GoalieId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GamePenaltyEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    CommittedByPlayerTeamId INT NOT NULL,
    DrawnByPlayerId INT,
    CommittedByPlayerId INT,
    ServedByPlayerId INT,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    Duration INT NOT NULL,
    PenaltyType INT NOT NULL,
    PenaltySeverity INT NOT NULL,
    CONSTRAINT PK_GamePenaltyEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (CommittedByPlayerTeamId) REFERENCES Team(Id),
    FOREIGN KEY (DrawnByPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (CommittedByPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (ServedByPlayerId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GamePeriodStartEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    CONSTRAINT PK_GamePeriodStartEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id)
);

CREATE TABLE [dbo].[GamePeriodEndEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    CONSTRAINT PK_GamePeriodEndEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id)
);

CREATE TABLE [dbo].[GameShotEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    ShootingTeamId INT NOT NULL,
    ShootingPlayerId INT NOT NULL,
    GoalieId INT,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    ShotType INT NOT NULL,
    CONSTRAINT PK_ShotEvent PRIMARY KEY(GameId,Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (ShootingTeamId) REFERENCES Team(Id),
    FOREIGN KEY (ShootingPlayerId) REFERENCES Player(Id),
    FOREIGN KEY (GoalieId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GameStoppageEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    StoppageType INT NOT NULL,
    StoppageDetails INT NOT NULL,
    CONSTRAINT PK_GameStoppageEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id)
);

CREATE TABLE [dbo].[GameTakeawayEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    TakeawayPlayerTeamId INT NOT NULL,
    TakeawayPlayerId INT NOT NULL,
    XCoordinate INT,
    YCoordinate INT,
    [Zone] INT NOT NULL,
    CONSTRAINT PK_GameTakeawayEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (TakeawayPlayerTeamId) REFERENCES Team(Id),
    FOREIGN KEY (TakeawayPlayerId) REFERENCES Player(Id)
);

CREATE TABLE [dbo].[GameShootoutCompleteEvent]
(
    Id INT NOT NULL,
    GameId INT NOT NULL,
    TypeCode INT NOT NULL,
    SortOrder INT NOT NULL,
    SituationCode INT NOT NULL,
    PeriodNumber INT NOT NULL,
    PeriodType INT NOT NULL,
    EventTypeName VARCHAR(100) NOT NULL,
    HomeTeamDefendingSide INT NOT NULL,
    SecondsIntoPeriod INT NOT NULL,
    SecondsLeftInPeriod INT NOT NULL,
    CONSTRAINT PK_GameShootoutEvent PRIMARY KEY(GameId, Id),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id)
);

CREATE TABLE [dbo].[ErrorLog]
(
    Id INT IDENTITY(1,1) NOT NULL,
    TimestampUTC DATETIME2 NOT NULL,
    GameId INT NULL,
    SeasonStartYear INT NULL,
    ExceptionType VARCHAR(500) NOT NULL,
    Message VARCHAR(MAX) NOT NULL,
    StackTrace VARCHAR(MAX) NOT NULL,
    Source VARCHAR(250) NOT NULL,
    CONSTRAINT PK_ErrorLog PRIMARY KEY(Id)
);

GO