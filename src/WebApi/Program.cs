using System.Threading.RateLimiting;
using DatabaseAccess;
using DatabaseAccess.WebAdminRepository;
using DatabaseAccess.WebBookmakerOddsRepository;
using DatabaseAccess.WebGameOddsRepository;
using DatabaseAccess.WebTeamRepository;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using WebApi.BusinessLogic.AdminService;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.BusinessLogic.JobService;
using WebApi.BusinessLogic.StartupCacheWarmer;
using WebApi.BusinessLogic.StrategyBacktester;
using WebApi.BusinessLogic.TeamGetter;
using WebApi.Caching;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

string? _connectionString = Environment.GetEnvironmentVariable("NHL_DATABASE");
if (_connectionString == null)
{
    var config = new ConfigurationBuilder().AddJsonFile("appsettings.Local.json").Build();
    _connectionString = config.GetConnectionString("NHL_DATABASE");
}
if (_connectionString == null)
    throw new Exception("Connection String Null");

// Add services to the container
builder.Services.AddMemoryCache();
builder.Services.AddKeyedSingleton(ApiCache.ServiceKey, (_, _) => ApiCache.Create());
builder.Services.AddSingleton<IJobService, JobService>();
builder.Services.AddHostedService<StartupCacheWarmer>();
if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<LocalJobRunner>();
builder.Services.AddScoped<ITeamGetter, TeamGetter>();
builder.Services.AddScoped<IGameOddsGetter, GameOddsGetter>();
builder.Services.AddScoped<IStrategyBacktester, StrategyBacktester>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IWebBookmakerOddsRepository, WebBookmakerOddsRepository>();
builder.Services.AddScoped<IGameOddsRepository, GameOddsRepository>();
builder.Services.AddScoped<IWebAdminRepository, WebAdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddDbContext<GameDbContext>(x => x.UseNpgsql(_connectionString));
builder.Services.AddLogging();
// Prometheus scrapes a separate port so /metrics is never reachable through nginx/Cloudflare
builder.Services.AddMetricServer(options => options.Port = 9091);

builder.Services.AddRateLimiter(options =>
{
    static string GetClientIp(Microsoft.AspNetCore.Http.HttpContext ctx) =>
        ctx.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
        ?? ctx.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 60,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 2,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    options.AddPolicy("mcp", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 30,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 1,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    options.RejectionStatusCode = 429;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
// Mirrors getCurrentSeason in frontend/src/utils/season.ts: a new season starts on September 20
static int GetCurrentSeason(DateTime date) =>
    date >= new DateTime(date.Year, 9, 20) ? date.Year : date.Year - 1;

static string BuildMcpInstructions(int currentSeason) => $"""
        This server provides NHL game data, ML model predictions, and bookmaker odds.

        Season years use the start year of the season (e.g. 2024 for the 2024-25 season).
        The current season is {currentSeason}.

        Tool guidance:
        - Use GetTodaysGames first when the user asks about today's games or upcoming matchups.
        - Use GetGamesInDateRange for historical results or a specific week/range.
        - Use GetAllTeams for standings, model accuracy comparisons, or league-wide stats (no games).
        - Use GetTeamStats when the user asks about a specific team — it includes full game history.

        Odds are win probabilities (0–1). Log loss measures prediction accuracy — lower is better;
        random guessing scores ~0.693.
        """;

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new() { Name = "nhl-odds", Version = "1.0" };
})
    .WithHttpTransport(transport =>
    {
        // No server-side sessions: with 2 webapi replicas behind nginx, a session held in one pod's
        // memory 404'd ("Session not found") whenever a request landed on the other pod. The tools
        // are plain request/response, so nothing needs per-session state.
        transport.Stateless = true;
        // Built per session so the current season stays correct without a restart
        transport.ConfigureSessionOptions = (_, options, _) =>
        {
            options.ServerInstructions = BuildMcpInstructions(GetCurrentSeason(DateTime.UtcNow));
            return Task.CompletedTask;
        };
    })
    .WithToolsFromAssembly();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.WithOrigins("http://10.0.0.19:8081");
                          builder.WithOrigins("http://192.168.1.19:8081");
                          builder.WithOrigins("http://localhost:8081");
                          builder.WithOrigins("http://localhost:5173");
                      });
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors(MyAllowSpecificOrigins);

// HTTPS is handled by the reverse proxy (nginx/Cloudflare), not the API
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Before the rate limiter so 429s are counted; labels requests by route template
app.UseHttpMetrics();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");
app.MapMcp("/mcp").RequireRateLimiting("mcp");

app.Run();