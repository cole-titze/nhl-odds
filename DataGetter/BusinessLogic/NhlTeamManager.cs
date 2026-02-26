using DatabaseAccess.TeamRepository;
using Entities.Models.Teams;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;

using Services.NhlData;

namespace DataGetter.BusinessLogic;

public class NhlTeamManager
{
    private readonly ITeamRepository _teamRepo;
    private readonly NhlApiDataGetter _nhlDataGetter;
    private readonly ILogger<NhlTeamManager> _logger;
    public NhlTeamManager(ITeamRepository teamRepository, NhlApiDataGetter nhlDataGetter, ILoggerFactory loggerFactory)
    {
        _teamRepo = teamRepository;
        _nhlDataGetter = nhlDataGetter;
        _logger = loggerFactory.CreateLogger<NhlTeamManager>();
    }
    /// <summary>
    /// Gets all nhl teams within the season range and saves them to the database.
    /// </summary>
    /// <param name="seasonStartYear">Year to get season data for</param>
    /// <param name="mode">Whether to update existing records, or only add new teams</param>
    public async Task<IEnumerable<Team>> GetTeamData(int seasonStartYear, ModeType mode)
    {
        var hasAllSeasonTeams = await _teamRepo.HasSeasonTeams(seasonStartYear);
        var teams = await _teamRepo.GetSeasonTeams(seasonStartYear);
        if (hasAllSeasonTeams && mode != ModeType.Update)
        {
            _logger.LogInformation("All team data for season " + seasonStartYear.ToString() + " already exists. Skipping...");
            return teams;
        }

        teams = await GetAllTeams();
        var seasonTeams = await GetSeasonTeams(seasonStartYear);
        teams = BuildTeams(teams, seasonTeams);

        return teams;
    }

    /// <summary>
    ///  Builds the teams from the list of teams and season teams.
    /// </summary>
    /// <param name="teams">The teams to add to</param>
    /// <param name="seasonTeams">The season teams</param>
    /// <returns>The filled teams</returns>
    private IEnumerable<Team> BuildTeams(IEnumerable<Team> teams, IEnumerable<SeasonTeam> seasonTeams)
    {
        foreach (var team in teams)
        {
            team.SeasonInformation = seasonTeams
                .Where(x => x.Abbreviation == team.Abbreviation)
                .ToDictionary(x => x.SeasonStartYear, x => x);
        }

        return teams;
    }

    /// <summary>
    /// Gets all teams
    /// </summary>
    /// <param name="seasonTeams">Season teams</param>
    private async Task<IEnumerable<Team>> GetAllTeams()
    {
        var teamList = new List<Team>();
        _logger.LogInformation("Getting Teams from Season Teams");
        var teams = await _nhlDataGetter.ScheduleDataGetter.GetAllTeams();
        if (teams != null)
        {
            teamList.AddRange(teams);
        }

        return teamList;
    }

    /// <summary>
    /// Gets a seasons worth of teams. Only returns teams that have not already been found.
    /// </summary>
    /// <param name="seasonStartYear">The start year</param>
    /// <returns>The teams for the season</returns>
    private async Task<IEnumerable<SeasonTeam>> GetSeasonTeams(int seasonStartYear)
    {
        var seasonTeams = new List<SeasonTeam>();
        _logger.LogInformation("Getting Season Teams for season: " + seasonStartYear);
        var teams = await _nhlDataGetter.ScheduleDataGetter.GetTeamsForSeason(seasonStartYear);
        if (teams != null)
            seasonTeams.AddRange(teams);

        return seasonTeams;
    }
}
