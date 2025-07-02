CREATE TABLE [dbo].[GameOfficial]
(
    GameId INT NOT NULL,
    [Name] VARCHAR(250) NOT NULL,
    [Role] INT NOT NULL,
    CONSTRAINT PK_GameOfficial PRIMARY KEY(GameId, [Name]),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id)
);