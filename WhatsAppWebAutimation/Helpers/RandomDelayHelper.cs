namespace WhatsAppWebAutomation.Helpers;

/// <summary>
/// Thread-safe random delay helper for message timing
/// Uses Random.Shared for true randomness in .NET 8
/// </summary>
public static class RandomDelayHelper
{
    /// <summary>
    /// Get a random delay value between min and max (thread-safe)
    /// Each call returns a DIFFERENT random value
    /// Example: 34, 52, 41, 58, 30, 45 (all different)
    /// </summary>
    public static int GetRandomSeconds(int minSeconds, int maxSeconds)
    {
        if (minSeconds > maxSeconds)
        {
            (minSeconds, maxSeconds) = (maxSeconds, minSeconds);
        }

        // Random.Shared is thread-safe and provides true randomness
        var result = Random.Shared.Next(minSeconds, maxSeconds + 1);
        return result;
    }

    /// <summary>
    /// Wait for a random duration between min and max
    /// </summary>
    public static async Task WaitRandomAsync(int minSeconds, int maxSeconds, CancellationToken cancellationToken = default)
    {
        var seconds = GetRandomSeconds(minSeconds, maxSeconds);
        await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
    }

    /// <summary>
    /// Get random minutes for break duration
    /// </summary>
    public static int GetRandomMinutes(int minMinutes, int maxMinutes)
    {
        if (minMinutes > maxMinutes)
        {
            (minMinutes, maxMinutes) = (maxMinutes, minMinutes);
        }

        // Random.Shared is thread-safe and provides true randomness
        var result = Random.Shared.Next(minMinutes, maxMinutes + 1);
        return result;
    }

    /// <summary>
    /// Wait for a random break duration between min and max minutes
    /// </summary>
    public static async Task WaitRandomBreakAsync(int minMinutes, int maxMinutes, CancellationToken cancellationToken = default)
    {
        var minutes = GetRandomMinutes(minMinutes, maxMinutes);
        await Task.Delay(TimeSpan.FromMinutes(minutes), cancellationToken);
    }

    /// <summary>
    /// Get an aggressive/unpredictable break threshold
    /// Uses multiple random strategies for human-like unpredictability:
    /// - 15% chance: Very early break (50-80% of min)
    /// - 15% chance: Extended run (110-140% of max)
    /// - 20% chance: Lower half of range
    /// - 20% chance: Upper half of range
    /// - 30% chance: Random within range with bias
    /// </summary>
    public static int GetUnpredictableThreshold(int min, int max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        var strategy = Random.Shared.Next(100);
        int result;

        if (strategy < 15)
        {
            // Very early break - 50% to 80% of minimum
            var factor = 0.5 + (Random.Shared.NextDouble() * 0.3);
            result = Math.Max(1, (int)(min * factor));
        }
        else if (strategy < 30)
        {
            // Extended run - 110% to 140% of maximum
            var factor = 1.1 + (Random.Shared.NextDouble() * 0.3);
            result = (int)(max * factor);
        }
        else if (strategy < 50)
        {
            // Lower half of range
            result = Random.Shared.Next(min, min + (max - min) / 2 + 1);
        }
        else if (strategy < 70)
        {
            // Upper half of range
            result = Random.Shared.Next(min + (max - min) / 2, max + 1);
        }
        else
        {
            // Random within range but with second random for max (double randomization)
            var randomMax = Random.Shared.Next(min, max + 1);
            result = Random.Shared.Next(min, randomMax + 1);
        }

        return Math.Max(1, result);
    }
}
