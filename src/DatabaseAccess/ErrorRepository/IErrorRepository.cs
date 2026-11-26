using Entities.DbModels;

namespace DatabaseAccess.ErrorRepository;

public interface IErrorRepository
{
    Task AddError(DbErrorLog error);
}
