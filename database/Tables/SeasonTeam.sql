CREATE TABLE [dbo].[SeasonTeam]
(
    TeamId INT NOT NULL,
    Abbreviation VARCHAR(MAX) NOT NULL,
    SeasonStartYear INT NOT NULL,
    [Name] VARCHAR(MAX) NOT NULL,
    CommonName VARCHAR(MAX) NOT NULL,
    PlaceName VARCHAR(MAX) NOT NULL,
    LogoUri VARCHAR(MAX) NOT NULL,
    Division VARCHAR(MAX) NOT NULL,
    Conference VARCHAR(MAX) NOT NULL,
    DivisionAbbreviation VARCHAR(5) NOT NULL,
    ConferenceAbbreviation VARCHAR(5) NOT NULL,
    FOREIGN KEY(TeamId) REFERENCES Team(Id),
    PRIMARY KEY(TeamId, SeasonStartYear),
);