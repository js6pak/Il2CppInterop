using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Il2CppInterop.Generator.Extensions;
using Il2CppInterop.Generator.Extensions.Interop;
using Il2CppInterop.Generator.Imports;

namespace Il2CppInterop.Generator.Contexts.Interop;

internal sealed class InteropMethodContext
{
    public InteropTypeContext DeclaringType { get; }
    public InteropImports Imports => DeclaringType.Imports;
    public MethodDefinition Original { get; }
    public uint Token { get; }
    public FieldDefinition MethodInfoField { get; }

    public MethodDefinition? Definition { get; private set; }

    public InteropMethodContext(InteropClassContext declaringType, MethodDefinition original)
    {
        DeclaringType = declaringType;
        Original = original;
        Token = Original.ExtractToken();

        MethodInfoField = new FieldDefinition(
            $"Il2CppMethodInfo_{Token.ToString()}_{Original.Name}",
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, // TODO private
            Imports.Structs.Il2CppMethod.Pointer
        );
    }

    public void Setup()
    {
        DeclaringType.Definition.Fields.Add(MethodInfoField);

        var signature = (MethodSignature?)Original.Signature?.ImportWith(DeclaringType.Assembly.Importer);
        Definition = new MethodDefinition(Original.Name, Original.Attributes, signature);
        Definition.IsPublic = true;

        foreach (var originalParameter in Original.ParameterDefinitions)
        {
            Definition.ParameterDefinitions.Add(new ParameterDefinition(originalParameter.Sequence, originalParameter.Name, originalParameter.Attributes));
        }

        Definition.SetupGenerics(Original, DeclaringType.Assembly);

        DeclaringType.Definition.Methods.Add(Definition);

        var body = Definition.CilMethodBody = new CilMethodBody(Definition);
        DeclaringType.GenerationContext.InteropMethodBodyGenerator.GenerateBody(this, Definition, body);
    }
}
