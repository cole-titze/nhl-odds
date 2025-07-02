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