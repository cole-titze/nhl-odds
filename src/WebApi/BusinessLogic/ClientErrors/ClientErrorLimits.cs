using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace WebApi.BusinessLogic.ClientErrors;

/// <summary>
/// Limits for <c>/api/ClientError</c>. Each visitor has a small bucket (the "api" policy in Program.cs), but that is
/// keyed on the client IP header, so <see cref="ClientErrorThrottle"/> also caps what all visitors together can write:
/// a broken deploy that throws for everyone, or someone rotating IPs, can't flood ErrorLog. Worst case per API pod is
/// <see cref="DailyCap"/> rows of at most about 10 KB a day.
/// </summary>
public static class ClientErrorLimits
{
    public const string Source = "Frontend";

    /// <summary>Reports one visitor can send at once, then one per <see cref="MinutesPerReport"/></summary>
    public const int Burst = 5;
    public const int MinutesPerReport = 1;

    /// <summary>Reports all visitors together can send at once, then one per <see cref="SecondsPerSharedReport"/> (per API pod)</summary>
    public const int SharedBurst = 20;
    public const int SecondsPerSharedReport = 30;
    /// <summary>Most reports saved per UTC day (per API pod); the rest are dropped</summary>
    public const int DailyCap = 500;
    /// <summary>The same error (kind, name and message) is saved once per this many minutes (per API pod)</summary>
    public const int DuplicateWindowMinutes = 10;

    public const int MaxRequestBytes = 16 * 1024;
    public const int MaxNameChars = 100;
    public const int MaxMessageChars = 1_000;
    public const int MaxPageChars = 200;
    public const int MaxUserAgentChars = 300;
    public const int MaxStackTraceChars = 4_000;
    public const int MaxVersionChars = 60;

    /// <summary>What caught the error: see <see cref="ClientErrorReport.Kind"/></summary>
    public static readonly string[] Kinds = ["error", "unhandledrejection", "render", "api", "chunk"];
}

/// <summary>
/// The shared caps on reports across all visitors (a singleton): a token bucket, a daily cap, and dropping an error
/// already saved in the last <see cref="ClientErrorLimits.DuplicateWindowMinutes"/> minutes.
/// </summary>
public class ClientErrorThrottle
{
    private readonly RateLimiter _limiter;
    private readonly int _dailyCap;
    private readonly TimeProvider _time;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _recent = new();
    private readonly Lock _dayLock = new();
    private DateOnly _day;
    private int _savedToday;

    public ClientErrorThrottle() : this(new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
    {
        TokenLimit = ClientErrorLimits.SharedBurst,
        ReplenishmentPeriod = TimeSpan.FromSeconds(ClientErrorLimits.SecondsPerSharedReport),
        TokensPerPeriod = 1,
        AutoReplenishment = true,
        QueueLimit = 0,
    }), ClientErrorLimits.DailyCap, TimeProvider.System)
    {
    }

    public ClientErrorThrottle(RateLimiter limiter, int dailyCap, TimeProvider time)
    {
        _limiter = limiter;
        _dailyCap = dailyCap;
        _time = time;
    }

    /// <summary>Why a report was let through or not, for the metric.</summary>
    public enum Decision { Allowed, Duplicate, OverRate, OverDailyCap }

    public Decision TryAcquire(string duplicateKey)
    {
        var now = _time.GetUtcNow();
        var window = TimeSpan.FromMinutes(ClientErrorLimits.DuplicateWindowMinutes);
        if (_recent.TryGetValue(duplicateKey, out var seen) && now - seen < window)
            return Decision.Duplicate;

        lock (_dayLock)
        {
            var today = DateOnly.FromDateTime(now.UtcDateTime);
            if (today != _day)
            {
                _day = today;
                _savedToday = 0;
            }
            if (_savedToday >= _dailyCap)
                return Decision.OverDailyCap;

            using var lease = _limiter.AttemptAcquire();
            if (!lease.IsAcquired)
                return Decision.OverRate;
            _savedToday++;
        }

        _recent[duplicateKey] = now;
        // Keys are bounded by the daily cap; drop expired ones now and then so they don't pile up across days
        if (_recent.Count > 2 * _dailyCap)
            foreach (var (key, time) in _recent)
                if (now - time >= window)
                    _recent.TryRemove(key, out _);
        return Decision.Allowed;
    }
}
