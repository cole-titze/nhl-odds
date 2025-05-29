CREATE TABLE [dbo].[PlayerDraftDetails]
(
    playerId INT NOT NULL,
    [year] INT NOT NULL,
    teamAbbrev VARCHAR(MAX) NOT NULL,
    round INT NOT NULL,
    pickInRound INT NOT NULL,
    overallPick INT NOT NULL,
    CONSTRAINT PK_PlayerDraftDetails PRIMARY KEY(playerId),
    FOREIGN KEY (playerId) REFERENCES Player(id),
);