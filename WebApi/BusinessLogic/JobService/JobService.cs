using System.Collections.Concurrent;
using DatabaseAccess;
using Entities.DbModels;
using Entities.Models.Web;
using Microsoft.EntityFrameworkCore;

namespace WebApi.BusinessLogic.JobService;

public class JobService : IJobService
{
    private readonly ILogger<JobService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public JobService(ILogger<JobService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public JobInfo GetStatus(string jobName)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var row = db.JobStatus.Find(jobName);
            if (row != null)
            {
                return new JobInfo
                {
                    Id = row.JobName,
                    Name = row.JobName,
                    Status = row.Status,
                    StartedAt = row.StartedAt,
                    FinishedAt = row.FinishedAt,
                    Error = row.Error,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load job status for {JobName}", jobName);
        }

        return new JobInfo { Id = jobName, Name = jobName };
    }

    public IEnumerable<JobInfo> GetAllStatuses()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            return db.JobStatus.Select(row => new JobInfo
            {
                Id = row.JobName,
                Name = row.JobName,
                Status = row.Status,
                StartedAt = row.StartedAt,
                FinishedAt = row.FinishedAt,
                Error = row.Error,
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load job statuses from database");
            return [];
        }
    }

    public bool RequestJob(string jobName)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var existing = db.JobStatus.Find(jobName);

            if (existing != null)
            {
                if (existing.Status == "running" || existing.Status == "requested")
                    return false;

                existing.Status = "requested";
                existing.StartedAt = DateTime.UtcNow;
                existing.FinishedAt = null;
                existing.Error = null;
            }
            else
            {
                db.JobStatus.Add(new DbJobStatus
                {
                    JobName = jobName,
                    Status = "requested",
                    StartedAt = DateTime.UtcNow,
                    FinishedAt = null,
                    Error = null,
                });
            }

            db.SaveChanges();
            _logger.LogInformation("Job {JobName} requested", jobName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request job {JobName}", jobName);
            return false;
        }
    }
}
