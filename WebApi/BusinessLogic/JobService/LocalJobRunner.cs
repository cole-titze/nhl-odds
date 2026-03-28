using System.Diagnostics;
using DatabaseAccess;
using Microsoft.EntityFrameworkCore;

namespace WebApi.BusinessLogic.JobService;

/// <summary>
/// Polls for requested jobs and runs them as subprocesses.
/// Only registered in Development — in production the scheduler container handles this.
/// </summary>
public class LocalJobRunner : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LocalJobRunner> _logger;
    private readonly IConfiguration _configuration;

    private static readonly Dictionary<string, (string command, string args, Dictionary<string, string>? env)> JobCommands = new()
    {
        ["data-collection"] = ("dotnet", "run --project Entry", new() { { "RUN_MODE", "NhlAdd" } }),
        ["odds-fetch"] = ("dotnet", "run --project Entry", new() { { "RUN_MODE", "NextDayOdds" } }),
        ["odds-backfill"] = ("dotnet", "run --project Entry", new() { { "RUN_MODE", "BackfillOdds" } }),
        ["kalshi-fetch"] = ("dotnet", "run --project Entry", new() { { "RUN_MODE", "KalshiFetch" } }),
        ["kalshi-backfill"] = ("dotnet", "run --project Entry", new() { { "RUN_MODE", "BackfillKalshi" } }),
        ["prediction"] = ("python3", "-m game_predictor --mode predict", null),
        ["prediction-backfill"] = ("python3", "-m game_predictor --mode backfill", null),
    };

    public LocalJobRunner(IServiceScopeFactory scopeFactory, ILogger<LocalJobRunner> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Local job runner started — polling for requested jobs");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await PollAndRun(stoppingToken);
        }
    }

    private async Task PollAndRun(CancellationToken stoppingToken)
    {
        string? jobName;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            var row = await db.JobStatus.FirstOrDefaultAsync(j => j.Status == "requested", stoppingToken);
            if (row == null) return;
            jobName = row.JobName;
        }

        if (!JobCommands.TryGetValue(jobName, out var cmd))
        {
            _logger.LogWarning("Unknown job requested: {JobName}", jobName);
            return;
        }

        var repoRoot = GetRepoRoot();
        var command = cmd.command;

        if (command == "python3")
        {
            var venvPython = Path.Combine(repoRoot, ".venv", "bin", "python3");
            if (File.Exists(venvPython))
                command = venvPython;
        }

        _logger.LogInformation("Running requested job {JobName}: {Command} {Args}", jobName, command, cmd.args);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = cmd.args,
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.Environment["PYTHONUNBUFFERED"] = "1";
            if (cmd.env != null)
            {
                foreach (var (key, value) in cmd.env)
                    psi.Environment[key] = value;
            }

            using var process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogError("Failed to start process for job {JobName}", jobName);
                return;
            }

            try
            {
                await process.WaitForExitAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobName} threw an exception", jobName);
        }
    }

    private string GetRepoRoot()
    {
        var configured = _configuration["AdminSettings:RepoRoot"];
        if (!string.IsNullOrEmpty(configured))
            return configured;

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
