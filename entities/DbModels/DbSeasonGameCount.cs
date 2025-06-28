using System.ComponentModel.DataAnnotations;

namespace Entities.DbModels;

public class DbSeasonGameCount
{
    [Key]
    public int SeasonId { get; set; }
    public int GameCount { get; set; }
}