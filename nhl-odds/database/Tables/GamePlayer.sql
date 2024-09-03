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