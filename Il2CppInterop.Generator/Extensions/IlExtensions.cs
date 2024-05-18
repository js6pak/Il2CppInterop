using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace Il2CppInterop.Generator.Extensions;

internal static class IlExtensions
{
    /// <summary>
    /// Optimizes macros and verifies the method after creation instead when writing to make stacktrace nicer.
    /// </summary>
    public static void OptimizeAndVerify(this CilMethodBody body)
    {
        body.Instructions.OptimizeMacros();

        body.Instructions.CalculateOffsets();
        body.VerifyLabels(false);
        body.MaxStack = body.ComputeMaxStack(false);

        body.ComputeMaxStackOnBuild = false;
        body.VerifyLabelsOnBuild = false;
    }

    /// <summary>
    /// Adds an instruction that loads the specified string or null.
    /// </summary>
    public static void AddLdStrOrNull(this CilInstructionCollection instructions, Utf8String? value)
    {
        if (value is null)
        {
            instructions.Add(CilOpCodes.Ldnull);
        }
        else
        {
            instructions.Add(CilOpCodes.Ldstr, value);
        }
    }

    public static void AddStackAlloc(this CilInstructionCollection instructions, ITypeDefOrRef type, int size)
    {
        // stackalloc {type}[{size}]
        instructions.Add(CilOpCodes.Ldc_I4, size);
        instructions.Add(CilOpCodes.Conv_U);
        instructions.Add(CilOpCodes.Sizeof, type);
        instructions.Add(CilOpCodes.Mul_Ovf_Un);
        instructions.Add(CilOpCodes.Localloc);
    }

    public static void AddIfStatement(this CilInstructionCollection instructions, Action condition, Action thenCase)
    {
        var endLabel = new CilNextInstructionLabel();

        condition();
        instructions.Add(CilOpCodes.Brfalse, endLabel);

        thenCase();
        instructions.Add(CilOpCodes.Br, endLabel);

        endLabel.Instruction = instructions[^1];
    }

    public static void AddConditionalReturn(this CilInstructionCollection instructions, Action condition, Action value, bool negated = false)
    {
        // if ({condition}) return {value};
        var elseLabel = new CilNextInstructionLabel();

        condition();
        instructions.Add(negated ? CilOpCodes.Brtrue : CilOpCodes.Brfalse, elseLabel);

        value();
        instructions.Add(CilOpCodes.Ret);

        elseLabel.Instruction = instructions[^1];
    }

    public static void AddIfStatement(this CilInstructionCollection instructions, Action condition, Action thenCase, Action elseCase)
    {
        var elseLabel = new CilNextInstructionLabel();
        var endLabel = new CilNextInstructionLabel();

        condition();
        instructions.Add(CilOpCodes.Brfalse, elseLabel);

        thenCase();
        instructions.Add(CilOpCodes.Br, endLabel);

        elseLabel.Instruction = instructions[^1];
        elseCase();

        endLabel.Instruction = instructions[^1];
    }

    /// <summary>
    /// <see cref="CilInstructionLabel"/> but points to the offset after the referenced instruction.
    /// </summary>
    private sealed class CilNextInstructionLabel : ICilLabel
    {
        /// <summary>
        /// Gets or sets the referenced instruction.
        /// </summary>
        public CilInstruction? Instruction { get; set; }

        /// <inheritdoc />
        public int Offset => Instruction != null ? Instruction.Offset + Instruction.Size : -1;

        /// <inheritdoc />
        public bool Equals(ICilLabel? other) => other is not null && Offset == other.Offset;

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as ICilLabel);

        /// <inheritdoc />
        public override int GetHashCode() => Offset;

        /// <inheritdoc />
        public override string ToString() => Instruction is null
            ? "IL_????"
            : $"IL_{Offset:X4}";
    }
}
