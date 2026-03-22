CREATE TABLE [dbo].[BookmakerSpreads]
(
    GameId INT NOT NULL,
    BookmakerName VARCHAR(100) NOT NULL,
    BookmakerKey VARCHAR(100) NOT NULL DEFAULT '',
    HomePoint FLOAT NOT NULL,
    HomePrice INT NOT NULL,
    AwayPoint FLOAT NOT NULL,
    AwayPrice INT NOT NULL,
    FetchedDateUTC DATETIME2 NOT NULL,
    BookmakerLastUpdate DATETIME2 NOT NULL DEFAULT '0001-01-01',
    MarketLastUpdate DATETIME2 NOT NULL DEFAULT '0001-01-01',
    OddsApiGameId VARCHAR(100) NOT NULL DEFAULT '',
    CONSTRAINT PK_BookmakerSpreads PRIMARY KEY(GameId, BookmakerName),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
);
