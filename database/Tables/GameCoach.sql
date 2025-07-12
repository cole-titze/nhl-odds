CREATE TABLE [dbo].[GameCoach]
(
    GameId INT NOT NULL,
    [Name] VARCHAR(250) NOT NULL,
    [TeamId] INT NOT NULL,
    CONSTRAINT PK_GameCoach PRIMARY KEY(GameId, [Name]),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (TeamId) REFERENCES Team(Id)
);