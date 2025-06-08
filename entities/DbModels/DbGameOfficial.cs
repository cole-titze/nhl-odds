using System;

namespace Entities.DbModels
{
    public enum Role
    {
        Referee,
        Linesman,
    }
    public class DbGameOfficial
    {
        public string name { get; set; } = string.Empty;
        public Role role { get; set; } = Role.Referee;
    }
}