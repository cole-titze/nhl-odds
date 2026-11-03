using Entities.DbModels;
using Entities.Models.Web;

namespace DatabaseAccess.WebErrorLogRepository.Mappers;

public static class ErrorLogToDbErrorLogMapper
{
    public static DbErrorLog Map(ErrorLog error)
    {
        return new DbErrorLog
        {
            TimestampUTC = error.TimestampUTC,
            GameId = error.GameId,
            SeasonStartYear = error.SeasonStartYear,
            ExceptionType = error.ExceptionType,
            Message = error.Message,
            StackTrace = error.StackTrace,
            Source = error.Source,
        };
    }
}
