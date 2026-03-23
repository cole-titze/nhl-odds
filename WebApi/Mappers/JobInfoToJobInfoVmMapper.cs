using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class JobInfoToJobInfoVmMapper
{
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
        };
    }
}
