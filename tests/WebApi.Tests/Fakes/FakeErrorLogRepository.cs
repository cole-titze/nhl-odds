using DatabaseAccess.WebErrorLogRepository;
using Entities.Models.Web;

namespace WebApi.Tests.BusinessLogic.Fakes;

public class FakeErrorLogRepository(Exception? throwOnAdd = null) : IWebErrorLogRepository
{
    public List<ErrorLog> Added { get; } = new();

    public Task AddError(ErrorLog error, CancellationToken cancellationToken = default)
    {
        if (throwOnAdd != null)
            throw throwOnAdd;
        Added.Add(error);
        return Task.CompletedTask;
    }
}
