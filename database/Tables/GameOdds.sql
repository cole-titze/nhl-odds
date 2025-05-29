CREATE TABLE [dbo].[GameOdds]
(
    gameId INT NOT NULL,
    modelName INT NOT NULL,
    runDateUTC DATETIME2 NOT NULL,
    homeOdds FLOAT NOT NULL,
    awayOdds FLOAT NOT NULL,
    logLoss FLOAT NOT NULL DEFAULT 0,
    notes VARCHAR(MAX),
    CONSTRAINT PK_GameOdds PRIMARY KEY(gameId, modelName, runDateUTC),
    FOREIGN KEY (gameId) REFERENCES Game(id),
);