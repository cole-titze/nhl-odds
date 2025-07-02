CREATE TABLE [dbo].[SeasonGameCount]
(
    SeasonId INT NOT NULL,
    GameCount INT NOT NULL,
    CONSTRAINT PK_SeasonGameCount PRIMARY KEY(SeasonId)
);