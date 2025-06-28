using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Entry;
using Microsoft.IdentityModel.Tokens;
using Entities.Types.Enums;

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
    connectionString = Environment.GetEnvironmentVariable("NHL_DATABASE") ?? string.Empty,
    mode = ModeTypeParser.ParseFromString(Environment.GetEnvironmentVariable("RUN_MODE")),
};

if (settings.connectionString.IsNullOrEmpty())
{
    var config = new ConfigurationBuilder().AddJsonFile("appsettings.Local.json").Build();
    settings = config.GetSection("ModeSettings").Get<ModeSettings>() ?? new ModeSettings();
    settings.connectionString = config.GetConnectionString("NHL_DATABASE") ?? string.Empty;
}

if (settings.connectionString.IsNullOrEmpty())
    throw new Exception("Connection String Null");

await dataGetter.Main(settings);