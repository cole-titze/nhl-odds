using System.ComponentModel.DataAnnotations;

namespace Entities.DbModels
{
    public enum Role
    {
        Referee,
        Linesman,
    }
    public class DbGameOfficial
    {
        [Key]
        public string name { get; set; } = string.Empty;
        public Role role { get; set; } = Role.Referee;
    }
}