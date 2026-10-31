namespace WebApi.BusinessLogic.Assistant;

public record AssistantMessage(string Role, string Content);

public record AssistantRequest(List<AssistantMessage> Messages, string? Page);

// Thrown when another visitor's question is still running past AssistantLimits.QueueWait
public class AssistantBusyException() : Exception("The assistant is busy");

public interface IAssistantService
{
    Task<string> Chat(AssistantRequest request, CancellationToken cancellationToken);
}