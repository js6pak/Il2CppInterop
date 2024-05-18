using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal sealed class RuntimeHelpersImports
{
    public ITypeDefOrRef Type { get; }
    public MemberReference RunClassConstructor { get; }

    public RuntimeHelpersImports(InteropImports imports)
    {
        Type = imports.CorLibScope.CreateTypeReference(imports.Module, "System.Runtime.CompilerServices", "RuntimeHelpers");

        RunClassConstructor = Type.CreateMemberReference("RunClassConstructor", MethodSignature.CreateStatic(
            imports.CorLibFactory.Void,
            imports.RuntimeTypeHandle.ToTypeSignature(true) // type
        ));
    }
}
