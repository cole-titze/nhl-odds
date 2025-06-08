using System.Diagnostics.CodeAnalysis;
using Entities.DbModels;
using Entities.Models;
using Microsoft.VisualBasic;

namespace DatabaseAccess.GameRepository
{
    public interface IGameRepository
    {
        Task AddSeasonGameCounts(IDictionary<int, int> seasonGameCountCache);
        Task AddUpdateGames(IEnumerable<Game> seasonGames);
        Task AddUpdateTvBroadcasters(IEnumerable<Game> games);
        Task AddUpdateGameTvBroadcasters(IEnumerable<Game> games);
        Task Commit();
        Task<Game?> GetGame(int gameId);
        Task<int> GetGameCountInSeason(int seasonStartYear);
    }
}

