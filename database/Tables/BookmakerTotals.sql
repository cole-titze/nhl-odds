CREATE TABLE [dbo].[BookmakerTotals]
(
    GameId INT NOT NULL,
    BookmakerName VARCHAR(100) NOT NULL,
    BookmakerKey VARCHAR(100) NOT NULL DEFAULT '',
    OverUnderPoint FLOAT NOT NULL,
    OverPrice INT NOT NULL,
    UnderPrice INT NOT NULL,
    FetchedDateUTC DATETIME2 NOT NULL,
    BookmakerLastUpdate DATETIME2 NOT NULL DEFAULT '0001-01-01',
    MarketLastUpdate DATETIME2 NOT NULL DEFAULT '0001-01-01',
    OddsApiGameId VARCHAR(100) NOT NULL DEFAULT '',
    CONSTRAINT PK_BookmakerTotals PRIMARY KEY(GameId, BookmakerName),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
);
