namespace Entities.DbModels
{
    public class DbGameReports
    {
        public int gameId { get; set; }
        public string gameSummary { get; set; } = string.Empty;
        public string eventSummary { get; set; } = string.Empty;
        public string playByPlaySummary { get; set; } = string.Empty;
        public string faceoffSummary { get; set; } = string.Empty;
        public string faceoffComparisonSummary { get; set; } = string.Empty;
        public string rosterSummary { get; set; } = string.Empty;
        public string shotSummary { get; set; } = string.Empty;
        public string shiftChartSummary { get; set; } = string.Empty;
        public string toiAwaySummary { get; set; } = string.Empty;
        public string toiHomeSummary { get; set; } = string.Empty;
        public int threeMinuteRecapVideoId { get; set; }
        public int condensedGameVideoId { get; set; }
        public void Clone(DbGameReports gameReports)
        {
            gameId = gameReports.gameId;
            gameSummary = gameReports.gameSummary;
            eventSummary = gameReports.eventSummary;
            playByPlaySummary = gameReports.playByPlaySummary;
            faceoffSummary = gameReports.faceoffSummary;
            faceoffComparisonSummary = gameReports.faceoffComparisonSummary;
            rosterSummary = gameReports.rosterSummary;
            shotSummary = gameReports.shotSummary;
            shiftChartSummary = gameReports.shiftChartSummary;
            toiAwaySummary = gameReports.toiAwaySummary;
            toiHomeSummary = gameReports.toiHomeSummary;
            threeMinuteRecapVideoId = gameReports.threeMinuteRecapVideoId;
            condensedGameVideoId = gameReports.condensedGameVideoId;
        }
    }
}