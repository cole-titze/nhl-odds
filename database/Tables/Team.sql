CREATE TABLE [dbo].[Team]
(
    Id INT NOT NULL,
    Abbreviation VARCHAR(MAX) NOT NULL,
    FranchiseId INT NOT NULL,
    LeagueId INT NOT NULL,
    CONSTRAINT PK_Team PRIMARY KEY(Id),
);