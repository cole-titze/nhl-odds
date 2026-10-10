using Entities.DbModels;

namespace DatabaseAccess.LineupArticleRepository;

public interface ILineupArticleRepository
{
    Task<bool> Exists(string contentHash);
    Task Add(DbLineupArticle article);
    /// <summary>Saved versions with no LineupArticleParse row, oldest first.</summary>
    Task<List<DbLineupArticle>> GetUnparsedVersions();
    Task<bool> SectionExists(string sectionHash);
    /// <summary>The newest season's teams: id, abbreviation and full name.</summary>
    Task<List<DbSeasonTeam>> GetLatestSeasonTeams();
    /// <summary>The first game between these teams starting in [fromUtc, toUtc], or null.</summary>
    Task<int?> FindGameId(int awayTeamId, int homeTeamId, DateTime fromUtc, DateTime toUtc);
    /// <summary>Every player's id and name.</summary>
    Task<List<(int Id, string FirstName, string LastName)>> GetPlayerNames();
    /// <summary>Saves a version's new sections and players and marks it parsed.</summary>
    Task AddParsed(IEnumerable<DbLineupGame> games, IEnumerable<DbLineupPlayer> players, DbLineupArticleParse parse);
    Task AddParseFailure(DbLineupArticleParse parse);
}