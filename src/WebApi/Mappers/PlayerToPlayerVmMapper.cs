using Entities.Models.Web;
using Entities.Types;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class PlayerToPlayerVmMapper
{
    public static PlayerVM Map(PlayerProfile player, IEnumerable<PlayerGameLine> lines, int seasonStartYear)
    {
        var games = lines
            .Where(l => l.Game.HasBeenPlayed)
            .OrderByDescending(l => l.Game.GameDate)
            .ThenByDescending(l => l.Game.Id)
            .ToList();
        return new PlayerVM
        {
            Id = player.Id,
            FirstName = player.FirstName,
            LastName = player.LastName,
            HeadShot = player.HeadShot,
            Position = player.Position is { } position ? PositionCode(position) : null,
            SweaterNumber = player.SweaterNumber,
            BirthDate = player.BirthDate?.ToString("yyyy-MM-dd"),
            BirthCity = player.BirthCity,
            BirthStateProvince = player.BirthStateProvince,
            HeightInInches = player.HeightInInches,
            WeightInPounds = player.WeightInPounds,
            CurrentTeam = player.CurrentTeam is { } team ? MapTeam(team) : null,
            SeasonStartYear = seasonStartYear,
            Games = games.Select(MapGame).ToList(),
            SeasonTotals = MapTotals(games),
        };
    }

    /// <summary>The NHL's position letter: C, L, R, D or G.</summary>
    public static string PositionCode(POSITION position) => position switch
    {
        POSITION.Center => "C",
        POSITION.LeftWing => "L",
        POSITION.RightWing => "R",
        POSITION.Defenseman => "D",
        POSITION.Goalie => "G",
        _ => string.Empty,
    };

    public static PlayerTeamVM MapTeam(Team team) => new()
    {
        Id = team.Id,
        LocationName = team.LocationName,
        TeamName = team.TeamName,
        LogoUri = team.LogoUri,
    };

    public static PlayerGameStatsVM MapStats(PlayerGameLine line) => new()
    {
        TimeOnIceSeconds = line.TimeOnIceSeconds,
        Goals = line.Goals,
        Assists = line.Assists,
        Points = line.Goals + line.Assists,
        ShotsOnGoal = line.ShotsOnGoal,
        PlusMinus = line.PlusMinus,
        ShotsAgainst = line.ShotsAgainst,
        Saves = line.Saves,
        GoalsAgainst = line.GoalsAgainst,
        SavePct = line.ShotsAgainst > 0 ? (double)line.Saves / line.ShotsAgainst : null,
        IsStarter = line.IsStarter,
    };

    private static PlayerGameVM MapGame(PlayerGameLine line)
    {
        var game = line.Game;
        var isHome = game.HomeTeam.Id == line.TeamId;
        return new PlayerGameVM
        {
            GameId = game.Id,
            GameDate = game.GameDate,
            GameType = (int)game.GameType,
            TeamId = line.TeamId,
            IsHome = isHome,
            Opponent = MapTeam(isHome ? game.AwayTeam : game.HomeTeam),
            TeamGoals = isHome ? game.HomeGoals : game.AwayGoals,
            OpponentGoals = isHome ? game.AwayGoals : game.HomeGoals,
            Result = game.IsWin(line.TeamId) == 1 ? "W" : game.IsOvertimeLoss(line.TeamId) == 1 ? "OTL" : "L",
            Stats = MapStats(line),
        };
    }

    private static PlayerSeasonTotalsVM MapTotals(List<PlayerGameLine> games)
    {
        var toi = games.Sum(g => g.TimeOnIceSeconds);
        var goals = games.Sum(g => g.Goals);
        var assists = games.Sum(g => g.Assists);
        var shotsAgainst = games.Sum(g => g.ShotsAgainst);
        var saves = games.Sum(g => g.Saves);
        var goalsAgainst = games.Sum(g => g.GoalsAgainst);
        var isGoalie = games.Count > 0 && games.All(g => g.IsGoalie);
        return new PlayerSeasonTotalsVM
        {
            GamesPlayed = games.Count,
            TimeOnIceSeconds = toi,
            Goals = goals,
            Assists = assists,
            Points = goals + assists,
            ShotsOnGoal = games.Sum(g => g.ShotsOnGoal),
            PlusMinus = games.Sum(g => g.PlusMinus),
            ShotsAgainst = shotsAgainst,
            Saves = saves,
            GoalsAgainst = goalsAgainst,
            SavePct = shotsAgainst > 0 ? (double)saves / shotsAgainst : null,
            GoalsAgainstAverage = isGoalie && toi > 0 ? goalsAgainst * 3600.0 / toi : null,
        };
    }
}
