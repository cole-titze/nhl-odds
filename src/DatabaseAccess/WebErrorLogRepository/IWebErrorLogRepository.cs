using Entities.Models.Web;

namespace DatabaseAccess.WebErrorLogRepository;

public interface IWebErrorLogRepository
{
    /// <summary>Adds the error to ErrorLog and saves it.</summary>
    Task AddError(ErrorLog error, CancellationToken cancellationToken = default);
}
