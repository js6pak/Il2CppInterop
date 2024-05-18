using System.Diagnostics;
using AsmResolver.DotNet;
using Il2CppInterop.Generator.Contexts.Interop;

namespace Il2CppInterop.Generator.Extensions.Interop;

internal static class SetupExtensions
{
    public static void SetupCustomAttributes(this IHasCustomAttribute definition, IHasCustomAttribute original, InteropAssemblyContext context)
    {
        foreach (var originalCustomAttribute in original.CustomAttributes)
        {
            definition.CustomAttributes.Add(new CustomAttribute(
                (ICustomAttributeType?)originalCustomAttribute.Constructor?.ImportWith(context.Importer),
                context.Importer.ImportCustomAttributeSignature(originalCustomAttribute.Signature))
            );
        }
    }

    public static void SetupGenerics(this IHasGenericParameters definition, IHasGenericParameters original, InteropAssemblyContext context)
    {
        foreach (var originalGenericParameter in original.GenericParameters)
        {
            var genericParameter = new GenericParameter(originalGenericParameter.Name, originalGenericParameter.Attributes);

            foreach (var originalConstraint in originalGenericParameter.Constraints)
            {
                Debug.Assert(originalConstraint.Constraint != null);

                ITypeDefOrRef constraint;

                // We special case interop structs and enums to be real so do it here too
                if (originalConstraint.Constraint.IsTypeOf("System", "ValueType"))
                {
                    constraint = context.Imports.ValueType;
                }
                else if (originalConstraint.Constraint.IsTypeOf("System", "Enum"))
                {
                    constraint = context.Imports.Enum;
                }
                else
                {
                    constraint = originalConstraint.Constraint.ImportWith(context.Importer);
                }

                genericParameter.Constraints.Add(new GenericParameterConstraint(constraint));
            }

            // Custom attributes on generic parameters are unsupported by il2cpp unfortunately
            // genericParameter.SetupCustomAttributes(originalGenericParameter, context);

            definition.GenericParameters.Add(genericParameter);
        }
    }
}
