CREATE TABLE [dbo].[GameSpreadTotalOdds]
(
    GameId INT NOT NULL,
    ModelId INT NOT NULL,
    RunDateUTC DATETIME2 NOT NULL,
    PredictedValue FLOAT NOT NULL,
    ResidualStd FLOAT NOT NULL,
    Line FLOAT NULL,
    CoverProbability FLOAT NULL,
    Notes VARCHAR(MAX),
    CONSTRAINT PK_GameSpreadTotalOdds PRIMARY KEY(GameId, ModelId, RunDateUTC),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id)
);
