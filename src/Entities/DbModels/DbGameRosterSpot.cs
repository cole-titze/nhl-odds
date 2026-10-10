namespace Entities.DbModels;

/// <summary>A player dressed for a game (play-by-play rosterSpots): his team, sweater number and position that night.</summary>
public class DbGameRosterSpot
{
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public short? SweaterNumber { get; set; }
    // C, L, R, D or G
    public string PositionCode { get; set; } = string.Empty;
}