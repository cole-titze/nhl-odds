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