using Entities.DbModels;
using Entities.Models;

namespace Entities.Mappers.GameMappers;

public static class MapDbGameCoachToCoach
{
    public static Coach Map(DbGameCoach? dbGameCoach)
    {
        if (dbGameCoach == null)
        {
            throw new ArgumentNullException(nameof(dbGameCoach), "DbGameCoach cannot be null.");
        }

        return new Coach()
        {
            Name = dbGameCoach.Name,
        };
    }
}

