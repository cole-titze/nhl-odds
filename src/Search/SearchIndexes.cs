using System.Text.Json.Nodes;

namespace Search;

/// <summary>
/// One searchable index. The site only ever uses <see cref="Alias"/>; each rebuild creates a new
/// <c>{alias}-{timestamp}</c> index and moves the alias to it (see <see cref="SearchIndexer"/>).
/// </summary>
public record SearchIndex(string Alias, JsonObject Mappings, int Size)
{
    /// <summary>The same index with one more field mapped.</summary>
    public SearchIndex WithProperty(string name, JsonObject mapping)
    {
        var mappings = Mappings.DeepClone().AsObject();
        mappings["properties"]![name] = mapping;
        return this with { Mappings = mappings };
    }

    public string NewIndexName(DateTime nowUtc) => $"{Alias}-{nowUtc:yyyyMMddHHmmss}";

    /// <summary>The create-index body: shared analysis settings, one shard, no replicas (it's a single node).</summary>
    public JsonObject CreateBody() => new()
    {
        ["settings"] = new JsonObject
        {
            ["number_of_shards"] = 1,
            ["number_of_replicas"] = 0,
            ["analysis"] = SearchIndexes.Analysis(),
        },
        ["mappings"] = Mappings.DeepClone(),
    };
}

public static class SearchIndexes
{
    /// <summary>
    /// Fan nicknames that aren't a word of the team's name, applied only when searching (search_analyzer), so a
    /// change here works without reindexing. "Leafs", "Wings" and "Jackets" already match their team's name.
    /// </summary>
    public static readonly string[] TeamNicknames =
    [
        "habs => canadiens",
        "bolts => lightning",
        "pens => penguins",
        "caps => capitals",
        "sens => senators",
        "nucks => canucks",
        "hawks => blackhawks",
        "isles => islanders",
        "avs => avalanche",
        "preds => predators",
        "canes => hurricanes",
        "yotes => coyotes",
        "cats => panthers",
        "knights => golden knights",
        "b's => bruins",
    ];

    // Names index lowercased and accent-folded ("Montréal" matches "montreal"); searches also expand nicknames
    public const string NameAnalyzer = "name_index";
    public const string NameSearchAnalyzer = "name_search";
    public const string ArticleAnalyzer = "english";

    public static JsonObject Analysis() => new()
    {
        ["filter"] = new JsonObject
        {
            ["team_nicknames"] = new JsonObject
            {
                ["type"] = "synonym_graph",
                ["synonyms"] = new JsonArray(TeamNicknames.Select(s => (JsonNode)s).ToArray()),
            },
        },
        ["analyzer"] = new JsonObject
        {
            [NameAnalyzer] = new JsonObject
            {
                ["tokenizer"] = "standard",
                ["filter"] = new JsonArray("lowercase", "asciifolding"),
            },
            [NameSearchAnalyzer] = new JsonObject
            {
                ["tokenizer"] = "standard",
                ["filter"] = new JsonArray("lowercase", "asciifolding", "team_nicknames"),
            },
        },
        ["normalizer"] = new JsonObject
        {
            ["lowercase"] = new JsonObject { ["type"] = "custom", ["filter"] = new JsonArray("lowercase") },
        },
    };

    /// <summary>A name to match as you type: search_as_you_type adds the 2/3-word shingle and prefix subfields.</summary>
    private static JsonObject TypeaheadName() => new()
    {
        ["type"] = "search_as_you_type",
        ["analyzer"] = NameAnalyzer,
        ["search_analyzer"] = NameSearchAnalyzer,
    };

    private static JsonObject Name() => new()
    {
        ["type"] = "text",
        ["analyzer"] = NameAnalyzer,
        ["search_analyzer"] = NameSearchAnalyzer,
    };

    private static JsonObject Abbreviation() => new() { ["type"] = "keyword", ["normalizer"] = "lowercase" };
    private static JsonObject Keyword() => new() { ["type"] = "keyword" };
    private static JsonObject StoredOnly() => new() { ["type"] = "keyword", ["index"] = false };
    private static JsonObject Of(string type) => new() { ["type"] = type };

    private static JsonObject Mappings(JsonObject properties) => new()
    {
        // A field the mapping doesn't list is stored but not indexed, so a new document field can't change the mapping
        ["dynamic"] = false,
        ["properties"] = properties,
    };

    public static readonly SearchIndex Teams = new("nhl-teams", Mappings(new JsonObject
    {
        ["teamId"] = Of("integer"),
        ["name"] = TypeaheadName(),
        ["commonName"] = TypeaheadName(),
        ["placeName"] = TypeaheadName(),
        ["formerNames"] = Name(),
        ["abbreviation"] = Abbreviation(),
        ["logoUri"] = StoredOnly(),
        ["active"] = Of("boolean"),
        ["lastSeason"] = Of("integer"),
    }), Size: 4);

    public static readonly SearchIndex Players = new("nhl-players", Mappings(new JsonObject
    {
        ["playerId"] = Of("integer"),
        ["fullName"] = TypeaheadName(),
        ["position"] = Keyword(),
        ["teamId"] = Of("integer"),
        ["teamName"] = StoredOnly(),
        ["teamAbbreviation"] = Abbreviation(),
        ["isActive"] = Of("boolean"),
        ["headshot"] = StoredOnly(),
    }), Size: 6);

    private static JsonObject GameTeam() => new()
    {
        ["properties"] = new JsonObject
        {
            ["teamId"] = Of("integer"),
            // Copied into the game's "teams" field, so "bruins rangers" can match one name on each side
            ["name"] = new JsonObject { ["type"] = "text", ["analyzer"] = NameAnalyzer, ["copy_to"] = "teams" },
            ["abbreviation"] = new JsonObject { ["type"] = "keyword", ["normalizer"] = "lowercase", ["copy_to"] = "teams" },
            ["goals"] = Of("integer"),
        },
    };

    public static readonly SearchIndex Games = new("nhl-games", Mappings(new JsonObject
    {
        ["gameId"] = Of("integer"),
        // The game's date in Eastern time (the NHL's schedule date), for "oilers march 3"
        ["gameDate"] = new JsonObject { ["type"] = "date", ["format"] = "yyyy-MM-dd" },
        ["startTimeUtc"] = Of("date"),
        ["season"] = Of("integer"),
        ["gameType"] = Keyword(),
        ["played"] = Of("boolean"),
        ["home"] = GameTeam(),
        ["away"] = GameTeam(),
        ["teams"] = Name(),
    }), Size: 8);

    public static readonly SearchIndex Articles = new("nhl-articles", Mappings(new JsonObject
    {
        ["entityId"] = Keyword(),
        ["title"] = new JsonObject { ["type"] = "text", ["analyzer"] = ArticleAnalyzer },
        ["summary"] = new JsonObject { ["type"] = "text", ["analyzer"] = ArticleAnalyzer },
        // Plain text (markdown removed), so highlighted snippets read cleanly
        ["body"] = new JsonObject { ["type"] = "text", ["analyzer"] = ArticleAnalyzer },
        ["date"] = Of("date"),
        ["url"] = StoredOnly(),
        ["embeddingKey"] = StoredOnly(),
    }), Size: 6);

    public const string ArticleEmbeddingField = "embedding";

    /// <summary>
    /// The articles index with its vector field, sized for the model that made the vectors. Only mapped when some
    /// article has a vector: a dense_vector needs its dimensions up front. Cosine similarity, stored as int8 HNSW
    /// (a quarter of the memory of float32, for a recall loss that doesn't matter at a few thousand articles).
    /// </summary>
    public static SearchIndex ArticlesWithEmbeddings(int dims) => Articles.WithProperty(ArticleEmbeddingField, new JsonObject
    {
        ["type"] = "dense_vector",
        ["dims"] = dims,
        ["similarity"] = "cosine",
        ["index_options"] = new JsonObject { ["type"] = "int8_hnsw" },
    });

    public static readonly SearchIndex[] All = [Teams, Players, Games, Articles];
}
