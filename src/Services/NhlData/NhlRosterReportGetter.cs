using Entities.ServiceModels;
using Microsoft.Extensions.Logging;

namespace Services.NhlData;

public class NhlRosterReportGetter : INhlRosterReportGetter
{
    private const string REPORT_URL = "https://media.nhl.com/site/api/team/reports/roster/public";
    private readonly HttpClient _httpClient;
    private readonly ILogger<NhlRosterReportGetter> _logger;

    public NhlRosterReportGetter(ILoggerFactory loggerFactory)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _logger = loggerFactory.CreateLogger<NhlRosterReportGetter>();
    }

    public async Task<List<ServiceRosterReportEntry>> GetRosterReport()
    {
        try
        {
            var report = await _httpClient.GetStringAsync(REPORT_URL);
            return RosterReportParser.Parse(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch the NHL roster report");
            return [];
        }
    }
}