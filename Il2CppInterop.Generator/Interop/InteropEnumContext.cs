using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace Il2CppInterop.Generator.Interop;

public sealed class InteropEnumContext : InteropTypeContext
{
    public InteropEnumContext(InteropAssemblyContext assembly, TypeDefinition original, InteropTypeContext? declaringType = null) : base(assembly, original, declaringType)
    {
    }

    protected override void SetupBaseType()
    {
        Definition.BaseType = Imports.Enum;
    }

    public override void Setup()
    {
        base.Setup();
        ConfigureFields();
    }

    private void ConfigureFields()
    {
        foreach (var originalField in Original.Fields)
        {
            var fieldSignature = originalField.Signature?.ImportWith(Assembly.Importer);
            var fieldDefinition = new FieldDefinition(originalField.Name, originalField.Attributes | FieldAttributes.HasDefault, fieldSignature);
            Definition.Fields.Add(fieldDefinition);

            if (originalField.Constant != null)
            {
                fieldDefinition.Constant = originalField.Constant;
            }
        }
    }
}
