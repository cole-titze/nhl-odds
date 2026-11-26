using Entities.DbModels;
using Entities.Models.Web;

namespace DatabaseAccess.WebAdminRepository.Mappers;

public static class DbErrorLogToErrorLogMapper
{
    public static ErrorLog Map(DbErrorLog db)
    {
        return new ErrorLog
        {
            Id = db.Id,
            TimestampUTC = db.TimestampUTC,
            GameId = db.GameId,
            SeasonStartYear = db.SeasonStartYear,
            ExceptionType = db.ExceptionType,
            Message = db.Message,
            StackTrace = db.StackTrace,
            Source = db.Source,
        };
    }
}
