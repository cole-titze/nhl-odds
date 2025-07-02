using Entities.Models.Teams;
using Entities.ServiceModels;
using Microsoft.Extensions.Logging;
using Services.RequestMaker;

namespace Services.NhlData;

public class NhlScheduleGetter : INhlScheduleGetter
{
    private readonly IRequestMaker _requestMaker;
    private readonly ILogger<NhlGameGetter> _logger;
    private readonly IDictionary<int, int> _seasonGameCountCache = new Dictionary<int, int>();
    private const int DEFAULT_GAME_COUNT = 1400;

    public NhlScheduleGetter(IRequestMaker requestMaker, IDictionary<int, int> seasonGameCountCache, ILoggerFactory loggerFactory)
    {
        _requestMaker = requestMaker;
        _logger = loggerFactory.CreateLogger<NhlGameGetter>();
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
    public async Task<int> GetGameCountInSeason(int seasonStartYear)
    {
        if (_seasonGameCountCache.TryGetValue(seasonStartYear, out int value))
            return value;

        string url = "https://api.nhle.com/stats/rest/en/season";
        var scheduleServiceResponse = new ServiceScheduleResponse(await _requestMaker.MakeRequest(url, ""));
        if (scheduleServiceResponse.response == null)
        {
            _logger.LogWarning("Schedule request failed, using default game count: " + DEFAULT_GAME_COUNT.ToString());
            return DEFAULT_GAME_COUNT;
        }
        var seasonId = NhlDataGetter.GetFullSeasonId(seasonStartYear);
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
        string url = "https://api-web.nhle.com/v1/standings/" + seasonStartYear + "-11-11";

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

