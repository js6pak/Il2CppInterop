using AsmResolver;
using AsmResolver.DotNet;

namespace Il2CppInterop.Generator.Extensions;

internal static class CustomAttributeExtensions
{
    private static CustomAttribute? GetCustomAttribute(IHasCustomAttribute hasCustomAttribute, string attributeName)
    {
        return hasCustomAttribute.CustomAttributes.SingleOrDefault(a => a.Constructor?.DeclaringType?.Name == attributeName);
    }

    private static string? GetNamedAttribute(CustomAttribute customAttribute, string argumentName)
    {
        var argument = customAttribute.Signature!.NamedArguments.SingleOrDefault(x => x.MemberName == argumentName);
        return (Utf8String?)argument?.Argument.Element;
    }

    private static string? Extract(IHasCustomAttribute hasCustomAttribute, string attributeName, string argumentName)
    {
        var customAttribute = GetCustomAttribute(hasCustomAttribute, attributeName);
        return customAttribute == null ? null : GetNamedAttribute(customAttribute, argumentName);
    }

    public static uint ExtractToken(this IHasCustomAttribute hasCustomAttribute)
    {
        var raw = Extract(hasCustomAttribute, "TokenAttribute", "Token") ?? throw new ArgumentException($"{hasCustomAttribute} is missing a token attribute");
        return Convert.ToUInt32(raw, 16);
    }

    public static (ulong Rva, long Offset, int Length) ExtractAddress(this IHasCustomAttribute hasCustomAttribute)
    {
        var customAttribute = GetCustomAttribute(hasCustomAttribute, "AddressAttribute") ?? throw new ArgumentException($"{hasCustomAttribute} is missing an address attribute");
        return (
            Convert.ToUInt64(GetNamedAttribute(customAttribute, "RVA") ?? throw new ArgumentException($"{hasCustomAttribute} is missing RVA"), 16),
            Convert.ToInt64(GetNamedAttribute(customAttribute, "Offset") ?? throw new ArgumentException($"{hasCustomAttribute} is missing offset"), 16),
            Convert.ToInt32(GetNamedAttribute(customAttribute, "Length") ?? throw new ArgumentException($"{hasCustomAttribute} is missing length"), 16)
        );
    }
}
