using Microsoft.AspNetCore.Mvc;
using WebApi.BusinessLogic.Assistant;
using WebApi.Filters;

namespace WebApi.Controllers;

// Experimental: only enabled when OLLAMA_URL is set; otherwise Status says so (the site hides the widget) and Chat 404s.
// Chat has its own small rate-limit bucket (the "api" policy in Program.cs).
[Route("api/[controller]/[action]")]
[ApiController]
public class AssistantController(IConfiguration config, IServiceProvider services, IHttpContextAccessor http, ILogger<AssistantController> logger)
{
    private const int MaxMessages = 20;
    private const int MaxMessageChars = 2_000;

    private bool Enabled => !string.IsNullOrEmpty(config["OLLAMA_URL"]);

    [HttpGet]
    // 200 either way so a disabled assistant doesn't put a 404 in the metrics on every page view
    public IResult Status() => Enabled
        ? Results.Ok(new
        {
            enabled = true,
            burst = AssistantLimits.Burst,
            minutesPerQuestion = AssistantLimits.MinutesPerQuestion,
            // The admin key lifts the per-visitor limit (Program.cs rate limiter)
            unlimited = RequireAdminKeyAttribute.IsAdmin(http.HttpContext!),
        })
        : Results.Ok(new { enabled = false });

    [HttpPost]
    public async Task<IResult> Chat(AssistantRequest request, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return Results.NotFound();
        if (request.Messages is not { Count: > 0 and <= MaxMessages }
            || request.Messages.Any(m => m.Role is not ("user" or "assistant") || m.Content.Length > MaxMessageChars)
            || request.Messages[^1].Role != "user")
            return Results.BadRequest();

        try
        {
            var reply = await services.GetRequiredService<IAssistantService>().Chat(request, cancellationToken);
            return Results.Ok(new { reply });
        }
        catch (AssistantBusyException)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "Ollama request failed");
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}