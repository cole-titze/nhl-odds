using Entities.DbModels;
using Entities.Models.Web;

namespace DatabaseAccess.WebTeamRepository.Mappers;

public static class DbSeasonTeamToTeamMapper
{
    public static Team Map(DbSeasonTeam dbTeam)
    {
        return new Team
        {
            id = dbTeam.TeamId,
            teamName = dbTeam.CommonName,
            locationName = dbTeam.PlaceName,
            logoUri = dbTeam.LogoUri,
        };
    }
}
