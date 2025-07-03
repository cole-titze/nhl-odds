CREATE TABLE [dbo].[GameGoalieStats]
(
    GameId INT NOT NULL,
    PlayerId INT NOT NULL,
    TeamId INT NOT NULL,
    EvenStrengthShotsSaved INT NOT NULL,
    PowerPlayShotsSaved INT NOT NULL,
    ShortHandedShotsSaved INT NOT NULL,
    EvenStrengthGoalsAllowed INT NOT NULL,
    PowerPlayGoalsAllowed INT NOT NULL,
    ShortHandedGoalsAllowed INT NOT NULL,
    TimeOnIceSeconds INT NOT NULL,
    IsStarter BIT NOT NULL,
    Position INT NOT NULL,
    CONSTRAINT PK_GameGoalieStats PRIMARY KEY(GameId, PlayerId),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (PlayerId) REFERENCES Player(Id),
    FOREIGN KEY (TeamId) REFERENCES Team(Id)
);