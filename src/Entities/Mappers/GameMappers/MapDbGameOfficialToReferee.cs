using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapDbGameOfficialToReferee
{
    public static Referee Map(DbGameOfficial? dbGameOfficial)
    {
        if (dbGameOfficial == null)
        {
            throw new ArgumentNullException(nameof(dbGameOfficial), "DbGameOfficial cannot be null.");
        }

        return new Referee()
        {
            Name = dbGameOfficial.Name,
        };
    }

    public static IEnumerable<Referee> MapList(IEnumerable<DbGameOfficial> dbGameOfficials)
    {
        var refereesList = new List<Referee>();
        foreach (var dbGameOfficial in dbGameOfficials)
        {
            refereesList.Add(Map(dbGameOfficial));
        }
        return refereesList;
    }
}