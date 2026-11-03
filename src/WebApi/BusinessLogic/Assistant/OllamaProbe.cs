namespace WebApi.BusinessLogic.Assistant;

/// <summary>
/// Whether the Ollama server answers right now, so Status can hide the widget while the Mac is
/// asleep, locked or off. Every page view asks Status, so one answer is reused for a short while
/// (shorter when down, so the widget comes back soon after the Mac does) and visitors share one probe.
/// </summary>
public class OllamaProbe(HttpClient http, ILogger<OllamaProbe> logger, TimeProvider? time = null)
{
    public static readonly TimeSpan UpFor = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan DownFor = TimeSpan.FromSeconds(15);
    // The client's own timeout; a sleeping Mac usually just never answers
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly Lock _lock = new();
    private Task<bool>? _probe;
    private DateTimeOffset _expires;

    public Task<bool> IsReachable()
    {
        lock (_lock)
        {
            if (_probe is null || (_probe.IsCompleted && _time.GetUtcNow() >= _expires))
                _probe = Probe();
            return _probe;
        }
    }

    private async Task<bool> Probe()
    {
        bool up;
        try
        {
            // Cheapest Ollama endpoint: no model load
            using var response = await http.GetAsync("api/version");
            up = response.IsSuccessStatusCode;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            logger.LogInformation("Ollama unreachable, hiding the assistant: {Message}", e.Message);
            up = false;
        }
        lock (_lock)
            _expires = _time.GetUtcNow() + (up ? UpFor : DownFor);
        return up;
    }
}
