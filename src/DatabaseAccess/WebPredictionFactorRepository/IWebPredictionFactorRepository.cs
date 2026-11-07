using Entities.Models.Web;

namespace DatabaseAccess.WebPredictionFactorRepository;

public interface IWebPredictionFactorRepository
{
    /// <summary>The factors saved with the game's newest prediction (the one the site shows); null if that run has none.</summary>
    Task<PredictionFactors?> GetForLatestPrediction(int gameId);
}
