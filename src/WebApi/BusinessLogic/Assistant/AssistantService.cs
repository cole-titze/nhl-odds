using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Entities.ViewModels;
using WebApi.BusinessLogic.GameOddsGetter;
using WebApi.McpTools;

namespace WebApi.BusinessLogic.Assistant;

/// <summary>
/// Experimental chat assistant: sends the conversation to a local Ollama model and lets it call the
/// same read-only tools the MCP server exposes, so answers come from the site's own data.
/// </summary>
public class AssistantService(
    HttpClient http,
    IConfiguration config,
    GameOddsMcpTools gameTools,
    TeamMcpTools teamTools,
    IGameOddsGetter gameOddsGetter,
    ILogger<AssistantService> logger) : IAssistantService
{
    // Each round is a full model call on the Mac, so stop a model that keeps asking for tools
    private const int MaxToolRounds = 3;
    // GetTeamStats returns a whole season of games; keep tool results inside the model's context
    private const int MaxToolResultChars = 24_000;

    // Enums as names: the model read Winner 0 as "nobody won"
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // One question at a time across all visitors (both WebApi replicas share one Mac, so this halves it at worst)
    private static readonly SemaphoreSlim OneAtATime = new(1, 1);

    private static readonly JsonArray Tools =
    [
        Tool("get_todays_games", "Get today's NHL games with model win probabilities and bookmaker odds.",
            ("seasonStartYear", "integer", "Season start year, e.g. 2026 for 2026-27")),
        Tool("get_games_in_date_range", "Get NHL games, results, model odds and bookmaker lines for a date range.",
            ("startDate", "string", "Start date, yyyy-MM-dd"),
            ("endDate", "string", "End date, yyyy-MM-dd"),
            ("seasonStartYear", "integer", "Season start year")),
        Tool("get_game", "Get one game by its game ID (the number in a /game/{id} page URL).",
            ("gameId", "integer", "The NHL game ID")),
        Tool("get_all_teams", "Get every team's season record, stats and model accuracy (standings). No games.",
            ("seasonStartYear", "integer", "Season start year")),
        Tool("get_team_stats", "Get one team's stats and game history by team ID (the number in a /team/{id} page URL).",
            ("teamId", "integer", "The team ID"),
            ("seasonStartYear", "integer", "Season start year")),
    ];

    public async Task<string> Chat(AssistantRequest request, CancellationToken cancellationToken)
    {
        if (!await OneAtATime.WaitAsync(AssistantLimits.QueueWait, cancellationToken))
            throw new AssistantBusyException();
        try
        {
            return await Answer(request, cancellationToken);
        }
        finally
        {
            OneAtATime.Release();
        }
    }

    private async Task<string> Answer(AssistantRequest request, CancellationToken cancellationToken)
    {
        var messages = new JsonArray { Message("system", BuildSystemPrompt(request.Page)) };
        foreach (var m in request.Messages)
            messages.Add(Message(m.Role, m.Content));

        for (var round = 0; ; round++)
        {
            var body = new JsonObject
            {
                ["model"] = config["OLLAMA_MODEL"] ?? "qwen3.5:9b",
                ["messages"] = messages.DeepClone(),
                ["stream"] = false,
                // A 9B model spends most of a reply reasoning when thinking is on
                ["think"] = false,
                // get_all_teams alone is ~5k tokens; Ollama's default context would cut it off. Set
                // here so a stock model works without a custom 32k variant.
                ["options"] = new JsonObject { ["num_ctx"] = 32768 },
            };
            if (round < MaxToolRounds)
                body["tools"] = Tools.DeepClone();

            using var response = await http.PostAsJsonAsync("/api/chat", body, cancellationToken);
            response.EnsureSuccessStatusCode();
            var reply = (await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken))?["message"]?.AsObject()
                ?? throw new InvalidOperationException("Ollama returned no message");

            var toolCalls = reply["tool_calls"]?.AsArray();
            if (toolCalls is null || toolCalls.Count == 0)
                return reply["content"]?.GetValue<string>().Trim() ?? "";

            messages.Add(reply.DeepClone());
            foreach (var call in toolCalls)
            {
                var name = call?["function"]?["name"]?.GetValue<string>() ?? "";
                var args = call?["function"]?["arguments"]?.AsObject() ?? [];
                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_name"] = name,
                    ["content"] = await RunTool(name, args),
                });
            }
        }
    }

    private async Task<string> RunTool(string name, JsonObject args)
    {
        try
        {
            object? result = name switch
            {
                "get_todays_games" => await GetTodaysGames(Int(args, "seasonStartYear")),
                "get_games_in_date_range" => await gameTools.GetGamesInDateRange(
                    Date(args, "startDate"), Date(args, "endDate"), Int(args, "seasonStartYear")),
                "get_game" => await gameOddsGetter.GetGameOdds(Int(args, "gameId")),
                "get_all_teams" => await teamTools.GetAllTeams(Int(args, "seasonStartYear")),
                "get_team_stats" => await teamTools.GetTeamStats(Int(args, "teamId"), Int(args, "seasonStartYear")),
                _ => throw new ArgumentException($"Unknown tool {name}"),
            };
            var node = JsonSerializer.SerializeToNode(result, JsonOptions);
            Simplify(node);
            var json = node?.ToJsonString() ?? "null";
            return json.Length <= MaxToolResultChars ? json : json[..MaxToolResultChars] + " …(truncated)";
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Assistant tool {Tool} failed", name);
            return $"Error: {e.Message}";
        }
    }

    // A 9B model read an unplayed game's Winner HOME and 0-0 goals as a result and made up a score,
    // so unplayed games lose those fields and say so; logo URLs only cost context
    private static void Simplify(JsonNode? node)
    {
        switch (node)
        {
            case JsonArray array:
                foreach (var item in array)
                    Simplify(item);
                break;
            case JsonObject obj:
                obj.Remove("logoUri");
                // The model reads times as given, so hand it the site's Central time
                if (obj["gameDate"] is JsonValue date && DateTime.TryParse(date.ToString(), null,
                        System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var utc))
                {
                    obj.Remove("gameDate");
                    obj["startTimeCentral"] = TimeZoneInfo.ConvertTimeFromUtc(utc, Central).ToString("ddd MMM d, h:mm tt") + " CT";
                }
                if (obj["hasBeenPlayed"] is JsonValue played && !played.GetValue<bool>())
                {
                    obj.Remove("winner");
                    obj.Remove("logLoss");
                    obj["status"] = "not played yet";
                    foreach (var side in new[] { "homeTeam", "awayTeam" })
                        (obj[side] as JsonObject)?.Remove("goals");
                }
                foreach (var (_, child) in obj.ToList())
                    Simplify(child);
                break;
        }
    }

    // The site's "today" is Central time: after 7 PM CT the UTC date is already tomorrow, and a
    // night's late games start after midnight UTC, so fetch both UTC days and keep the Central date
    private async Task<IEnumerable<GameOddsVM>> GetTodaysGames(int seasonStartYear)
    {
        var today = CentralNow().Date;
        var games = await gameTools.GetGamesInDateRange(today, today.AddDays(1), seasonStartYear);
        return games.Where(g => TimeZoneInfo.ConvertTimeFromUtc(g.GameDate, Central).Date == today);
    }

    private static readonly TimeZoneInfo Central = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    private static DateTime CentralNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Central);

    private static string BuildSystemPrompt(string? page)
    {
        var today = CentralNow();
        var season = today >= new DateTime(today.Year, 9, 20) ? today.Year : today.Year - 1;
        return $"""
            You are a small assistant on an NHL odds website. The site shows its own machine-learning
            model's win probabilities next to bookmaker odds (DraftKings and Kalshi), plus team stats.
            Answer briefly (a few sentences) in plain text. Use the tools for any facts about games,
            teams or odds; never make numbers up. If the tools can't answer, say so.

            Today is {today:dddd yyyy-MM-dd} (US Central time, which game times use too). The current season is {season} ({season}-{(season + 1) % 100:00}).
            In game data, winner is HOME or AWAY and goals are each team's final score. A game with
            status "not played yet" has no result: never give a score or winner for it. Odds are win probabilities from 0 to 1. Log loss measures prediction accuracy; lower is
            better, and random guessing scores about 0.693.
            The user is on the page "{page ?? "/"}". If it's /game/{"{id}"} or /team/{"{id}"}, that ID is
            what "this game" or "this team" means.
            This is a hobby project; don't give betting advice beyond describing what the data shows.
            """;
    }

    private static JsonObject Message(string role, string content) =>
        new() { ["role"] = role, ["content"] = content };

    private static JsonObject Tool(string name, string description, params (string Name, string Type, string Description)[] parameters) => new()
    {
        ["type"] = "function",
        ["function"] = new JsonObject
        {
            ["name"] = name,
            ["description"] = description,
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(parameters.Select(p => KeyValuePair.Create(p.Name,
                    (JsonNode?)new JsonObject { ["type"] = p.Type, ["description"] = p.Description }))),
                ["required"] = new JsonArray(parameters.Select(p => (JsonNode?)p.Name).ToArray()),
            },
        },
    };

    // Small models sometimes send numbers as strings
    private static int Int(JsonObject args, string key) => args[key] switch
    {
        JsonValue v when v.TryGetValue<int>(out var i) => i,
        JsonValue v when int.TryParse(v.ToString(), out var i) => i,
        _ => throw new ArgumentException($"{key} must be an integer"),
    };

    private static DateTime Date(JsonObject args, string key) =>
        DateTime.TryParse(args[key]?.ToString(), out var d) ? d.Date : throw new ArgumentException($"{key} must be a date");
}