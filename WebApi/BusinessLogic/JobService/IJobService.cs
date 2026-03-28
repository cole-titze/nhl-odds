using Entities.Models.Web;

namespace WebApi.BusinessLogic.JobService;

public interface IJobService
{
    JobInfo GetStatus(string jobName);
    IEnumerable<JobInfo> GetAllStatuses();
    bool RequestJob(string jobName);
}
