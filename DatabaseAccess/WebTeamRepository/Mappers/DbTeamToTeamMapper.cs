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
            LocationName = dbTeam.PlaceName,
            LogoUri = dbTeam.LogoUri,
        };
    }
}