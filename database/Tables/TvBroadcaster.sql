CREATE TABLE [dbo].[TvBroadcaster]
(
    id INT NOT NULL,
    networkName VARCHAR(MAX) NOT NULL,
    marketAbbreviation VARCHAR(MAX) NOT NULL,
    sequenceNumber INT NOT NULL,
    countryCode VARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_TvBroadcaster PRIMARY KEY(id),
);