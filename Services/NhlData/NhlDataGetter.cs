namespace Services.NhlData
{
    public class NhlDataGetter
    {
        public INhlGameGetter GameDataGetter;
        public INhlPlayerGetter PlayerDataGetter;
        public INhlScheduleGetter ScheduleDataGetter;
        public NhlDataGetter(INhlGameGetter gameGetter, INhlPlayerGetter playerGetter, INhlScheduleGetter scheduleGetter)
        {
            GameDataGetter = gameGetter;
            PlayerDataGetter = playerGetter;
            ScheduleDataGetter = scheduleGetter;
        }
        /// <summary>
        /// Builds a game id to the nhl api standard "year""gameType"""gameId"
        /// (ex. 2022020001    year=2022 gameType=02 (02 is regular season) gameId=0001)
        /// </summary>
        /// <param name="seasonstartYear">Year to use in id</param>
        /// <param name="gameNumber">Game number to use in id</param>
        /// <returns>The internal game id</returns>
        public static int GetGameId(int seasonstartYear, int gameNumber)
        {
            return (seasonstartYear * 1000000) + 20000 + gameNumber;
        }
        /// <summary>
        /// Gets the full season id used by the Nhl Api
        /// </summary>
        /// <param name="seasonStartYear"></param>
        /// <returns></returns>
        public static int GetFullSeasonId(int seasonStartYear)
        {
            int nextYear = seasonStartYear + 1;
            return (seasonStartYear * 10000) + nextYear;
        }
        public enum GameRequestType
        {
            /// <summary>
            /// Request type for game summary. This holds data like the team ID's and status
            /// </summary>
            GameSummary,
            /// <summary>
            /// Request type for game stats. This holds data like goals, shots on goal, and other stats
            /// </summary>
            GameStats,
            /// <summary>
            /// Request type for game play by play events. This holds data like the events that happened in the game
            /// </summary>
            GameEvents
        }
        /// <summary>
        /// Creates the game query
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requestType">The type of request to make</param>
        /// <returns>Game query string</returns>
        public static string GetGameQuery(int id, GameRequestType requestType)
        {
            string urlParameters = string.Empty;
            switch (requestType)
            {
                case GameRequestType.GameSummary:
                    urlParameters = $"{id}/boxscore";
                    break;
                case GameRequestType.GameStats:
                    urlParameters = $"{id}/right-rail";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null);
            }

            return urlParameters;
        }
        /// <summary>
        /// Creates the game query
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requestType">The type of request to make</param>
        /// <returns>Game query string</returns>
        public static string GetPlayerQuery(int playerId)
        {
            return $"{playerId}/landing";
        }
    }
}
