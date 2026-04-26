using System.Text.Json.Nodes;

namespace Services.RequestMaker;

public interface IRequestMaker
{
    public Task<JsonNode?> MakeRequest(string url, string query, int throttleTime);
    public Task<JsonNode?> MakeRequest(string url, string query);
}