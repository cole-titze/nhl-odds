using Entities.Models.Web;

namespace DatabaseAccess.WebLineupRepository;

public interface IWebLineupRepository
{
    /// <summary>The newest projection saved before puck drop, or null if there isn't one</summary>
    Task<GameLineup?> GetGameLineup(int gameId);
}
