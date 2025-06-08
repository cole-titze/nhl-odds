CREATE TABLE [dbo].[GameOfficial]
(
    gameId INT NOT NULL,
    [name] VARCHAR(250) NOT NULL,
    [role] INT NOT NULL,
    CONSTRAINT PK_GameOfficial PRIMARY KEY(gameId, [name]),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
);