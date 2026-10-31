namespace WebApi.BusinessLogic.Assistant;

public record AssistantMessage(string Role, string Content);

public record AssistantRequest(List<AssistantMessage> Messages, string? Page);

public interface IAssistantService
{
    Task<string> Chat(AssistantRequest request, CancellationToken cancellationToken);
}