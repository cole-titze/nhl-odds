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