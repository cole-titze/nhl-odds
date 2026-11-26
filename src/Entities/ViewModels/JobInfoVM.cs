namespace Entities.ViewModels;

public class JobInfoVM
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "idle";
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Error { get; set; }
    public string Output { get; set; } = string.Empty;
    public bool CompletedToday { get; set; }
}
