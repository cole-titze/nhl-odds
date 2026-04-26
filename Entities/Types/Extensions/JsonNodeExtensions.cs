using System.Text.Json;
using System.Text.Json.Nodes;

namespace Entities.Types;

public static class JsonNodeExtensions
{
    /// <summary>
    /// Gets an int from a JsonNode that may be a JSON number or a JSON string containing a number.
    /// The NHL API is inconsistent about whether certain fields (e.g. situationCode, season)
    /// are serialized as numbers or strings.
    /// </summary>
    public static int GetCoercedInt(this JsonNode node)
    {
        var element = node.GetValue<JsonElement>();
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetInt32(),
            JsonValueKind.String => int.Parse(element.GetString()!),
            _ => throw new InvalidOperationException($"Cannot convert {element.ValueKind} to Int32")
        };
    }

    /// <summary>
    /// Gets a string from a JsonNode that may be a JSON string or a JSON number.
    /// </summary>
    public static string GetCoercedString(this JsonNode node)
    {
        var element = node.GetValue<JsonElement>();
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.GetRawText(),
            _ => element.GetRawText()
        };
    }
}