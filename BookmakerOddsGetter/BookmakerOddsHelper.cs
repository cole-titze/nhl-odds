using DatabaseAccess.BookmakerOddsRepository;
using Entities.ServiceModels.Mappers;
using Entities.ServiceModels.OddsApi;
using Microsoft.Extensions.Logging;

namespace BookmakerOddsGetter;

public static class BookmakerOddsHelper
{
    public static List<OddsApiResponseMapper.GameInfo> BuildGameInfoList(
        IEnumerable<GameRef> games,
        Dictionary<int, string> seasonTeams,
        Dictionary<int, DateTime> gameDatesByGameId)
    {
        var gameInfoList = new List<OddsApiResponseMapper.GameInfo>();
        foreach (var game in games)
        {
            if (seasonTeams.TryGetValue(game.HomeTeamId, out var homeName)
                && seasonTeams.TryGetValue(game.AwayTeamId, out var awayName)
                && gameDatesByGameId.TryGetValue(game.Id, out var dateUtc))
            {
                gameInfoList.Add(new OddsApiResponseMapper.GameInfo
                {
                    GameId = game.Id,
                    HomeTeamName = homeName,
                    AwayTeamName = awayName,
                    GameDateUTC = dateUtc,
                });
            }
        }
        return gameInfoList;
    }

    public static async Task MapAndSave(ILogger logger, IBookmakerOddsRepository repo,
        List<OddsApiResponse> responses, List<OddsApiResponseMapper.GameInfo> gameInfoList)
    {
        var mapped = OddsApiResponseMapper.Map(responses, gameInfoList);

        // Log which games matched
        var matchedGameIds = mapped.H2H.Select(h => h.GameId).Distinct().ToHashSet();
        foreach (var r in responses)
        {
            var matched = mapped.H2H.FirstOrDefault(h => h.OddsApiGameId == r.Id);
            if (matched != null)
                logger.LogInformation("  Matched: {Home} vs {Away} -> GameId {GameId}", r.HomeTeam, r.AwayTeam, matched.GameId);
            else
                logger.LogInformation("  No match: {Home} vs {Away} ({Commence})", r.HomeTeam, r.AwayTeam, r.CommenceTime.ToString("yyyy-MM-dd"));
        }

        if (!mapped.H2H.Any() && !mapped.Spreads.Any() && !mapped.Totals.Any())
        {
            logger.LogWarning("No bookmaker odds could be matched to games.");
            return;
        }

        // Deduplicate by composite key (same game+bookmaker can appear across date boundaries)
        var h2h = mapped.H2H
            .GroupBy(x => (x.GameId, x.BookmakerName))
            .Select(g => g.First())
            .ToList();
        var spreads = mapped.Spreads
            .GroupBy(x => (x.GameId, x.BookmakerName))
            .Select(g => g.First())
            .ToList();
        var totals = mapped.Totals
            .GroupBy(x => (x.GameId, x.BookmakerName))
            .Select(g => g.First())
            .ToList();

        if (h2h.Any())
            await repo.AddOrUpdateBookmakerOdds(h2h);
        if (spreads.Any())
            await repo.AddOrUpdateBookmakerSpreads(spreads);
        if (totals.Any())
            await repo.AddOrUpdateBookmakerTotals(totals);
        await repo.Commit();

        logger.LogInformation("Saved {H2H} h2h, {Spreads} spreads, {Totals} totals records.",
            h2h.Count, spreads.Count, totals.Count);
    }

    public static void LogMatchingDetails(ILogger logger, List<OddsApiResponse> responses,
        List<OddsApiResponseMapper.GameInfo> gameInfoList)
    {
        logger.LogInformation("API returned {Count} games. DB has {DbCount} games needing odds.",
            responses.Count, gameInfoList.Count);
        foreach (var r in responses)
        {
            var apiDateUtc = r.CommenceTime.ToUniversalTime().Date;
            var candidates = gameInfoList.Where(g => g.GameDateUTC.Date == apiDateUtc).ToList();
            var bestCandidate = candidates
                .Select(g => new { g.GameId, g.HomeTeamName, Score = FuzzySharp.Fuzz.TokenSortRatio(g.HomeTeamName.ToLower(), r.HomeTeam.ToLower()) })
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            logger.LogInformation("API: {Home} vs {Away} | date={UtcDate} | candidates={Count} | best={BestName} (id={BestId}, score={Score})",
                r.HomeTeam, r.AwayTeam, apiDateUtc, candidates.Count,
                bestCandidate?.HomeTeamName ?? "none", bestCandidate?.GameId ?? 0, bestCandidate?.Score ?? 0);
        }
    }
}

public record GameRef(int Id, int HomeTeamId, int AwayTeamId);
