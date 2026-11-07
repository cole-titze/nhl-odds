using DatabaseAccess.WebTeamRepository.Mappers;
using Entities.DbModels;
using Entities.Models.Web;
using Entities.Types;
using Entities.Types.Enums;

namespace DatabaseAccess.WebPlayerRepository.Mappers;

/// <summary>The GameRaw columns the player queries read (not its large summary text columns).</summary>
internal sealed record GameRow(
    int Id,
    DateTime GameDateUTC,
    int SeasonStartYear,
    int HomeTeamId,
    int AwayTeamId,
    int HomeGoals,
    int AwayGoals,
    Winner Winner,
    PeriodType EndPeriod,
    bool HasBeenPlayed,
    GameType GameType);

internal static class GameRowToGameMapper
{
    public static Game Map(GameRow game, IReadOnlyDictionary<int, DbSeasonTeam> seasonTeams)
    {
        seasonTeams.TryGetValue(game.HomeTeamId, out var home);
        seasonTeams.TryGetValue(game.AwayTeamId, out var away);
        return new Game
        {
            Id = game.Id,
            GameDate = DateTime.SpecifyKind(game.GameDateUTC, DateTimeKind.Utc),
            SeasonStartYear = game.SeasonStartYear,
            HomeGoals = game.HomeGoals,
            AwayGoals = game.AwayGoals,
            Winner = game.Winner,
            EndPeriod = game.EndPeriod,
            HasBeenPlayed = game.HasBeenPlayed,
            GameType = game.GameType,
            HomeTeam = home != null ? DbSeasonTeamToTeamMapper.Map(home) : new Team { Id = game.HomeTeamId },
            AwayTeam = away != null ? DbSeasonTeamToTeamMapper.Map(away) : new Team { Id = game.AwayTeamId },
        };
    }
}
