CREATE TABLE [dbo].[ErrorLog]
(
    Id INT IDENTITY(1,1) NOT NULL,
    TimestampUTC DATETIME2 NOT NULL,
    GameId INT NULL,
    SeasonStartYear INT NULL,
    ExceptionType VARCHAR(500) NOT NULL,
    Message VARCHAR(MAX) NOT NULL,
    StackTrace VARCHAR(MAX) NOT NULL,
    Source VARCHAR(250) NOT NULL,
    CONSTRAINT PK_ErrorLog PRIMARY KEY(Id)
);
