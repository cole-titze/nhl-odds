using System.Globalization;
using System.Text.Json;
using DatabaseAccess.SnapshotRepository;
using Entities.DbModels;
using Microsoft.Extensions.Logging;
using Services.NhlData;

namespace DataGetter.BusinessLogic;

/// <summary>
/// Saves each new version of NHL.com's betting-partner odds widget. It only shows the current odds, so these
/// snapshots are the only history of how the lines moved.
/// </summary>
public class NhlPartnerOddsManager
{
    public static readonly string[] Countries = ["US", "CA"];
    private readonly ISnapshotRepository _snapshotRepo;
    private readonly INhlContentGetter _contentGetter;
    private readonly ILogger<NhlPartnerOddsManager> _logger;

    public NhlPartnerOddsManager(ISnapshotRepository snapshotRepository, INhlContentGetter contentGetter, ILoggerFactory loggerFactory)
    {
        _snapshotRepo = snapshotRepository;
        _contentGetter = contentGetter;
        _logger = loggerFactory.CreateLogger<NhlPartnerOddsManager>();
    }

    public async Task SavePartnerOdds(string country, DateTime nowUtc)
    {
        var json = await _contentGetter.GetPartnerOdds(country);
        var odds = ParseOdds(json, country, nowUtc.AddTicks(-(nowUtc.Ticks % TimeSpan.TicksPerSecond)));
        if (odds.Count == 0)
        {
            _logger.LogInformation("{Country} partner odds have no games", country);
            return;
        }

        var version = odds[0].PartnerUpdatedUTC;
        if (await _snapshotRepo.PartnerOddsVersionExists(country, version))
        {
            _logger.LogInformation("{Country} partner odds unchanged (last updated {Updated})", country, version);
            return;
        }
        await _snapshotRepo.AddPartnerOdds(odds);
        _logger.LogInformation("Saved {Count} {Country} partner odds rows for {Games} games (last updated {Updated})",
            odds.Count, country, odds.Select(o => o.GameId).Distinct().Count(), version);
    }

    /// <summary>One row per team per odds line. Lines repeated within a version are kept once.</summary>
    public static List<DbPartnerOdds> ParseOdds(string json, string country, DateTime firstSeenUtc)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var updated = ReadDate(root, "lastUpdatedUTC")
            ?? throw new Exception($"{country} partner odds have no lastUpdatedUTC");
        var oddsDate = ReadDate(root, "currentOddsDate");
        var partner = root.TryGetProperty("bettingPartner", out var p) && p.TryGetProperty("name", out var name)
            ? name.GetString() ?? string.Empty
            : string.Empty;

        var rows = new List<DbPartnerOdds>();
        if (!root.TryGetProperty("games", out var games))
            return rows;
        foreach (var game in games.EnumerateArray())
        {
            var gameId = game.GetProperty("gameId").GetInt32();
            var start = ReadDate(game, "startTimeUTC") ?? throw new Exception($"Game {gameId} has no startTimeUTC");
            foreach (var (side, isHome) in new[] { ("homeTeam", true), ("awayTeam", false) })
            {
                if (!game.TryGetProperty(side, out var team) || !team.TryGetProperty("odds", out var lines))
                    continue;
                var teamId = team.GetProperty("id").GetInt32();
                foreach (var line in lines.EnumerateArray())
                {
                    // Suspended markets may come without a price
                    if (!line.TryGetProperty("value", out var price) || price.ValueKind != JsonValueKind.Number)
                        continue;
                    rows.Add(new DbPartnerOdds
                    {
                        Country = country,
                        PartnerUpdatedUTC = updated,
                        GameId = gameId,
                        TeamId = teamId,
                        Market = line.GetProperty("description").GetString() ?? string.Empty,
                        Qualifier = line.TryGetProperty("qualifier", out var q) ? q.GetString() ?? string.Empty : string.Empty,
                        Price = price.GetDecimal(),
                        IsHome = isHome,
                        PartnerName = partner,
                        OddsDate = oddsDate,
                        StartTimeUTC = start,
                        FirstSeenUTC = firstSeenUtc,
                    });
                }
            }
        }
        return rows.DistinctBy(o => (o.GameId, o.TeamId, o.Market, o.Qualifier)).ToList();
    }

    private static DateTime? ReadDate(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            && DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date)
            ? date
            : null;
    }
}