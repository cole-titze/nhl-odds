CREATE TABLE [dbo].[BookmakerOddsResponse]
(
    Id INT IDENTITY(1,1) NOT NULL,
    FetchedDateUTC DATETIME2 NOT NULL,
    QueryDateUTC DATETIME2 NULL,
    RawJson VARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_BookmakerOddsResponse PRIMARY KEY(Id),
);
