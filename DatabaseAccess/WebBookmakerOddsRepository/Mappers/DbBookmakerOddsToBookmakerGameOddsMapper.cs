using Entities.DbModels;
using Entities.Models.Web;

namespace DatabaseAccess.WebBookmakerOddsRepository.Mappers;

public static class DbBookmakerOddsToBookmakerGameOddsMapper
{
    public static BookmakerGameOdds Map(
        DbBookmakerOdds dbOdds,
        DbBookmakerSpreads? dbSpread,
        DbBookmakerTotals? dbTotal)
    {
        var odds = new BookmakerGameOdds
        {
            BookmakerName = dbOdds.BookmakerName,
            HomeOdds = dbOdds.HomeOdds,
            AwayOdds = dbOdds.AwayOdds,
        };

        if (dbSpread != null)
        {
            odds.HomePoint = dbSpread.HomePoint;
            odds.HomePrice = dbSpread.HomePrice;
            odds.AwayPoint = dbSpread.AwayPoint;
            odds.AwayPrice = dbSpread.AwayPrice;
        }

        if (dbTotal != null)
        {
            odds.OverUnderPoint = dbTotal.OverUnderPoint;
            odds.OverPrice = dbTotal.OverPrice;
            odds.UnderPrice = dbTotal.UnderPrice;
        }

        return odds;
    }
}