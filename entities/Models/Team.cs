namespace Entities.Models
{
    public class Team
    {
        public int id { get; set; }
        public string abbreviation { get; set; } = string.Empty;
        public string locationName { get; set; } = string.Empty;
        public string teamName { get; set; } = string.Empty;
        public string logoUri { get; set; } = string.Empty;

    }
}

