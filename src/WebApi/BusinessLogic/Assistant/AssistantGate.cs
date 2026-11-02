using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Prometheus;

namespace WebApi.BusinessLogic.Assistant;

public enum GateResult { On, Off, Error }

/// <summary>
/// Quick "system one" check before the assistant's tool loop: one short model call with no tools
/// that answers ON or OFF, so off-topic questions (math problems, homework, code) don't cost a full
/// 32k-context answer on the Mac. Anything but a clear OFF lets the question through.
/// </summary>
public static class AssistantGate
{
    // A clear verdict takes a second or two; give up well before a visitor would
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    // The previous reply only gives a follow-up its context; it doesn't need all of it
    private const int MaxContextChars = 500;
    // Enough for the prompt, for a separate gate model (OLLAMA_GATE_MODEL)
    public const int ContextTokens = 2048;

    public const string OffTopicReply =
        "I can only answer questions about NHL games, teams, odds and this site's predictions.";

    private static readonly Counter Decisions = Metrics.CreateCounter(
        "assistant_gate_total", "Assistant topic check results", "result");

    private const string SystemPrompt = """
        You decide whether a message to the chat assistant of an NHL odds website is on topic.
        ON: anything about the NHL or hockey (games, teams, players, history, rules), betting odds,
        predictions, this website, greetings, or what the assistant can do. A short follow-up to
        the assistant's previous reply is ON.
        OFF: anything else, such as math problems, homework, writing, code, or other sports.
        Reply with exactly one word: ON or OFF.
        """;

    public static async Task<GateResult> Check(HttpClient http, string model, int contextTokens, IReadOnlyList<AssistantMessage> messages,
        ILogger logger, CancellationToken cancellationToken)
    {
        var question = messages[^1].Content;
        GateResult result;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            var body = new JsonObject
            {
                ["model"] = model,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "system", ["content"] = SystemPrompt },
                    new JsonObject { ["role"] = "user", ["content"] = BuildPrompt(messages) },
                },
                ["stream"] = false,
                ["think"] = false,
                ["options"] = new JsonObject
                {
                    ["num_ctx"] = contextTokens,
                    ["num_predict"] = 4,
                    ["temperature"] = 0,
                },
            };
            using var response = await http.PostAsJsonAsync("/api/chat", body, timeout.Token);
            response.EnsureSuccessStatusCode();
            var reply = (await response.Content.ReadFromJsonAsync<JsonObject>(timeout.Token))
                ?["message"]?["content"]?.GetValue<string>();
            result = Parse(reply);
            if (result == GateResult.Error)
                logger.LogWarning("Assistant gate gave an unclear verdict {Reply}", reply);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            // Includes our own timeout; the visitor's cancellation still propagates
            logger.LogWarning(e, "Assistant gate failed, letting the question through");
            result = GateResult.Error;
        }

        Decisions.WithLabels(result.ToString().ToLowerInvariant()).Inc();
        logger.LogInformation("Assistant gate {Result}: {Question}", result,
            question.Length <= 200 ? question : question[..200] + "…");
        return result;
    }

    // The question and, for follow-ups, the reply it follows, in one message so the model
    // classifies them rather than answering
    private static string BuildPrompt(IReadOnlyList<AssistantMessage> messages)
    {
        var question = messages[^1].Content;
        if (messages.Count < 2 || messages[^2].Role != "assistant")
            return $"Message: {question}";
        var previous = messages[^2].Content;
        if (previous.Length > MaxContextChars)
            previous = previous[..MaxContextChars] + "…";
        return $"Assistant's previous reply: {previous}\n\nMessage: {question}";
    }

    public static GateResult Parse(string? reply)
    {
        var word = new string((reply ?? "").Trim().TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
        return word switch
        {
            "ON" => GateResult.On,
            "OFF" => GateResult.Off,
            _ => GateResult.Error,
        };
    }
}
