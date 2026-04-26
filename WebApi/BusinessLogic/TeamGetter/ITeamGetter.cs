using Entities.ViewModels;

namespace WebApi.BusinessLogic.TeamGetter;

public interface ITeamGetter
{
    Task<TeamsVM> GetAllTeamsStats(int seasonStartYear);
    Task<TeamVM> GetTeamStats(int teamId, int seasonStartYear);
}