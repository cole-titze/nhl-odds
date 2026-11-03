using Entities.Models.Web;

namespace DatabaseAccess.WebTrackRecordRepository;

public interface IWebTrackRecordRepository
{
    /// <summary>Every played game in the season that has a model prediction, oldest first</summary>
    Task<List<TrackRecordGame>> GetPlayedGames(int seasonStartYear);
}
