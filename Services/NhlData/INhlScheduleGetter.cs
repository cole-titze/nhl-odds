namespace Services.NhlData
{
    public interface INhlScheduleGetter
	{
        Task<int> GetGameCountInSeason(int year);
        IDictionary<int, int> GetSeasonGameCounts();
        //Task<List<int>> GetTeamsForSeason(int seasonStartYear);
    }
}

