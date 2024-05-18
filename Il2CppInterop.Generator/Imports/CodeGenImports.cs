using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class CodeGenImports : TypeImports
{
    public MemberReference EnsureValueTypeSize { get; }

    public CodeGenImports(InteropImports imports) : base(imports, imports.Il2CppInteropRuntime.CreateTypeReference(imports.Module, "Il2CppInterop.Runtime", "Il2CppCodeGen"))
    {
        EnsureValueTypeSize = new MemberReference(Type, "EnsureValueTypeSize", MethodSignature.CreateStatic(imports.CorLibFactory.Void, 1));
    }
}
