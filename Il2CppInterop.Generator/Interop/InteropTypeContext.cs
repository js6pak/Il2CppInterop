using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Il2CppInterop.Common;

namespace Il2CppInterop.Generator.Interop;

public abstract class InteropTypeContext
{
    public InteropGenerationContext GenerationContext => Assembly.GenerationContext;
    public InteropCorLibContext CorLibContext => GenerationContext.CorLibContext;
    public InteropAssemblyContext Assembly { get; }
    public ModuleDefinition Module => Assembly.MainModuleDefinition;
    public InteropImports Imports => Assembly.Imports;
    public TypeDefinition Original { get; }
    public TypeDefinition Definition { get; private set; } = null!;

    public InteropTypeContext? DeclaringType { get; }
    public string Namespace { get; }
    public string Name { get; }

    public List<InteropTypeContext> NestedTypes { get; } = new();

    protected InteropTypeContext(InteropAssemblyContext assembly, TypeDefinition original, InteropTypeContext? declaringType = null)
    {
        Assembly = assembly;
        Original = original;
        DeclaringType = declaringType;

        Namespace = NamePrefix.Apply(Original.Namespace);
        Name = Original.Name ?? throw new InvalidOperationException("Original type name can't be null");
    }

    protected virtual TypeDefinition CreateTypeDefinition()
    {
        return new TypeDefinition(Namespace, Name, Original.Attributes & (TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.LayoutMask))
        {
            IsPublic = true,
            ClassLayout = Original.ClassLayout,
        };
    }

    protected void InitializeTypeDefinition()
    {
        Definition = CreateTypeDefinition();
    }

    protected virtual void InitializeNested()
    {
        foreach (var originalNestedType in Original.NestedTypes)
        {
            var nested = CreateForOriginal(Assembly, originalNestedType, this);
            Definition.NestedTypes.Add(nested.Definition);
            NestedTypes.Add(nested);
        }
    }

    public virtual void Setup()
    {
        SetupBaseType();
        SetupInterfaces();
        SetupGenerics();

        foreach (var nestedType in NestedTypes)
        {
            nestedType.Setup();
        }
    }

    public virtual void Fill()
    {
        foreach (var nestedType in NestedTypes)
        {
            nestedType.Fill();
        }
    }

    protected virtual void SetupBaseType()
    {
        Definition.BaseType = Original.BaseType?.ImportWith(Assembly.Importer);
    }

    private void SetupInterfaces()
    {
        foreach (var originalInterface in Original.Interfaces)
        {
            var interfaceType = originalInterface.Interface?.ImportWith(Assembly.Importer);
            Definition.Interfaces.Add(new InterfaceImplementation(interfaceType));
        }
    }

    private void SetupGenerics()
    {
        foreach (var originalGenericParameter in Original.GenericParameters)
        {
            var genericParameter = new GenericParameter(originalGenericParameter.Name, originalGenericParameter.Attributes);

            foreach (var originalConstraint in originalGenericParameter.Constraints)
            {
                var constraint = originalConstraint.Constraint?.ImportWith(Assembly.Importer);
                genericParameter.Constraints.Add(new GenericParameterConstraint(constraint));
            }

            Definition.GenericParameters.Add(genericParameter);
        }
    }

    public static InteropTypeContext CreateForOriginal(InteropAssemblyContext assembly, TypeDefinition originalType, InteropTypeContext? declaringType = null)
    {
        InteropTypeContext result;

        if (originalType.IsEnum)
        {
            result = new InteropEnumContext(assembly, originalType, declaringType);
        }
        else if (originalType.IsValueType)
        {
            result = new InteropStructContext(assembly, originalType, declaringType);
        }
        else
        {
            result = new InteropClassContext(assembly, originalType, declaringType);
        }

        result.InitializeTypeDefinition();
        assembly.RegisterOriginal(result);
        result.InitializeNested();

        return result;
    }
}
