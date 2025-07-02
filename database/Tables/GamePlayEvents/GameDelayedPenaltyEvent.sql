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
    FOREIGN KEY (GameId) REFERENCES GameRaw(id),
    FOREIGN KEY (PenaltyTeamId) REFERENCES Team(id)
);