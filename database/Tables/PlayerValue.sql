CREATE TABLE [dbo].[PlayerValue]
(
	[id] INT NOT NULL,
	[name] VARCHAR(MAX) NOT NULL,
	[value] FLOAT NOT NULL,
	[seasonStartYear] INT NOT NULL,
	[position] varchar(MAX) NOT NULL
	CONSTRAINT PK_PlayerValue PRIMARY KEY (id, seasonStartYear),
)