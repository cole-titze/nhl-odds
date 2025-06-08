CREATE TABLE [dbo].[GameSkaterStats]
(
    gameId INT NOT NULL,
    playerId INT NOT NULL,
    teamId INT NOT NULL,
    goals INT NOT NULL,
    assists INT NOT NULL,
    plusMinus INT NOT NULL,
    penaltyMinutes INT NOT NULL,
    hits INT NOT NULL,
    powerPlayGoals INT NOT NULL,
    shotsOnGoal INT NOT NULL,
    faceoffWinningPctg FLOAT NOT NULL,
    blockedShots INT NOT NULL,
    giveaways INT NOT NULL,
    takeaways INT NOT NULL,
    timeOnIceSeconds FLOAT NOT NULL,
    position INT NOT NULL,
    CONSTRAINT PK_GameSkaterStats PRIMARY KEY(gameId,playerId),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (playerId) REFERENCES Player(id),
    FOREIGN KEY (teamId) REFERENCES Team(id)
);