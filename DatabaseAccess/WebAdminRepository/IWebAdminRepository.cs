using Entities.Models.Web;

namespace DatabaseAccess.WebAdminRepository;

public interface IWebAdminRepository
{
    Task<List<ErrorLog>> GetErrorLogs(int? seasonStartYear);
    Task<List<SeasonHealthCheck>> GetHealthChecks();
}