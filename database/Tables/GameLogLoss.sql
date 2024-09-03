CREATE TABLE [dbo].[LogLossGame]
(
    gameId INT NOT NULL,
    draftKingsLogLoss FLOAT NOT NULL DEFAULT 0,
    bovadaLogLoss FLOAT NOT NULL DEFAULT 0,
    betMgmLogLoss FLOAT NOT NULL DEFAULT 0,
    barstoolLogLoss FLOAT NOT NULL DEFAULT 0,
    modelLogLoss FLOAT NOT NULL DEFAULT 0,
    PRIMARY KEY(gameId),
    FOREIGN KEY (gameId) REFERENCES Game(id),
);