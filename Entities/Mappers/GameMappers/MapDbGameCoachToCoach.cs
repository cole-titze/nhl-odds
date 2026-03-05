using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapDbGameCoachToCoach
{
    public static Coach Map(DbGameCoach? dbGameCoach)
    {
        if (dbGameCoach == null)
        {
            return new Coach();
        }

        return new Coach()
        {
            Name = dbGameCoach.Name,
        };
    }
}

