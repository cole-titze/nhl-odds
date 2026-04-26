using Entities.Models.Web;
using Entities.ViewModels;

namespace WebApi.Mappers;

public static class ErrorLogToErrorLogVmMapper
{
    public static ErrorLogVM Map(ErrorLog errorLog)
    {
        return new ErrorLogVM
        {
            Id = errorLog.Id,
            TimestampUTC = errorLog.TimestampUTC,
            GameId = errorLog.GameId,
            SeasonStartYear = errorLog.SeasonStartYear,
            ExceptionType = errorLog.ExceptionType,
            Message = errorLog.Message,
            StackTrace = errorLog.StackTrace,
            Source = errorLog.Source,
        };
    }
}