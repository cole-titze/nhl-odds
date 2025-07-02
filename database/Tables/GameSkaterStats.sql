CREATE TABLE [dbo].[GameSkaterStats]
(
    GameId INT NOT NULL,
    PlayerId INT NOT NULL,
    TeamId INT NOT NULL,
    Goals INT NOT NULL,
    Assists INT NOT NULL,
    PlusMinus INT NOT NULL,
    PenaltyMinutes INT NOT NULL,
    Hits INT NOT NULL,
    PowerPlayGoals INT NOT NULL,
    ShotsOnGoal INT NOT NULL,
    FaceoffWinningPctg FLOAT NOT NULL,
    BlockedShots INT NOT NULL,
    Giveaways INT NOT NULL,
    Takeaways INT NOT NULL,
    TimeOnIceSeconds FLOAT NOT NULL,
    Position INT NOT NULL,
    CONSTRAINT PK_GameSkaterStats PRIMARY KEY(GameId, PlayerId),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (PlayerId) REFERENCES Player(Id),
    FOREIGN KEY (TeamId) REFERENCES Team(Id)
);