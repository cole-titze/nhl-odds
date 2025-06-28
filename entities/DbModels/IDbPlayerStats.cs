using Entities.Types;

namespace Entities.DbModels;

public interface IDbGamePlayerStats
{
    public int PlayerId { get; set; }
    public int GameId { get; set; }
    public int TimeOnIceSeconds { get; set; }
    public POSITION Position { get; set; }
    public void Clone(IDbGamePlayerStats gamePlayerStats);
}