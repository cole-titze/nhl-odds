CREATE TABLE [dbo].[GameOdds]
(
    gameId INT NOT NULL,
    draftKingsHomeOdds FLOAT NOT NULL DEFAULT 0,
    draftKingsAwayOdds FLOAT NOT NULL DEFAULT 0,
    bovadaHomeOdds FLOAT NOT NULL DEFAULT 0,
    bovadaAwayOdds FLOAT NOT NULL DEFAULT 0,
    betMgmHomeOdds FLOAT NOT NULL DEFAULT 0,
    betMgmAwayOdds FLOAT NOT NULL DEFAULT 0,
    barstoolHomeOdds FLOAT NOT NULL DEFAULT 0,
    barstoolAwayOdds FLOAT NOT NULL DEFAULT 0,
    modelHomeOdds FLOAT NOT NULL DEFAULT 0,
    modelAwayOdds FLOAT NOT NULL DEFAULT 0,
    PRIMARY KEY(gameId),
    FOREIGN KEY (gameId) REFERENCES Game(id),
);