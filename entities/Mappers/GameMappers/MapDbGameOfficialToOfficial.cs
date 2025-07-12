using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapDbGameOfficialToLinesmen
{
    public static Linesman Map(DbGameOfficial? dbGameOfficial)
    {
        if (dbGameOfficial == null)
        {
            throw new ArgumentNullException(nameof(dbGameOfficial), "DbGameOfficial cannot be null.");
        }

        return new Linesman()
        {
            Name = dbGameOfficial.Name,
        };
    }

    public static IEnumerable<Linesman> MapList(IEnumerable<DbGameOfficial> dbGameOfficials)
    {
        var linesmenList = new List<Linesman>();
        foreach (var dbGameOfficial in dbGameOfficials)
        {
            linesmenList.Add(Map(dbGameOfficial));
        }

        return linesmenList;
    }
}

