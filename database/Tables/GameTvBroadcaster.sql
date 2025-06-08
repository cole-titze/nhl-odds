CREATE TABLE [dbo].[GameTvBroadcaster]
(
    gameId INT NOT NULL,
    broadcasterId INT NOT NULL,
    CONSTRAINT PK_GameTvBroadcaster PRIMARY KEY(gameId, broadcasterId),
    FOREIGN KEY (gameId) REFERENCES GameRaw(id),
    FOREIGN KEY (broadcasterId) REFERENCES GameTvBroadcaster(id),
);