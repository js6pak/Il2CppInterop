using System.Diagnostics.CodeAnalysis;

namespace Il2CppInterop.Common;

public static class NamePrefix
{
    public static string[] Whitelist { get; } =
    {
        "UnityEngine", "Unity", "Il2Cpp",
    };

    public static bool IsWhitelisted([NotNullWhen(true)] string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        foreach (var whitelist in Whitelist)
        {
            if (value == whitelist || value.StartsWith(whitelist + "."))
            {
                return true;
            }
        }

        return false;
    }

    public static string Apply(string? value)
    {
        if (IsWhitelisted(value)) return value;
        return string.IsNullOrEmpty(value) ? "Il2Cpp" : "Il2Cpp." + value;
    }
}
