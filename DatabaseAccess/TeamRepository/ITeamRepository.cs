using Entities.DbModels;

namespace DatabaseAccess.TeamRepository
{
    public interface ITeamRepository
    {
        Task<DbTeam> GetTeam(int teamId);
    }
}

