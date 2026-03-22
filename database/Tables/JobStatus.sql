CREATE TABLE [dbo].[JobStatus]
(
    JobName VARCHAR(100) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'idle',
    StartedAt DATETIME2 NULL,
    FinishedAt DATETIME2 NULL,
    Error VARCHAR(MAX) NULL,
    CONSTRAINT PK_JobStatus PRIMARY KEY(JobName)
);
