using DatabaseAccess.WebTrackRecordRepository;
using Entities.ViewModels;

namespace WebApi.BusinessLogic.TrackRecord;

public class TrackRecordGetter(IWebTrackRecordRepository repository) : ITrackRecordGetter
{
    public async Task<TrackRecordVM> GetTrackRecord(int seasonStartYear, bool liveOnly) =>
        TrackRecordCalculator.Calculate(seasonStartYear, liveOnly, await repository.GetPlayedGames(seasonStartYear));
}
