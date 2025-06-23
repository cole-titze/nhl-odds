using Entities.Types.Enums;

namespace Entities.Models.GamePlayEvents
{
    public interface IGameEvent
    {
        public int id { get; set; }
        public int periodNumber { get; set; }
        public PeriodType periodType { get; set; }
        public int situationCode { get; set; }
        public int typeCode { get; set; }
        public int sortOrder { get; set; }
        public HomeTeamDefendingSide homeTeamDefendingSide { get; set; }
        public string eventTypeName { get; set; }
        public int secondsIntoPeriod { get; set; }
        public int secondsLeftInPeriod { get; set; }
    }
}
