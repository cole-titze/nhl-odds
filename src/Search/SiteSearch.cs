using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Search;

/// <summary>The text of a search and the date it names, if any ("oilers march 3" → "oilers", March 3).</summary>
public record ParsedQuery(string Text, DateOnly? Date);

public record TeamResult(int TeamId, string Name, string Abbreviation, string LogoUri, bool Active);
public record PlayerResult(int PlayerId, string Name, string? Position, int? TeamId, string? TeamName, string? TeamAbbreviation, bool IsActive, string Headshot);
public record GameSideResult(int TeamId, string Name, string Abbreviation, int? Goals);
public record GameResult(int GameId, string GameDate, DateTime StartTimeUtc, bool Played, GameSideResult Home, GameSideResult Away);
/// <param name="Snippet">Body text around the match, with each match between <see cref="SiteSearch.MatchStart"/> and <see cref="SiteSearch.MatchEnd"/></param>
public record ArticleResult(string Title, string? Summary, string Url, DateTime? Date, string? Snippet);
public record SearchResults(string Query, List<TeamResult> Teams, List<PlayerResult> Players, List<GameResult> Games, List<ArticleResult> Articles);

/// <summary>Builds the site's search request (one _msearch over the four indices) and reads its response.</summary>
public static partial class SiteSearch
{
    public const int MinQueryChars = 2;
    public const int MaxQueryChars = 100;

    /// <summary>
    /// Typos allowed in a name: none up to 3 letters, 1 up to 7, 2 from 8. The default (AUTO = 3,6) allows 2 in a
    /// 6-letter word, so "bruins" matched the player Brent Burns.
    /// </summary>
    public const string NameFuzziness = "AUTO:4,8";

    // Private-use characters mark matches in article snippets, so the snippet stays plain text (no HTML to escape)
    public const char MatchStart = '';
    public const char MatchEnd = '';

    private static readonly string[] Months =
        ["january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december"];

    /// <summary>
    /// Takes the first date out of the query: today/tonight/yesterday/tomorrow, 2026-03-03, 3/3, 3/3/26 or
    /// "march 3" (any month abbreviation of 3+ letters, an optional "rd"/"th" and year). A date without a year is
    /// the nearest one to <paramref name="today"/>, so in November "march 3" is next March but "oct 30" is last week.
    /// </summary>
    public static ParsedQuery Parse(string query, DateOnly today)
    {
        var text = WhitespaceRegex().Replace(query.Trim(), " ");

        var relative = RelativeDateRegex().Match(text);
        if (relative.Success)
        {
            var offset = relative.Value.ToLowerInvariant() switch { "yesterday" => -1, "tomorrow" => 1, _ => 0 };
            return new ParsedQuery(Remove(text, relative), today.AddDays(offset));
        }

        var iso = IsoDateRegex().Match(text);
        if (iso.Success && DateOnly.TryParseExact(iso.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoDate))
            return new ParsedQuery(Remove(text, iso), isoDate);

        var numeric = NumericDateRegex().Match(text);
        if (numeric.Success)
        {
            var date = MakeDate(int.Parse(numeric.Groups["m"].Value), int.Parse(numeric.Groups["d"].Value),
                numeric.Groups["y"].Success ? int.Parse(numeric.Groups["y"].Value) : null, today);
            if (date != null)
                return new ParsedQuery(Remove(text, numeric), date);
        }

        foreach (Match named in NamedDateRegex().Matches(text))
        {
            var word = named.Groups["month"].Value.ToLowerInvariant();
            var month = Array.FindIndex(Months, m => m.StartsWith(word) || (word == "sept" && m == "september"));
            if (month < 0)
                continue;
            var date = MakeDate(month + 1, int.Parse(named.Groups["d"].Value),
                named.Groups["y"].Success ? int.Parse(named.Groups["y"].Value) : null, today);
            if (date != null)
                return new ParsedQuery(Remove(text, named), date);
        }

        return new ParsedQuery(text, null);
    }

    private static string Remove(string text, Match match) =>
        WhitespaceRegex().Replace(text.Remove(match.Index, match.Length), " ").Trim();

    private static DateOnly? MakeDate(int month, int day, int? year, DateOnly today)
    {
        if (month is < 1 or > 12 || day < 1)
            return null;
        if (year is { } y)
        {
            var fullYear = y < 100 ? 2000 + y : y;
            return day <= DateTime.DaysInMonth(fullYear, month) ? new DateOnly(fullYear, month, day) : null;
        }
        // The nearest such date: last year's, this year's or next year's
        return new[] { today.Year - 1, today.Year, today.Year + 1 }
            .Where(yr => day <= DateTime.DaysInMonth(yr, month))
            .Select(yr => new DateOnly(yr, month, day))
            .OrderBy(d => Math.Abs(d.DayNumber - today.DayNumber))
            .Cast<DateOnly?>()
            .FirstOrDefault();
    }

    /// <summary>
    /// How many article matches each half of a hybrid search brings to <see cref="FuseArticles"/>: deeper than the
    /// page shows, so an article both halves find, though neither ranks it first, can still make the page.
    /// </summary>
    public const int FusionDepth = 20;

    /// <summary>Reciprocal rank fusion's k: a larger value flattens the gap between first and tenth place.</summary>
    public const int RrfK = 60;

    /// <summary>
    /// The _msearch body: a header line and a query line for each index, in the order of <see cref="SearchIndexes.All"/>,
    /// then, given a query vector, a vector (knn) search of the articles. Elasticsearch's own hybrid search (the rrf
    /// retriever) needs a paid license, so the two article searches are fused here (<see cref="ReadResponse"/>).
    /// </summary>
    /// <param name="queryVector">The query's text embedded by the same model as the articles, or null for keyword search alone</param>
    /// <param name="minSimilarity">The least cosine similarity a vector match needs (<see cref="EmbeddingSettings.MinSimilarity"/>)</param>
    public static string BuildRequest(ParsedQuery query, DateTime nowUtc, float[]? queryVector = null,
        double minSimilarity = EmbeddingSettings.DefaultMinSimilarity)
    {
        var hybrid = queryVector != null && query.Text.Length >= MinQueryChars;
        var body = new StringBuilder();
        void Add(SearchIndex index, JsonObject search, int? size = null)
        {
            search["size"] = size ?? index.Size;
            body.Append(new JsonObject { ["index"] = index.Alias }.ToJsonString()).Append('\n');
            body.Append(search.ToJsonString()).Append('\n');
        }

        Add(SearchIndexes.Teams, TeamsQuery(query.Text));
        Add(SearchIndexes.Players, PlayersQuery(query.Text));
        Add(SearchIndexes.Games, GamesQuery(query, nowUtc));
        Add(SearchIndexes.Articles, ArticlesQuery(query.Text), hybrid ? FusionDepth : null);
        if (hybrid)
            Add(SearchIndexes.Articles, ArticlesVectorQuery(query.Text, queryVector!, minSimilarity), FusionDepth);
        return body.ToString();
    }

    /// <summary>Matches nothing: used for an index the query can't apply to (e.g. players for a bare date).</summary>
    private static JsonObject MatchNone() => new() { ["query"] = new JsonObject { ["match_none"] = new JsonObject() } };

    /// <summary>
    /// A name as you type ("mapl" → Maple Leafs) or with a typo ("mapel leafs"). bool_prefix treats the last word as a
    /// prefix; the second clause adds nickname synonyms and fuzziness, which prefixes don't get.
    /// </summary>
    private static JsonArray NameClauses(string text, params string[] fields)
    {
        var prefixFields = new JsonArray(fields.SelectMany(f => new[] { f, $"{f}._2gram", $"{f}._3gram" }).Select(f => (JsonNode)f).ToArray());
        return new JsonArray(
            new JsonObject
            {
                ["multi_match"] = new JsonObject
                {
                    ["query"] = text,
                    ["type"] = "bool_prefix",
                    ["fields"] = prefixFields,
                    ["operator"] = "and",
                },
            },
            new JsonObject
            {
                ["multi_match"] = new JsonObject
                {
                    ["query"] = text,
                    ["fields"] = new JsonArray(fields.Select(f => (JsonNode)f).ToArray()),
                    ["fuzziness"] = NameFuzziness,
                    ["operator"] = "and",
                },
            });
    }

    private static JsonObject TeamsQuery(string text)
    {
        if (text.Length < MinQueryChars)
            return MatchNone();
        var should = NameClauses(text, "name", "commonName", "placeName");
        should.Add(new JsonObject { ["match"] = new JsonObject { ["formerNames"] = new JsonObject { ["query"] = text, ["operator"] = "and" } } });
        should.Add(new JsonObject { ["term"] = new JsonObject { ["abbreviation"] = new JsonObject { ["value"] = text, ["boost"] = 5 } } });
        return new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["bool"] = new JsonObject
                {
                    ["must"] = new JsonObject { ["bool"] = new JsonObject { ["should"] = should, ["minimum_should_match"] = 1 } },
                    // Today's teams before relocated ones (Coyotes, Thrashers) among similar matches
                    ["should"] = new JsonArray(new JsonObject { ["term"] = new JsonObject { ["active"] = new JsonObject { ["value"] = true, ["boost"] = 2 } } }),
                },
            },
        };
    }

    private static JsonObject PlayersQuery(string text)
    {
        if (text.Length < MinQueryChars)
            return MatchNone();
        return new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["bool"] = new JsonObject
                {
                    ["must"] = new JsonObject
                    {
                        ["bool"] = new JsonObject { ["should"] = NameClauses(text, "fullName"), ["minimum_should_match"] = 1 },
                    },
                    // Active players first among similar matches
                    ["should"] = new JsonArray(new JsonObject { ["term"] = new JsonObject { ["isActive"] = new JsonObject { ["value"] = true, ["boost"] = 2 } } }),
                },
            },
        };
    }

    /// <summary>
    /// Games whose two teams between them match every word ("bruins rangers", "leafs"), on the date if one was given.
    /// Without a date, games nearest today rank first (a decay on start time).
    /// </summary>
    private static JsonObject GamesQuery(ParsedQuery query, DateTime nowUtc)
    {
        if (query.Text.Length < MinQueryChars && query.Date == null)
            return MatchNone();

        var filter = new JsonArray();
        if (query.Date is { } date)
            filter.Add(new JsonObject { ["term"] = new JsonObject { ["gameDate"] = date.ToString("yyyy-MM-dd") } });

        var must = new JsonArray();
        if (query.Text.Length > 0)
            must.Add(new JsonObject
            {
                ["bool"] = new JsonObject
                {
                    // Nicknames ("habs") through the field's search analyzer, or typos through fuzziness
                    ["should"] = new JsonArray(
                        new JsonObject { ["match"] = new JsonObject { ["teams"] = new JsonObject { ["query"] = query.Text, ["operator"] = "and" } } },
                        new JsonObject
                        {
                            ["match"] = new JsonObject
                            {
                                ["teams"] = new JsonObject
                                {
                                    ["query"] = query.Text, ["operator"] = "and", ["fuzziness"] = NameFuzziness,
                                    ["analyzer"] = SearchIndexes.NameAnalyzer,
                                },
                            },
                        }),
                    ["minimum_should_match"] = 1,
                },
            });

        var boolQuery = new JsonObject { ["must"] = must, ["filter"] = filter };
        if (query.Date != null)
            return new JsonObject
            {
                ["query"] = new JsonObject { ["bool"] = boolQuery },
                ["sort"] = new JsonArray(new JsonObject { ["startTimeUtc"] = "asc" }),
            };

        return new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["function_score"] = new JsonObject
                {
                    ["query"] = new JsonObject { ["bool"] = boolQuery },
                    ["functions"] = new JsonArray(new JsonObject
                    {
                        ["gauss"] = new JsonObject
                        {
                            ["startTimeUtc"] = new JsonObject
                            {
                                ["origin"] = nowUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                                ["scale"] = "14d",
                                ["decay"] = 0.5,
                            },
                        },
                    }),
                    // Every match scores about the same, so closeness to today decides the order
                    ["boost_mode"] = "replace",
                },
            },
        };
    }

    private static JsonObject ArticlesQuery(string text)
    {
        if (text.Length < MinQueryChars)
            return MatchNone();
        return new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["function_score"] = new JsonObject
                {
                    ["query"] = ArticleTextMatch(text),
                    // Newer articles first among similar matches, without burying an exact old one
                    ["functions"] = new JsonArray(new JsonObject
                    {
                        ["gauss"] = new JsonObject
                        {
                            ["date"] = new JsonObject { ["origin"] = "now", ["scale"] = "30d", ["decay"] = 0.5 },
                        },
                        ["weight"] = 1,
                    }),
                    ["boost_mode"] = "sum",
                },
            },
            ["_source"] = ArticleSource(),
            ["highlight"] = ArticleHighlight(null),
        };
    }

    /// <summary>The words an article must mostly contain: short queries need every word; longer ones most of them.</summary>
    private static JsonObject ArticleTextMatch(string text) => new()
    {
        ["multi_match"] = new JsonObject
        {
            ["query"] = text,
            ["fields"] = new JsonArray("title^3", "summary^2", "body"),
            ["minimum_should_match"] = "2<75%",
        },
    };

    private static JsonArray ArticleSource() => new("title", "summary", "url", "date");

    /// <param name="highlightQuery">What to mark in the snippet when the search itself has no words (a knn search)</param>
    private static JsonObject ArticleHighlight(JsonObject? highlightQuery)
    {
        // Without a match, no_match_size gives the article's opening instead
        var body = new JsonObject { ["fragment_size"] = 160, ["number_of_fragments"] = 1, ["no_match_size"] = 160 };
        if (highlightQuery != null)
            body["highlight_query"] = highlightQuery;
        return new JsonObject
        {
            ["pre_tags"] = new JsonArray(MatchStart.ToString()),
            ["post_tags"] = new JsonArray(MatchEnd.ToString()),
            ["fields"] = new JsonObject { ["body"] = body },
        };
    }

    /// <summary>
    /// Articles about what the query means, not only the words it uses ("who got traded" finds a trade article
    /// that says "acquired"). The <paramref name="minSimilarity"/> cut keeps a query no article is about (a player's
    /// name, say) from bringing up the least unrelated ones.
    /// </summary>
    private static JsonObject ArticlesVectorQuery(string text, float[] queryVector, double minSimilarity) => new()
    {
        ["knn"] = new JsonObject
        {
            ["field"] = SearchIndexes.ArticleEmbeddingField,
            ["query_vector"] = new JsonArray(queryVector.Select(v => (JsonNode)v).ToArray()),
            ["k"] = FusionDepth,
            ["num_candidates"] = 100,
            ["similarity"] = minSimilarity,
        },
        ["_source"] = ArticleSource(),
        // Mark any of the query's words the article has; the match itself was by meaning, so it may have none
        ["highlight"] = ArticleHighlight(new JsonObject
        {
            ["multi_match"] = new JsonObject { ["query"] = text, ["fields"] = new JsonArray("title", "summary", "body") },
        }),
    };

    /// <summary>
    /// Reads the _msearch response. An index that failed (e.g. not built yet, or an articles index without vectors
    /// asked for a knn search) gives no results rather than an error.
    /// </summary>
    public static SearchResults ReadResponse(string query, JsonNode response)
    {
        var responses = response["responses"]!.AsArray();
        List<JsonNode> Hits(int i) =>
            responses.Count > i && responses[i]!["hits"]?["hits"] is JsonArray hits ? hits.Select(h => h!).ToList() : [];
        T Source<T>(JsonNode hit) => hit["_source"].Deserialize<T>(SearchJson.Options)!;

        var teams = Hits(0).Select(Source<TeamDocument>)
            .Select(t => new TeamResult(t.TeamId, t.Name, t.Abbreviation, t.LogoUri, t.Active)).ToList();
        var players = Hits(1).Select(Source<PlayerDocument>)
            .Select(p => new PlayerResult(p.PlayerId, p.FullName, p.Position, p.TeamId, p.TeamName, p.TeamAbbreviation, p.IsActive, p.Headshot)).ToList();
        var games = Hits(2).Select(Source<GameDocument>)
            .Select(g => new GameResult(g.GameId, g.GameDate, g.StartTimeUtc, g.Played,
                new GameSideResult(g.Home.TeamId, g.Home.Name, g.Home.Abbreviation, g.Home.Goals),
                new GameSideResult(g.Away.TeamId, g.Away.Name, g.Away.Abbreviation, g.Away.Goals))).ToList();
        var articles = FuseArticles(Hits(3), Hits(4)).Take(SearchIndexes.Articles.Size).Select(hit =>
        {
            var source = hit["_source"]!;
            var snippet = hit["highlight"]?["body"]?[0]?.GetValue<string>();
            return new ArticleResult(source["title"]!.GetValue<string>(), source["summary"]?.GetValue<string>(),
                source["url"]!.GetValue<string>(), source["date"]?.GetValue<DateTime>(), snippet);
        }).ToList();

        return new SearchResults(query, teams, players, games, articles);
    }

    /// <summary>
    /// Reciprocal rank fusion of the keyword and vector article hits: each hit scores 1/(k + rank) in each list it's
    /// in, summed. Ranks, not scores, because BM25 and cosine scores aren't on the same scale. An article in both
    /// lists keeps the keyword hit, whose snippet marks the words that matched. With no vector hits it's the keyword
    /// order unchanged.
    /// </summary>
    public static List<JsonNode> FuseArticles(IReadOnlyList<JsonNode> keywordHits, IReadOnlyList<JsonNode> vectorHits)
    {
        var fused = new Dictionary<string, (JsonNode Hit, double Score, int FirstSeen)>();
        var seen = 0;
        void AddList(IReadOnlyList<JsonNode> hits)
        {
            for (var rank = 0; rank < hits.Count; rank++)
            {
                var id = hits[rank]["_id"]?.GetValue<string>() ?? hits[rank]["_source"]?["url"]?.GetValue<string>() ?? $"#{seen}";
                var score = 1.0 / (RrfK + rank + 1);
                fused[id] = fused.TryGetValue(id, out var existing)
                    ? (existing.Hit, existing.Score + score, existing.FirstSeen)
                    : (hits[rank], score, seen++);
            }
        }
        AddList(keywordHits);
        AddList(vectorHits);
        // Ties (one list each, same rank) go to the keyword hit, added first
        return fused.Values.OrderByDescending(f => f.Score).ThenBy(f => f.FirstSeen).Select(f => f.Hit).ToList();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
    [GeneratedRegex(@"\b(today|tonight|yesterday|tomorrow)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RelativeDateRegex();
    [GeneratedRegex(@"\b\d{4}-\d{2}-\d{2}\b")]
    private static partial Regex IsoDateRegex();
    [GeneratedRegex(@"\b(?<m>\d{1,2})/(?<d>\d{1,2})(/(?<y>\d{2}|\d{4}))?\b")]
    private static partial Regex NumericDateRegex();
    [GeneratedRegex(@"\b(?<month>[a-z]{3,9})\.?\s+(?<d>\d{1,2})(st|nd|rd|th)?(,?\s+(?<y>\d{4}))?\b", RegexOptions.IgnoreCase)]
    private static partial Regex NamedDateRegex();
}
