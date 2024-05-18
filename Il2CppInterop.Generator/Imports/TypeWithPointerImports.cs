using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Imports;

internal class TypeWithPointerImports : TypeImports
{
    public PointerTypeSignature Pointer { get; }

    public TypeWithPointerImports(InteropImports imports, ITypeDefOrRef type, bool isValueType = false) : base(imports, type)
    {
        Pointer = Type.MakePointerType(isValueType);
    }
}

internal class TypeWithPointerPointerImports : TypeWithPointerImports
{
    public PointerTypeSignature PointerPointer { get; }

    public TypeWithPointerPointerImports(InteropImports imports, ITypeDefOrRef type, bool isValueType = false) : base(imports, type, isValueType)
    {
        PointerPointer = Pointer.MakePointerType();
    }
}
