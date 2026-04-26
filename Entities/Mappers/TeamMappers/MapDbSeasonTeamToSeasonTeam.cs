using Entities.DbModels;
using Entities.Models.Teams;

namespace Entities.Mappers.TeamMappers;

public static class MapDbSeasonTeamToSeasonTeam
{
    public static SeasonTeam Map(DbSeasonTeam dbSeasonTeam)
    {
        return new SeasonTeam()
        {
            SeasonStartYear = dbSeasonTeam.SeasonStartYear,
            CommonName = dbSeasonTeam.CommonName,
            Abbreviation = dbSeasonTeam.Abbreviation,
            Name = dbSeasonTeam.Name,
            LogoUri = dbSeasonTeam.LogoUri,
            Division = dbSeasonTeam.Division,
            DivisionAbbreviation = dbSeasonTeam.DivisionAbbreviation,
            Conference = dbSeasonTeam.Conference,
            ConferenceAbbreviation = dbSeasonTeam.ConferenceAbbreviation,
            PlaceName = dbSeasonTeam.PlaceName
        };
    }
}