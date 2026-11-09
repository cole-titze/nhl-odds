using Entities.DbModels;
using Entities.Models.Web;

namespace DatabaseAccess.WebTeamRepository.Mappers;

public static class DbSeasonTeamToTeamMapper
{
    public static Team Map(DbSeasonTeam dbTeam)
    {
        return new Team
        {
            Id = dbTeam.TeamId,
            TeamName = dbTeam.CommonName,
            LocationName = Place(dbTeam),
            LogoUri = dbTeam.LogoUri,
        };
    }

    /// <summary>
    /// The place part of the full name ("New York Rangers" → "New York"), since pages show it before the common
    /// name. The NHL's own placeName won't do: for the New York teams it's "NY Rangers"/"NY Islanders", which came
    /// out as "NY Rangers Rangers". Falls back to placeName when the full name doesn't end with the common name.
    /// </summary>
    public static string Place(DbSeasonTeam dbTeam)
    {
        var name = dbTeam.Name?.Trim() ?? "";
        var common = dbTeam.CommonName?.Trim() ?? "";
        if (common.Length > 0 && name.Length > common.Length
            && name.EndsWith(" " + common, StringComparison.OrdinalIgnoreCase))
            return name[..^(common.Length + 1)].TrimEnd();
        return dbTeam.PlaceName;
    }
}
