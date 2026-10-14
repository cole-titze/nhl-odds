namespace Entities.ViewModels;

/// <summary>One book's odds for a game, loaded when that book is picked on the Teams page.</summary>
public class SeasonBookmakerOddsVM
{
    public int GameId { get; set; }
    public double HomeOdds { get; set; }
    public double AwayOdds { get; set; }
}