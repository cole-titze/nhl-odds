CREATE TABLE [dbo].[PlayerDraftDetails]
(
    PlayerId INT NOT NULL,
    [Year] INT NOT NULL,
    TeamAbbrev VARCHAR(MAX) NOT NULL,
    Round INT NOT NULL,
    PickInRound INT NOT NULL,
    OverallPick INT NOT NULL,
    CONSTRAINT PK_PlayerDraftDetails PRIMARY KEY(PlayerId),
    FOREIGN KEY (PlayerId) REFERENCES Player(Id)
);