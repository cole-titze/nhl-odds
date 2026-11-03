using Entities.Models.Web;
using WebApi.BusinessLogic.ClientErrors;

namespace WebApi.Mappers;

public static class ClientErrorReportToErrorLogMapper
{
    /// <summary>
    /// Maps a browser error to an ErrorLog row under the current season, so the Admin page lists it with that
    /// season's errors. The kind, build version, page and user agent go at the top of the stack trace. Long fields are cut short.
    /// </summary>
    public static ErrorLog Map(ClientErrorReport report, string? userAgent, DateTime nowUtc)
    {
        var name = string.IsNullOrWhiteSpace(report.Name) ? "Error" : report.Name.Trim();
        var details = $"Kind: {report.Kind}\n"
            + $"Version: {Truncate(report.Version ?? "unknown", ClientErrorLimits.MaxVersionChars)}\n"
            + $"Page: {Truncate(report.Page ?? "", ClientErrorLimits.MaxPageChars)}\n"
            + $"User agent: {Truncate(userAgent ?? "", ClientErrorLimits.MaxUserAgentChars)}";
        var stack = string.IsNullOrWhiteSpace(report.Stack) ? details : details + "\n\n" + report.Stack;

        return new ErrorLog
        {
            TimestampUTC = nowUtc,
            SeasonStartYear = GetSeasonStartYear(nowUtc),
            ExceptionType = Truncate(name, ClientErrorLimits.MaxNameChars),
            Message = Truncate(report.Message.Trim(), ClientErrorLimits.MaxMessageChars),
            StackTrace = Truncate(stack, ClientErrorLimits.MaxStackTraceChars),
            Source = ClientErrorLimits.Source,
        };
    }

    // A new season starts on September 20 (as in Program.cs and frontend/src/utils/season.ts)
    private static int GetSeasonStartYear(DateTime date) =>
        date >= new DateTime(date.Year, 9, 20) ? date.Year : date.Year - 1;

    private static string Truncate(string value, int maxChars) =>
        value.Length <= maxChars ? value : value[..(maxChars - 1)] + "…";
}
