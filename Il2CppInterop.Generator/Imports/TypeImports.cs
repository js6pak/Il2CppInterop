using AsmResolver.DotNet;

namespace Il2CppInterop.Generator.Imports;

internal class TypeImports
{
    public InteropImports Imports { get; set; }
    public ITypeDefOrRef Type { get; }

    public TypeImports(InteropImports imports, ITypeDefOrRef type)
    {
        Imports = imports;
        Type = type;
    }

    public virtual void SetupMemberReferences()
    {
    }
}
