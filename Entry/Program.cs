using Entities.Types.Enums;
using Entry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

ServiceProvider serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole()
        )
    .BuildServiceProvider();

var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

var dataGetter = new DataGetterEntry(loggerFactory);
var settings = new ModeSettings()
{
    ConnectionString = Environment.GetEnvironmentVariable("NHL_DATABASE") ?? string.Empty,
    Mode = ModeTypeParser.ParseFromString(Environment.GetEnvironmentVariable("RUN_MODE")),
    OddsApiKey = Environment.GetEnvironmentVariable("ODDS_API_KEY") ?? string.Empty,
};

if (settings.ConnectionString.IsNullOrEmpty())
{
    var config = new ConfigurationBuilder().AddJsonFile("appsettings.Local.json").Build();
    settings.Mode = ModeTypeParser.ParseFromString(config["ModeSettings:RUN_MODE"]);
    settings.ThrottleTimeMs = int.Parse(config["ModeSettings:THROTTLE_TIME_MS"] ?? "0");
    settings.ConnectionString = config.GetConnectionString("NHL_DATABASE") ?? string.Empty;
    if (string.IsNullOrEmpty(settings.OddsApiKey))
        settings.OddsApiKey = config["OddsApi:API_KEY"] ?? string.Empty;
}

if (settings.ConnectionString.IsNullOrEmpty())
    throw new Exception("Connection String Null");

await dataGetter.Main(settings);