using Entities.ViewModels;

namespace WebApi.BusinessLogic.JobService;

public interface IJobService
{
    JobInfoVM GetStatus(string jobName);
    IEnumerable<JobInfoVM> GetAllStatuses();
    bool TryStart(string jobName, string command, string args, string workingDirectory, Dictionary<string, string>? environmentVariables = null);
    Task WaitForCompletion(string jobName, CancellationToken cancellationToken = default);
}
