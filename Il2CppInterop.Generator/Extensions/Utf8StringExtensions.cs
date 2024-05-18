using AsmResolver;
using AsmResolver.DotNet;

namespace Il2CppInterop.Generator.Extensions;

internal static class Utf8StringExtensions
{
    public static Utf8String ToUtf8String(this ReadOnlySpan<byte> span)
    {
        return new Utf8String(span.ToArray());
    }

    public static bool Equals(this Utf8String? utf8String, ReadOnlySpan<byte> value)
    {
        return utf8String is not null && MemoryExtensions.SequenceEqual(utf8String.GetBytesUnsafe(), value);
    }

    public static bool StartsWith(this Utf8String? utf8String, ReadOnlySpan<byte> value)
    {
        return utf8String is not null && MemoryExtensions.StartsWith(utf8String.GetBytesUnsafe(), value);
    }

    public static bool IsTypeOf(this ITypeDefOrRef type, ReadOnlySpan<byte> @namespace, ReadOnlySpan<byte> name)
    {
        return type.Namespace.Equals(@namespace) && type.Name.Equals(name);
    }
}
