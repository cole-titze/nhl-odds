CREATE TABLE [dbo].[GameGoalieStats]
(
    gameId INT NOT NULL,
    playerId INT NOT NULL,
    teamId INT NOT NULL,
    evenStrengthShotsSaved INT NOT NULL,
    powerPlayShotsSaved INT NOT NULL,
    evenStrengthGoalsAllowed INT NOT NULL,
    powerPlayGoalsAllowed INT NOT NULL,
    timeOnIceSeconds FLOAT NOT NULL,
    isStarter BIT NOT NULL,
    CONSTRAINT PK_GameGoalieStats PRIMARY KEY(gameId,playerId),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (playerId) REFERENCES Player(id),
    FOREIGN KEY (teamId) REFERENCES Team(id)
);