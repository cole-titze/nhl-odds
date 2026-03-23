using Entities.Models.Web;

namespace WebApi.BusinessLogic.JobService;

public interface IJobService
{
    JobInfo GetStatus(string jobName);
    IEnumerable<JobInfo> GetAllStatuses();
    bool TryStart(string jobName, string command, string args, string workingDirectory, Dictionary<string, string>? environmentVariables = null);
    Task WaitForCompletion(string jobName, CancellationToken cancellationToken = default);
}
