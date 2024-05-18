namespace Il2CppInterop.Generator;

public sealed record GeneratorOptions(InputContext Input, string OutputPath)
{
    public InteropMethodBodyType InteropMethodBodyType { get; init; } = InteropMethodBodyType.RuntimeInvoke;
}

public enum InteropMethodBodyType
{
    /// <summary>
    /// The default and most stable option.
    /// Uses il2cpp_runtime_invoke which adds some overhead and requires the return type to always be boxed.
    /// </summary>
    RuntimeInvoke,

    /// <summary>
    /// The experimental but fastest option.
    /// Calls the method pointer directly ensuring no overhead, which requires us to handle the exceptions.
    /// </summary>
    Direct,
}
