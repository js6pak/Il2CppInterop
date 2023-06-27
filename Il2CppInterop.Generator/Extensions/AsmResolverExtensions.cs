using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace Il2CppInterop.Generator.Extensions;

internal static class AsmResolverExtensions
{
    public static TypeReference CreateTypeReference(this IResolutionScope scope, ModuleDefinition? module, string? ns, string name)
    {
        return new TypeReference(module, scope, ns, name);
    }

    public static MethodDefinition CreateStaticConstructor(ModuleDefinition module)
    {
        return new MethodDefinition(
            ".cctor",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName | MethodAttributes.Static,
            MethodSignature.CreateStatic(module.CorLibTypeFactory.Void)
        );
    }

    public static bool IsBlittable(this ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Void:
            case ElementType.Boolean:
            case ElementType.Char:
            case ElementType.I1 or ElementType.I2 or ElementType.I4 or ElementType.I8:
            case ElementType.U1 or ElementType.U2 or ElementType.U4 or ElementType.U8:
            case ElementType.R4 or ElementType.R8:
            case ElementType.I or ElementType.U:
                return true;

            default:
                return false;
        }
    }
}
