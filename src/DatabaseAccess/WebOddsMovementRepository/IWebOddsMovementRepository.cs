using Entities.Models.Web;

namespace DatabaseAccess.WebOddsMovementRepository;

public interface IWebOddsMovementRepository
{
    /// <summary>The game's pre-game predictions and prices, or null if there's no such game</summary>
    Task<OddsMovementGame?> GetOddsMovement(int gameId);
}
