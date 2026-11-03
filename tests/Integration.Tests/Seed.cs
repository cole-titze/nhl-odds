using Npgsql;

namespace Integration.Tests;

/// <summary>
/// Small builders for test rows. Each has defaults for every NOT NULL column, so a test only
/// sets the fields it's about. Rows go in with plain SQL, so reading them back also checks
/// the EF model against the real columns.
/// </summary>
public static class Seed
{
    public const int Season = 2026;
    public const int Leafs = 10;
    public const int Bruins = 6;

    public record Team(int Id, string Abbreviation, string PlaceName, string CommonName, int Season = Seed.Season);

    public static readonly Team TorontoMapleLeafs = new(Leafs, "TOR", "Toronto", "Maple Leafs");
    public static readonly Team BostonBruins = new(Bruins, "BOS", "Boston", "Bruins");

    public record Game(int Id, DateTime StartUtc)
    {
        public int HomeTeamId { get; init; } = Leafs;
        public int AwayTeamId { get; init; } = Bruins;
        public int Season { get; init; } = Seed.Season;
        public bool Played { get; init; }
        public int HomeGoals { get; init; }
        public int AwayGoals { get; init; }
        /// <summary>0 home, 1 away (Entities.Types.Winner)</summary>
        public int Winner { get; init; }
        public int GameType { get; init; } = 2;
        /// <summary>0 regulation, 1 overtime, 2 shootout (Entities.Types.Enums.PeriodType)</summary>
        public int EndPeriod { get; init; }
    }

    // The predictor always writes Notes; null tests a row it left empty
    public record GameOdds(int GameId, DateTime RunUtc, double HomeOdds, int ModelId = 1, string? Notes = "integration-test");

    public record BookmakerOdds(int GameId, string Bookmaker, double HomeOdds)
    {
        public DateTime FetchedUtc { get; init; } = new(2026, 11, 1, 12, 0, 0);
        /// <summary>After the game's start means the book's price was live (in-game)</summary>
        public DateTime MarketLastUpdateUtc { get; init; } = new(2026, 11, 1, 12, 0, 0);
    }

    public static async Task Teams(params Team[] teams)
    {
        foreach (var t in teams)
        {
            await Insert("""
                INSERT INTO "Team" ("Id", "Abbreviation", "FranchiseId", "LeagueId")
                VALUES (@id, @abbrev, @id, 133) ON CONFLICT DO NOTHING
                """, ("id", t.Id), ("abbrev", t.Abbreviation));
            await Insert("""
                INSERT INTO "SeasonTeam" ("TeamId", "Abbreviation", "SeasonStartYear", "Name", "CommonName",
                    "PlaceName", "LogoUri", "Division", "Conference", "DivisionAbbreviation", "ConferenceAbbreviation")
                VALUES (@id, @abbrev, @season, @name, @common, @place, @logo, 'Atlantic', 'Eastern', 'A', 'E')
                """,
                ("id", t.Id), ("abbrev", t.Abbreviation), ("season", t.Season),
                ("name", $"{t.PlaceName} {t.CommonName}"), ("common", t.CommonName), ("place", t.PlaceName),
                ("logo", $"https://assets.nhle.com/logos/nhl/svg/{t.Abbreviation}_light.svg"));
        }
    }

    /// <summary>Both default teams, for the current test season.</summary>
    public static Task DefaultTeams() => Teams(TorontoMapleLeafs, BostonBruins);

    public static async Task Games(params Game[] games)
    {
        foreach (var g in games)
        {
            await Insert("""
                INSERT INTO "GameRaw" ("Id", "HomeTeamId", "AwayTeamId", "SeasonStartYear", "GameDateUTC",
                    "HomeGoals", "AwayGoals", "HomeSOG", "AwaySOG", "HomePPG", "AwayPPG", "HomePIM", "AwayPIM",
                    "HomeFaceOffWinPercent", "AwayFaceOffWinPercent", "HomeBlockedShots", "AwayBlockedShots",
                    "HomeHits", "AwayHits", "HomeTakeaways", "AwayTakeaways", "HomeGiveaways", "AwayGiveaways",
                    "Winner", "EndPeriod", "HasBeenPlayed", "GameType", "ThreeMinuteRecapVideoId",
                    "CondensedGameVideoId", "VenueName", "VenueLocation")
                VALUES (@id, @home, @away, @season, @start, @homeGoals, @awayGoals, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, @winner, @endPeriod, @played, @type, 0, 0, '', '')
                """,
                ("id", g.Id), ("home", g.HomeTeamId), ("away", g.AwayTeamId), ("season", g.Season),
                ("start", g.StartUtc), ("homeGoals", g.HomeGoals), ("awayGoals", g.AwayGoals),
                ("winner", g.Winner), ("endPeriod", g.EndPeriod), ("played", g.Played), ("type", g.GameType));
        }
    }

    public static async Task Predictions(params GameOdds[] odds)
    {
        foreach (var o in odds)
        {
            await Insert("""
                INSERT INTO "GameOdds" ("GameId", "ModelId", "RunDateUTC", "HomeOdds", "AwayOdds", "Notes")
                VALUES (@game, @model, @run, @home, @away, @notes)
                """,
                ("game", o.GameId), ("model", o.ModelId), ("run", o.RunUtc), ("home", o.HomeOdds), ("away", 1 - o.HomeOdds),
                ("notes", (object?)o.Notes ?? DBNull.Value));
        }
    }

    public static async Task Bookmakers(params BookmakerOdds[] odds)
    {
        foreach (var o in odds)
        {
            await Insert("""
                INSERT INTO "BookmakerOdds" ("GameId", "BookmakerName", "HomeOdds", "AwayOdds", "FetchedDateUTC",
                    "MarketLastUpdate")
                VALUES (@game, @book, @home, @away, @fetched, @market)
                """,
                ("game", o.GameId), ("book", o.Bookmaker), ("home", o.HomeOdds), ("away", 1 - o.HomeOdds),
                ("fetched", o.FetchedUtc), ("market", o.MarketLastUpdateUtc));
        }
    }

    private static async Task Insert(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }
}
