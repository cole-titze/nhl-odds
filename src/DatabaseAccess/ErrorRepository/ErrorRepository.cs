using Entities.DbModels;

namespace DatabaseAccess.ErrorRepository;

public class ErrorRepository : IErrorRepository
{
    private readonly NhlDbContext _context;

    public ErrorRepository(NhlDbContext context)
    {
        _context = context;
    }

    public async Task AddError(DbErrorLog error)
    {
        _context.ErrorLog.Add(error);
        await _context.SaveChangesAsync();
    }
}