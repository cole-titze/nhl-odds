using DatabaseAccess.WebErrorLogRepository.Mappers;
using Entities.Models.Web;

namespace DatabaseAccess.WebErrorLogRepository;

/// <summary>Writes to ErrorLog from the web API (errors reported by visitors' browsers).</summary>
public class WebErrorLogRepository(GameDbContext db) : IWebErrorLogRepository
{
    public async Task AddError(ErrorLog error, CancellationToken cancellationToken = default)
    {
        db.ErrorLog.Add(ErrorLogToDbErrorLogMapper.Map(error));
        await db.SaveChangesAsync(cancellationToken);
    }
}
