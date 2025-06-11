using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DbModels
{
    public enum Role
    {
        Referee,
        Linesman,
    }
    public class DbGameOfficial
    {
        public int gameId { get; set; }
        public string name { get; set; } = string.Empty;
        public Role role { get; set; } = Role.Referee;
        [ForeignKey("gameId")]
        public DbGameRaw? game { get; set; }

        public void Clone(DbGameOfficial gameOfficial)
        {
            gameId = gameOfficial.gameId;
            name = gameOfficial.name;
            role = gameOfficial.role;
        }
    }
}