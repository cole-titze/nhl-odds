using Microsoft.AspNetCore.Mvc;
using WebApi.BusinessLogic.Assistant;

namespace WebApi.Controllers;

// Experimental: only enabled when OLLAMA_URL is set; otherwise every action 404s and the site hides the widget.
// Chat has its own small rate-limit bucket (the "api" policy in Program.cs).
[Route("api/[controller]/[action]")]
[ApiController]
public class AssistantController(IConfiguration config, IServiceProvider services, ILogger<AssistantController> logger)
{
    private const int MaxMessages = 20;
    private const int MaxMessageChars = 2_000;

    private bool Enabled => !string.IsNullOrEmpty(config["OLLAMA_URL"]);

    [HttpGet]
    public IResult Status() => Enabled ? Results.Ok(new { enabled = true }) : Results.NotFound();

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
        catch (HttpRequestException e)
        {
            logger.LogWarning(e, "Ollama request failed");
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}