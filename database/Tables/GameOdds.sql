CREATE TABLE [dbo].[GameOdds]
(
    GameId INT NOT NULL,
    ModelId INT NOT NULL,
    RunDateUTC DATETIME2 NOT NULL,
    HomeOdds FLOAT NOT NULL,
    AwayOdds FLOAT NOT NULL,
    LogLoss FLOAT NOT NULL DEFAULT 0,
    Notes VARCHAR(MAX),
    CONSTRAINT PK_GameOdds PRIMARY KEY(GameId, ModelId, RunDateUTC),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id)
);