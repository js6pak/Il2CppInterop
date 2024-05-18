using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Il2CppInterop.Common;
using Il2CppInterop.Generator.Extensions;
using Il2CppInterop.Generator.Imports;

namespace Il2CppInterop.Generator.Contexts.Interop;

internal class InteropAssemblyContext
{
    private readonly List<InteropTypeContext> _topLevelTypes = new();
    private readonly Dictionary<TypeDefinition, InteropTypeContext> _allTypesMap = new();

    public InteropGenerationContext GenerationContext { get; }
    public AssemblyDefinition OriginalAssembly { get; }
    public ModuleDefinition OriginalModule => OriginalAssembly.ManifestModule!;

    public AssemblyDefinition Definition { get; }
    public ModuleDefinition MainModuleDefinition { get; }

    public InteropReferenceImporter Importer { get; }
    public InteropImports Imports { get; }

    public IReadOnlyList<InteropTypeContext> TopLevelTypes => _topLevelTypes;
    public IReadOnlyCollection<InteropTypeContext> AllTypes => _allTypesMap.Values;

    public InteropAssemblyContext(InteropGenerationContext generationContext, AssemblyDefinition originalAssembly, SimpleAssemblyResolver assemblyResolver)
    {
        GenerationContext = generationContext;
        OriginalAssembly = originalAssembly;

        var name = NamePrefix.Apply(originalAssembly.Name);

        Definition = new AssemblyDefinition(name, originalAssembly.Version);
        assemblyResolver.Assemblies.Add(name, Definition);

        MainModuleDefinition = new ModuleDefinition(name + ".dll", KnownCorLibs.SystemRuntime_v7_0_0_0)
        {
            MetadataResolver = new DefaultMetadataResolver(assemblyResolver),
        };
        Definition.Modules.Add(MainModuleDefinition);

        Importer = new InteropReferenceImporter(this);

        Imports = new InteropImports(MainModuleDefinition);

        // TODO move this somewhere else
        var ignoresAccessChecksToAttributeConstructor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
            MethodSignature.CreateInstance(Imports.CorLibFactory.Void, Imports.CorLibFactory.String)
        );
        ignoresAccessChecksToAttributeConstructor.AddEmptyBody();
        ignoresAccessChecksToAttributeConstructor.Parameters.Single().GetOrCreateDefinition().Name = "assemblyName"; // TODO replace with DefineParameter extension

        var ignoresAccessChecksToAttribute = new TypeDefinition(
            "System.Runtime.CompilerServices",
            "IgnoresAccessChecksToAttribute",
            TypeAttributes.NotPublic | TypeAttributes.Sealed
        )
        {
            BaseType = Imports.Attribute,
            Methods =
            {
                ignoresAccessChecksToAttributeConstructor,
            },
        };
        MainModuleDefinition.TopLevelTypes.Add(ignoresAccessChecksToAttribute);

        Definition.CustomAttributes.Add(new CustomAttribute(
            ignoresAccessChecksToAttributeConstructor,
            new CustomAttributeSignature { FixedArguments = { new CustomAttributeArgument(Imports.CorLibFactory.String, "Il2CppInterop.Runtime") } }
        ));

        Definition.CustomAttributes.Add(new CustomAttribute(
            MainModuleDefinition.CorLibTypeFactory.CorLibScope.CreateTypeReference(MainModuleDefinition, "System.Runtime.CompilerServices", "DisableRuntimeMarshallingAttribute")
                .CreateMemberReference(".ctor", MethodSignature.CreateInstance(Imports.CorLibFactory.Void))
        ));
    }

    public InteropTypeContext GetContextForOriginal(TypeDefinition originalType) => _allTypesMap[originalType];

    internal void RegisterOriginal(InteropTypeContext typeContext)
    {
        _allTypesMap.Add(typeContext.Original, typeContext);
    }

    public virtual void CreateEmptyTypeDefinitions()
    {
        foreach (var originalType in OriginalModule.TopLevelTypes)
        {
            if (originalType.Name.Equals("<Module>"u8)) continue;
            if (originalType.Namespace.StartsWith("Cpp2ILInjected"u8)) continue;

            var interopType = InteropTypeContext.CreateForOriginal(this, originalType);
            _topLevelTypes.Add(interopType);
            MainModuleDefinition.TopLevelTypes.Add(interopType.Definition);
        }
    }

    public virtual void Setup()
    {
        foreach (var type in TopLevelTypes)
        {
            type.Setup();
        }
    }

    public virtual void Fill()
    {
        foreach (var type in TopLevelTypes)
        {
            type.Fill();
        }
    }

    public InteropTypeContext? FindType(string @namespace, string name)
    {
        foreach (var typeContext in TopLevelTypes)
        {
            if (typeContext.Namespace != @namespace || typeContext.Name != name)
            {
                continue;
            }

            return typeContext;
        }

        return null;
    }
}
