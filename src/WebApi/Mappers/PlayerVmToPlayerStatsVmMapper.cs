using Entities.ViewModels;

namespace WebApi.Mappers;

/// <summary>The Player page's PlayerVM in the compact shape the MCP server and the assistant return.</summary>
public static class PlayerVmToPlayerStatsVmMapper
{
    private static readonly TimeZoneInfo Central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    public static PlayerStatsVM Map(PlayerVM player)
    {
        var isGoalie = player.Position == "G";
        var totals = player.SeasonTotals;
        var gp = totals.GamesPlayed;
        return new PlayerStatsVM
        {
            Id = player.Id,
            Name = $"{player.FirstName} {player.LastName}",
            Position = player.Position,
            SweaterNumber = player.SweaterNumber,
            BirthDate = player.BirthDate,
            CurrentTeamId = player.CurrentTeam?.Id,
            CurrentTeam = player.CurrentTeam is { } team ? $"{team.LocationName} {team.TeamName}" : null,
            IsInjuredReserve = player.IsInjuredReserve,
            LastPlayedFor = player.LastTeam is { } last && player.LastSeasonStartYear is { } season
                && (player.CurrentTeam?.Id != last.Id)
                ? $"{last.LocationName} {last.TeamName} ({season}-{(season + 1) % 100:00})"
                : null,
            SeasonStartYear = player.SeasonStartYear,
            SeasonTotals = Line(totals, isGoalie, 1),
            PerGameAverages = gp > 0 ? Line(totals, isGoalie, gp) : null,
            Games = player.Games.Select(g => Game(g, isGoalie)).ToList(),
        };
    }

    public static string Toi(double seconds)
    {
        var s = (int)Math.Round(seconds);
        return $"{s / 60}:{s % 60:00}";
    }

    // Totals divided by `per` (1 for totals, games played for averages); rates aren't divided
    private static PlayerStatsLineVM Line(PlayerSeasonTotalsVM t, bool isGoalie, int per)
    {
        double Per(int value) => Math.Round((double)value / per, 2);
        var line = new PlayerStatsLineVM { GamesPlayed = t.GamesPlayed, TimeOnIce = Toi((double)t.TimeOnIceSeconds / per) };
        if (isGoalie)
        {
            line.ShotsAgainst = Per(t.ShotsAgainst);
            line.Saves = Per(t.Saves);
            line.GoalsAgainst = Per(t.GoalsAgainst);
            line.SavePct = t.SavePct is { } pct ? Math.Round(pct, 3) : null;
            line.GoalsAgainstAverage = t.GoalsAgainstAverage is { } gaa ? Math.Round(gaa, 2) : null;
        }
        else
        {
            line.Goals = Per(t.Goals);
            line.Assists = Per(t.Assists);
            line.Points = Per(t.Points);
            line.ShotsOnGoal = Per(t.ShotsOnGoal);
            line.PlusMinus = Per(t.PlusMinus);
        }
        return line;
    }

    private static PlayerStatsGameVM Game(PlayerGameVM g, bool isGoalie)
    {
        var utc = DateTime.SpecifyKind(g.GameDate, DateTimeKind.Utc);
        var row = new PlayerStatsGameVM
        {
            GameId = g.GameId,
            Date = TimeZoneInfo.ConvertTimeFromUtc(utc, Central).ToString("yyyy-MM-dd"),
            Playoff = g.GameType == 3,
            OpponentId = g.Opponent.Id,
            Opponent = $"{g.Opponent.LocationName} {g.Opponent.TeamName}",
            Home = g.IsHome,
            Result = $"{g.Result} {g.TeamGoals}-{g.OpponentGoals}",
            TimeOnIce = Toi(g.Stats.TimeOnIceSeconds),
        };
        if (isGoalie)
        {
            row.ShotsAgainst = g.Stats.ShotsAgainst;
            row.Saves = g.Stats.Saves;
            row.GoalsAgainst = g.Stats.GoalsAgainst;
        }
        else
        {
            row.Goals = g.Stats.Goals;
            row.Assists = g.Stats.Assists;
            row.ShotsOnGoal = g.Stats.ShotsOnGoal;
            row.PlusMinus = g.Stats.PlusMinus;
        }
        return row;
    }
}
