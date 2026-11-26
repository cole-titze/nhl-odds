using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class JobInfoToJobInfoVmMapper
{
    private static readonly TimeZoneInfo CentralTime = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    public static JobInfoVM Map(JobInfo jobInfo)
    {
        return new JobInfoVM
        {
            Id = jobInfo.Id,
            Name = jobInfo.Name,
            Status = jobInfo.Status,
            StartedAt = jobInfo.StartedAt,
            FinishedAt = jobInfo.FinishedAt,
            Error = jobInfo.Error,
            Output = jobInfo.Output,
            CompletedToday = jobInfo.Status == "completed"
                && jobInfo.FinishedAt.HasValue
                && TimeZoneInfo.ConvertTimeFromUtc(jobInfo.FinishedAt.Value, CentralTime).Date
                    == TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTime).Date,
        };
    }
}
