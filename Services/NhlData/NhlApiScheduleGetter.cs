using Entities.Models.Teams;
using Entities.ServiceModels;
using Microsoft.Extensions.Logging;
using Services.RequestMaker;

namespace Services.NhlData;

public class NhlApiScheduleGetter : INhlScheduleGetter
{
    private readonly IRequestMaker _requestMaker;
    private readonly ILogger<NhlApiScheduleGetter> _logger;
    private readonly IDictionary<int, int> _seasonGameCountCache = new Dictionary<int, int>();

    public NhlApiScheduleGetter(IRequestMaker requestMaker, IDictionary<int, int> seasonGameCountCache, ILoggerFactory loggerFactory)
    {
        _requestMaker = requestMaker;
        _logger = loggerFactory.CreateLogger<NhlApiScheduleGetter>();
        _seasonGameCountCache = seasonGameCountCache;
    }

    /// <summary>
    /// Calls to the NHL's api to get the schedule response.
    /// Parses the response to get the maximum id which is used as the count
    /// Example Request: 
    /// https://api.nhle.com/stats/rest/en/season
    /// </summary>
    /// <param name="seasonStartYear">year of season to use</param>
    /// <returns>number of games in the season</returns>
    public async Task<int?> GetGameCountInSeason(int seasonStartYear)
    {
        if (_seasonGameCountCache.TryGetValue(seasonStartYear, out int value))
            return value;

        string url = "https://api.nhle.com/stats/rest/en/season";
        var scheduleServiceResponse = new ServiceScheduleResponse(await _requestMaker.MakeRequest(url, ""));
        if (scheduleServiceResponse.response == null)
        {
            _logger.LogWarning("Failed to get game count for season: " + seasonStartYear.ToString());
            return null;
        }
        var seasonId = NhlApiDataGetter.GetFullSeasonId(seasonStartYear);
        _seasonGameCountCache[seasonStartYear] = scheduleServiceResponse.ScheduleResponseToGameCount(seasonId);

        return _seasonGameCountCache[seasonStartYear];
    }
    /// <summary>
    /// Gets the seasons game counts
    /// </summary>
    /// <returns>Dictionary of year mapped to game count</returns>
    public IDictionary<int, int> GetSeasonGameCounts()
    {
        return _seasonGameCountCache;
    }
    /// <summary>
    /// Gets teams for the given season
    /// Example Request:
    /// https://api-web.nhle.com/v1/standings/2025-01-01
    /// </summary>
    /// <param name="seasonStartYear">The season start year</param>
    /// <returns>The teams active for the season</returns>
    public async Task<IEnumerable<SeasonTeam>?> GetTeamsForSeason(int seasonStartYear)
    {
        // Due to Covid, the 2020 season started on January 13th 2021
        // 2012 lockout season started on January 19th 2013.
        // Jan 20th catches all years
        int standingYear = seasonStartYear + 1;
        string url = "https://api-web.nhle.com/v1/standings/" + standingYear + "-01-20";

        var standingsServiceResponse = new ServiceStandingsResponse(await _requestMaker.MakeRequest(url, ""));

        if (standingsServiceResponse.response == null)
        {
            _logger.LogWarning("Failed to get team standings for season: " + seasonStartYear.ToString());
            return null;
        }

        return standingsServiceResponse.StandingsResponseToSeasonTeams();
    }

    /// <summary>
    /// Gets teams from the abbreviations provided.
    /// Example Request:
    /// https://api.nhle.com/stats/rest/en/team
    /// </summary>
    /// <param name="abbreviations">List of team abbreviations</param>
    /// <returns>The teams for the abbreviations</returns>
    public async Task<IEnumerable<Team>?> GetAllTeams()
    {
        string url = "https://api.nhle.com/stats/rest/en/team";

        var standingsServiceResponse = new ServiceAllTeamsResponse(await _requestMaker.MakeRequest(url, ""));

        if (standingsServiceResponse.response == null)
        {
            _logger.LogWarning("Failed to get team information from NHL API.");
            return null;
        }

        return standingsServiceResponse.AllTeamsResponseToTeams();
    }
}

