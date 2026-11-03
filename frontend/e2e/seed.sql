-- Seed data for the end-to-end smoke test (smoke.mjs), loaded after CreateTables.sql.
-- Dates are relative to now, so the Games page's anchor date and the current season
-- (September 20 rule, as in Seasons.Current / getCurrentSeason) always find these games.
-- Logos point at the frontend's own favicon, so the test doesn't depend on assets.nhle.com.
DO $$
DECLARE
    season INTEGER := CASE
        WHEN now() >= make_timestamp(extract(year FROM now())::int, 9, 20, 0, 0, 0)
        THEN extract(year FROM now())::int
        ELSE extract(year FROM now())::int - 1 END;
    -- 00:00 UTC is the evening before in Central time, a typical NHL start
    today TIMESTAMP := date_trunc('day', now() AT TIME ZONE 'UTC') + interval '1 day';
    g RECORD;
BEGIN
    INSERT INTO "Team" ("Id", "Abbreviation", "FranchiseId", "LeagueId") VALUES
        (10, 'TOR', 5, 133), (6, 'BOS', 6, 133), (20, 'CGY', 21, 133), (22, 'EDM', 25, 133);

    INSERT INTO "SeasonTeam" ("TeamId", "Abbreviation", "SeasonStartYear", "Name", "CommonName", "PlaceName",
        "LogoUri", "Division", "Conference", "DivisionAbbreviation", "ConferenceAbbreviation") VALUES
        (10, 'TOR', season, 'Toronto Maple Leafs', 'Maple Leafs', 'Toronto', '/favicon.svg', 'Atlantic', 'Eastern', 'A', 'E'),
        (6, 'BOS', season, 'Boston Bruins', 'Bruins', 'Boston', '/favicon.svg', 'Atlantic', 'Eastern', 'A', 'E'),
        (20, 'CGY', season, 'Calgary Flames', 'Flames', 'Calgary', '/favicon.svg', 'Pacific', 'Western', 'P', 'W'),
        (22, 'EDM', season, 'Edmonton Oilers', 'Oilers', 'Edmonton', '/favicon.svg', 'Pacific', 'Western', 'P', 'W');

    -- Game ids follow NhlApiDataGetter.GetGameId: season * 1,000,000 + 20,000 + game number
    FOR g IN SELECT * FROM (VALUES
        -- n, home, away, days from today, played, home goals, away goals, winner (0 home, 1 away), home model odds
        (1, 10, 6, -3, TRUE, 4, 2, 0, 0.58),
        (2, 20, 22, -2, TRUE, 1, 3, 1, 0.47),
        (3, 6, 20, -1, TRUE, 2, 3, 1, 0.61),
        (4, 22, 10, 0, FALSE, 0, 0, 0, 0.52),
        (5, 10, 20, 1, FALSE, 0, 0, 0, 0.63)
    ) AS v(n, home, away, days, played, home_goals, away_goals, winner, home_odds)
    LOOP
        INSERT INTO "GameRaw" ("Id", "HomeTeamId", "AwayTeamId", "SeasonStartYear", "GameDateUTC",
            "HomeGoals", "AwayGoals", "HomeSOG", "AwaySOG", "HomePPG", "AwayPPG", "HomePIM", "AwayPIM",
            "HomeFaceOffWinPercent", "AwayFaceOffWinPercent", "HomeBlockedShots", "AwayBlockedShots",
            "HomeHits", "AwayHits", "HomeTakeaways", "AwayTakeaways", "HomeGiveaways", "AwayGiveaways",
            "Winner", "EndPeriod", "HasBeenPlayed", "GameType", "ThreeMinuteRecapVideoId",
            "CondensedGameVideoId", "VenueName", "VenueLocation")
        VALUES (season * 1000000 + 20000 + g.n, g.home, g.away, season, today + g.days * interval '1 day',
            g.home_goals, g.away_goals, 30, 28, 1, 0, 6, 8, 52, 48, 12, 10, 20, 18, 5, 4, 7, 9,
            g.winner, 0, g.played, 2, 0, 0, '', '');

        INSERT INTO "GameOdds" ("GameId", "ModelId", "RunDateUTC", "HomeOdds", "AwayOdds", "Notes")
        VALUES (season * 1000000 + 20000 + g.n, 1, today + (g.days - 1) * interval '1 day',
            g.home_odds, 1 - g.home_odds, 'e2e-seed');

        INSERT INTO "BookmakerOdds" ("GameId", "BookmakerName", "BookmakerKey", "HomeOdds", "AwayOdds", "FetchedDateUTC")
        VALUES
            (season * 1000000 + 20000 + g.n, 'DraftKings', 'draftkings', g.home_odds - 0.03, 1.03 - g.home_odds,
                today + (g.days - 1) * interval '1 day'),
            (season * 1000000 + 20000 + g.n, 'Kalshi', 'kalshi', g.home_odds - 0.01, 1.01 - g.home_odds,
                today + (g.days - 1) * interval '1 day');
    END LOOP;
END $$;
