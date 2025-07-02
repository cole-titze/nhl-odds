CREATE TABLE [dbo].[TvBroadcaster]
(
    Id INT NOT NULL,
    NetworkName VARCHAR(MAX) NOT NULL,
    MarketAbbreviation VARCHAR(MAX) NOT NULL,
    SequenceNumber INT NOT NULL,
    CountryCode VARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_TvBroadcaster PRIMARY KEY(Id)
);