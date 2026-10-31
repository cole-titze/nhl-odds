namespace WebApi.BusinessLogic.Assistant;

// The assistant runs on one home computer, so it's limited hard. Status sends these to the widget so
// its explanation always matches.
public static class AssistantLimits
{
    // Per visitor: a burst of this many questions...
    public const int Burst = 3;
    // ...then one more every this many minutes
    public const int MinutesPerQuestion = 2;
    // Across everyone: the Mac answers one question at a time; others wait this long, then get 503
    public static readonly TimeSpan QueueWait = TimeSpan.FromSeconds(45);
}
