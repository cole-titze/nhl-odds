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