using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Interop;

public sealed class InteropReferenceImporter : ReferenceImporter, ITypeSignatureVisitor<TypeSignature>
{
    private readonly InteropAssemblyContext _context;
    private readonly InteropGenerationContext _generationContext;

    public InteropReferenceImporter(InteropAssemblyContext context) : base(context.MainModuleDefinition)
    {
        _context = context;
        _generationContext = context.GenerationContext;
    }

    protected override AssemblyReference ImportAssembly(AssemblyDescriptor assembly)
    {
        var resolved = assembly.Resolve();
        if (resolved == null) throw new InvalidOperationException("Failed to resolve " + assembly);

        if (_generationContext.TryGetContextForOriginal(resolved, out var context))
        {
            return base.ImportAssembly(context.Definition);
        }

        return base.ImportAssembly(assembly);
    }

    protected override FileReference ImportFile(FileReference file)
    {
        throw new NotImplementedException();
    }

    public override ModuleReference ImportModule(ModuleReference module)
    {
        throw new NotImplementedException();
    }

    protected override ITypeDefOrRef ImportType(TypeDefinition type)
    {
        return base.ImportType(_generationContext.GetInteropTypeForOriginal(type));
    }

    protected override ITypeDefOrRef ImportType(TypeReference type)
    {
        var resolved = type.Resolve();
        if (resolved == null) throw new InvalidOperationException("Failed to resolve " + type);
        return ImportType(resolved);
    }

    public override ExportedType ImportType(ExportedType type)
    {
        throw new NotImplementedException();
    }

    public override ITypeDefOrRef ImportType(Type type) => throw new NotSupportedException();
    public override TypeSignature ImportTypeSignature(Type type) => throw new NotSupportedException();
    public override IMethodDescriptor ImportMethod(MethodBase method) => throw new NotSupportedException();

    // TODO do something to type members?
    public override MethodSpecification ImportMethod(MethodSpecification method) => base.ImportMethod(method);
    public override IFieldDescriptor ImportField(IFieldDescriptor field) => base.ImportField(field);

    TypeSignature ITypeSignatureVisitor<TypeSignature>.VisitCorLibType(CorLibTypeSignature signature)
    {
        if (signature.ElementType.IsBlittable())
        {
            return TargetModule.CorLibTypeFactory.FromElementType(signature.ElementType)!;
        }

        return new TypeDefOrRefSignature(ImportType(signature.Type), signature.IsValueType);
    }
}
