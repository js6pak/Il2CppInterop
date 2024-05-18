using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Il2CppInterop.Common;
using Il2CppInterop.Generator.Extensions.Interop;
using Il2CppInterop.Generator.Imports;

namespace Il2CppInterop.Generator.Contexts.Interop;

internal abstract class InteropTypeContext
{
    public InteropGenerationContext GenerationContext => Assembly.GenerationContext;
    public InteropCorLibContext CorLibContext => GenerationContext.CorLibContext;
    public InteropAssemblyContext Assembly { get; }
    public ModuleDefinition Module => Assembly.MainModuleDefinition;
    public InteropImports Imports => Assembly.Imports;
    public TypeDefinition Original { get; }
    public TypeDefinition Definition { get; private set; } = null!;

    public InteropTypeContext? DeclaringType { get; }
    public string? Namespace { get; }
    public string Name { get; }

    public List<InteropTypeContext> NestedTypes { get; } = new();

    protected InteropTypeContext(InteropAssemblyContext assembly, TypeDefinition original, InteropTypeContext? declaringType = null)
    {
        Assembly = assembly;
        Original = original;
        DeclaringType = declaringType;

        Namespace = DeclaringType == null ? NamePrefix.Apply(Original.Namespace) : null;
        Name = Original.Name ?? throw new InvalidOperationException("Original type name can't be null");
    }

    protected virtual TypeDefinition CreateTypeDefinition()
    {
        var attributes = Original.Attributes & (TypeAttributes.Sealed | TypeAttributes.ClassSemanticsMask | TypeAttributes.LayoutMask);
        attributes |= DeclaringType == null ? TypeAttributes.Public : TypeAttributes.NestedPublic;
        return new TypeDefinition(Namespace, Name, attributes)
        {
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
            NestedTypes.Add(nested);
            Definition.NestedTypes.Add(nested.Definition);
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
            // TODO readd when valid
            // Definition.Interfaces.Add(new InterfaceImplementation(interfaceType));
        }
    }

    private void SetupGenerics()
    {
        Definition.SetupGenerics(Original, Assembly);
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
