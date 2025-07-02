CREATE TABLE [dbo].[GameTvBroadcaster]
(
    GameId INT NOT NULL,
    BroadcasterId INT NOT NULL,
    CONSTRAINT PK_GameTvBroadcaster PRIMARY KEY(GameId, BroadcasterId),
    FOREIGN KEY (GameId) REFERENCES GameRaw(Id),
    FOREIGN KEY (BroadcasterId) REFERENCES TvBroadcaster(Id)
);