using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Il2CppInterop.Common;
using Il2CppInterop.Generator.Extensions;

namespace Il2CppInterop.Generator.Interop;

public sealed class InteropStructContext : InteropTypeContext
{
    private readonly InteropBoxedContext _boxed;

    public InteropStructContext(InteropAssemblyContext assembly, TypeDefinition original, InteropTypeContext? declaringType = null) : base(assembly, original, declaringType)
    {
        _boxed = new InteropBoxedContext(this);
    }

    protected override void InitializeNested()
    {
        base.InitializeNested();
        NestedTypes.Add(_boxed);
        Definition.NestedTypes.Add(_boxed.Definition);
    }

    public override void Fill()
    {
        base.Fill();
        FillFields();
        FillStaticConstructor();
    }

    private void FillFields()
    {
        foreach (var originalField in Original.Fields)
        {
            var fieldSignature = originalField.Signature.ImportWith(Assembly.Importer);
            var typeSignature = fieldSignature.FieldType;

            PropertyDefinition? property = null;

            if (typeSignature is SzArrayTypeSignature)
            {
                typeSignature = Imports.Structs.Il2CppArrayPointer;
                // TODO replace with Il2CppArray* and generate Il2CppArrayBase property
            }

            if (GenerationContext.CorLibContext.IsString(typeSignature))
            {
                typeSignature = Imports.Structs.Il2CppStringPointer;
            }
            else if (!typeSignature.IsValueType)
            {
                typeSignature = Imports.Structs.Il2CppObjectPointer;
                property = new PropertyDefinition(originalField.Name, PropertyAttributes.None, PropertySignature.CreateInstance(typeSignature));
                // property.SetSemanticMethods(
                //     new MethodDefinition()
                // );
            }

            var fieldDefinition = new FieldDefinition(originalField.Name, originalField.Attributes, typeSignature);
            if (property == null) fieldDefinition.IsPublic = true;
            else
            {
                fieldDefinition.Name = "_" + fieldDefinition.Name;
                fieldDefinition.IsPrivate = true;
                Definition.Properties.Add(property);
            }

            Definition.Fields.Add(fieldDefinition);

            fieldDefinition.Constant = originalField.Constant;
        }
    }

    protected override void SetupBaseType()
    {
        Definition.BaseType = Imports.ValueType;
    }

    private void FillStaticConstructor()
    {
        var methodDefinition = AsmResolverExtensions.CreateStaticConstructor(Module);

        var body = methodDefinition.CilMethodBody = new CilMethodBody(methodDefinition);

        // RuntimeHelpers.RunClassConstructor(typeof(Boxed).TypeHandle);
        body.Instructions.Add(CilOpCodes.Ldtoken, _boxed.Definition);
        body.Instructions.Add(CilOpCodes.Call, Imports.RunClassConstructor);

        // Il2CppCodeGen.EnsureValueTypeSize<{this}>();
        body.Instructions.Add(CilOpCodes.Call, Imports.CodeGen.EnsureValueTypeSize.MakeGenericInstanceMethod(Definition.ToTypeSignature()));

        body.Instructions.Add(CilOpCodes.Ret);

        Definition.Methods.Add(methodDefinition);
    }

    private sealed class InteropBoxedContext : InteropClassContext
    {
        private readonly InteropStructContext _declaringStruct;

        public InteropBoxedContext(InteropStructContext declaringStruct) : base(declaringStruct.Assembly, declaringStruct.Original, declaringStruct.DeclaringType)
        {
            _declaringStruct = declaringStruct;
            InitializeTypeDefinition();
            InitializeNested();
        }

        protected override void InitializeNested()
        {
        }

        protected override TypeDefinition CreateTypeDefinition()
        {
            return new TypeDefinition(NamePrefix.Apply(Original.Namespace), "Boxed", TypeAttributes.NestedPublic);
        }

        protected override void ConfigureStaticConstructorBody(CilMethodBody body, CilLocalVariable klassLocal)
        {
            // Il2CppClassPointerStore<?>.Pointer = klass;
            body.Instructions.Add(CilOpCodes.Ldloc, klassLocal);
            body.Instructions.Add(CilOpCodes.Call, Imports.Il2CppClassPointerStore.GenericSet(_declaringStruct.Definition.ToTypeSignature()));

            base.ConfigureStaticConstructorBody(body, klassLocal);
        }
    }
}
