namespace Il2CppInterop.Generator.Extensions;

internal static class Extensions
{
    public static string ToPrettyString(this TimeSpan timeSpan)
    {
        if (timeSpan.Minutes > 0)
        {
            return $"{timeSpan.TotalMinutes:F3}m";
        }

        if (timeSpan.Seconds > 0)
        {
            return $"{timeSpan.TotalSeconds:F3}s";
        }

        return $"{timeSpan.TotalMilliseconds:F3}ms";
    }
}
