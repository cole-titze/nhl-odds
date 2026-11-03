using Entities.ViewModels;

namespace WebApi.BusinessLogic.TrackRecord;

public interface ITrackRecordGetter
{
    Task<TrackRecordVM> GetTrackRecord(int seasonStartYear, bool liveOnly);
}
