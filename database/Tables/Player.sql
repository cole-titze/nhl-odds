CREATE TABLE [dbo].[Player]
(
    Id INT NOT NULL,
    FirstName VARCHAR(MAX) NOT NULL,
    LastName VARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL,
    CurrentTeamId INT NOT NULL,
    HeadShot VARCHAR(MAX) NOT NULL,
    HeroImage VARCHAR(MAX) NOT NULL,
    HeightInInches INT NOT NULL,
    WeightInPounds INT NOT NULL,
    BirthDate DATETIME2,
    BirthCity VARCHAR(MAX),
    BirthStateProvince VARCHAR(MAX),
    BirthCountry VARCHAR(MAX),
    IsInTopOneHundredAllTime BIT NOT NULL,
    IsInHallOfFame BIT NOT NULL,
    ShopLink VARCHAR(MAX),
    TwitterLink VARCHAR(MAX),
    WatchLink VARCHAR(MAX),
    PlayerSlug VARCHAR(MAX),
    CONSTRAINT PK_Player PRIMARY KEY(Id),
    FOREIGN KEY (CurrentTeamId) REFERENCES Team(Id)
);