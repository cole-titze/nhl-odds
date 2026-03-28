using Entities.Types.Enums;
using Entry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

ServiceProvider serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole()
        )
    .BuildServiceProvider();

var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

var dataGetter = new DataGetterEntry(loggerFactory);
var runModeEnv = Environment.GetEnvironmentVariable("RUN_MODE");
var settings = new ModeSettings()
{
    ConnectionString = Environment.GetEnvironmentVariable("NHL_DATABASE") ?? string.Empty,
    Mode = ModeTypeParser.ParseFromString(runModeEnv),
    OddsApiKey = Environment.GetEnvironmentVariable("ODDS_API_KEY") ?? string.Empty,
};

if (string.IsNullOrEmpty(settings.ConnectionString))
{
    var config = new ConfigurationBuilder().AddJsonFile("appsettings.Local.json").Build();
    if (runModeEnv == null)
        settings.Mode = ModeTypeParser.ParseFromString(config["ModeSettings:RUN_MODE"]);
    settings.ThrottleTimeMs = int.Parse(config["ModeSettings:THROTTLE_TIME_MS"] ?? "0");
    settings.ConnectionString = config.GetConnectionString("NHL_DATABASE") ?? string.Empty;
    if (string.IsNullOrEmpty(settings.OddsApiKey))
        settings.OddsApiKey = config["OddsApi:API_FUTURE_KEY"] ?? string.Empty;
    if (string.IsNullOrEmpty(settings.OddsApiBackfillKey))
        settings.OddsApiBackfillKey = config["OddsApi:API_BACKFILL_KEY"] ?? string.Empty;
}

if (string.IsNullOrEmpty(settings.ConnectionString))
    throw new Exception("Connection String Null");

await dataGetter.Main(settings);