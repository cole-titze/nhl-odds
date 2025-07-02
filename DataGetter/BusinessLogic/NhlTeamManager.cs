using DatabaseAccess.TeamRepository;
using Entities.Models.Teams;
using Entities.Types;
using Entities.Types.Enums;
using Microsoft.Extensions.Logging;

using Services.NhlData;

namespace DataGetter.BusinessLogic;

public class NhlTeamManager
{
    private readonly ITeamRepository _teamRepo;
    private readonly NhlDataGetter _nhlDataGetter;
    private readonly ILogger<NhlGameManager> _logger;
    public NhlTeamManager(ITeamRepository teamRepository, NhlDataGetter nhlDataGetter, ILoggerFactory loggerFactory)
    {
        _teamRepo = teamRepository;
        _nhlDataGetter = nhlDataGetter;
        _logger = loggerFactory.CreateLogger<NhlGameManager>();
    }
    /// <summary>
    /// Gets all nhl teams within the season range and saves them to the database.
    /// </summary>
    /// <param name="seasonYearRange">Years to get teams for</param>
    /// <param name="mode">Whether to update existing records</param>
    public async Task GetTeamData(YearRange seasonYearRange, ModeType mode)
    {
        int totalTeamsAdded = 0;
        for (int seasonStartYear = seasonYearRange.StartYear; seasonStartYear <= seasonYearRange.EndYear; seasonStartYear++)
        {
            // Determines if data is already found and season can be skipped
            // If update mode then always rerun games to get new data fields
            var hasAllSeasonTeams = await _teamRepo.HasSeasonTeams(seasonStartYear);
            if (hasAllSeasonTeams && mode != ModeType.Update)
            {
                _logger.LogInformation("All team data for season " + seasonStartYear.ToString() + " already exists. Skipping...");
                continue;
            }

            var addedTeamCount = await FetchAndSaveNhlTeamData(seasonStartYear);
            await _teamRepo.Commit();

            totalTeamsAdded += addedTeamCount;
            _logger.LogInformation("Number of Teams Added To Season " + seasonStartYear.ToString() + ": " + addedTeamCount.ToString());
        }

        _logger.LogInformation("Number of Teams Added: " + totalTeamsAdded.ToString());
    }

    /// <summary>
    /// Gets all nhl teams within the season range.
    /// </summary>
    /// <param name="seasonYearRange">The years to get data for</param>
    /// <returns>Count saved</returns>
    private async Task<int> FetchAndSaveNhlTeamData(int seasonStartYear)
    {
        var teams = await GetAllTeams();
        await _teamRepo.AddUpdateTeams(teams);

        var seasonTeams = await GetSeasonTeams(seasonStartYear);
        teams = BuildTeams(teams, seasonTeams);

        await _teamRepo.AddUpdateSeasonTeams(teams);

        return teams.Count() + seasonTeams.Count();
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
