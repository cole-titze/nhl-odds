using System.Collections.Concurrent;
using System.Diagnostics;
using Entities.ViewModels;
using Microsoft.Extensions.Hosting;

namespace WebApi.BusinessLogic.JobService;

public class JobService : IJobService
{
    private readonly ConcurrentDictionary<string, JobInfoVM> _jobs = new();
    private readonly ConcurrentDictionary<string, object> _locks = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _completionSources = new();
    private readonly ILogger<JobService> _logger;
    private readonly CancellationToken _appStopping;

    public JobService(ILogger<JobService> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _appStopping = lifetime.ApplicationStopping;
    }

    public JobInfoVM GetStatus(string jobName)
    {
        return _jobs.GetOrAdd(jobName, name => new JobInfoVM { Id = name, Name = name });
    }

    public IEnumerable<JobInfoVM> GetAllStatuses()
    {
        return _jobs.Values;
    }

    public bool TryStart(string jobName, string command, string args, string workingDirectory, Dictionary<string, string>? environmentVariables = null)
    {
        var lockObj = _locks.GetOrAdd(jobName, _ => new object());

        lock (lockObj)
        {
            var job = _jobs.GetOrAdd(jobName, name => new JobInfoVM { Id = name, Name = name });

            if (job.Status == "running")
                return false;

            job.Status = "running";
            job.StartedAt = DateTime.UtcNow;
            job.FinishedAt = null;
            job.Error = null;
            job.Output = string.Empty;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _completionSources[jobName] = tcs;

            _ = Task.Run(async () =>
            {
                await RunProcess(job, command, args, workingDirectory, environmentVariables);
                tcs.TrySetResult();
            });
            return true;
        }
    }

    public async Task WaitForCompletion(string jobName, CancellationToken cancellationToken = default)
    {
        if (_completionSources.TryGetValue(jobName, out var tcs))
            await tcs.Task.WaitAsync(cancellationToken);
    }

    private async Task RunProcess(JobInfoVM job, string command, string args, string workingDirectory, Dictionary<string, string>? environmentVariables = null)
    {
        try
        {
            _logger.LogInformation("Starting job {JobName}: {Command} {Args}", job.Name, command, args);

            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.Environment["PYTHONUNBUFFERED"] = "1";
            if (environmentVariables != null)
            {
                foreach (var (key, value) in environmentVariables)
                    psi.Environment[key] = value;
            }

            using var process = Process.Start(psi);
            if (process == null)
            {
                job.Status = "failed";
                job.Error = "Failed to start process";
                job.FinishedAt = DateTime.UtcNow;
                return;
            }

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    job.Output += e.Data + "\n";
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    job.Output += e.Data + "\n";
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(_appStopping);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                job.Status = "cancelled";
                job.Error = "Application shutting down";
                job.FinishedAt = DateTime.UtcNow;
                return;
            }

            if (process.ExitCode == 0)
            {
                job.Status = "completed";
                _logger.LogInformation("Job {JobName} completed successfully", job.Name);
            }
            else
            {
                job.Status = "failed";
                job.Error = $"Process exited with code {process.ExitCode}";
                _logger.LogError("Job {JobName} failed (exit code {ExitCode})", job.Name, process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            job.Status = "failed";
            job.Error = ex.Message;
            _logger.LogError(ex, "Job {JobName} threw an exception", job.Name);
        }
        finally
        {
            job.FinishedAt = DateTime.UtcNow;
        }
    }
}
