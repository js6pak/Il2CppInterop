using Echo.Code;
using Echo.ControlFlow.Construction.Static;
using Echo.Platforms.Iced;
using Iced.Intel;

namespace Il2CppInterop.Runtime.Hooks;

internal class X86FunctionScanner : FunctionScanner<X86FunctionScanner, Instruction>, IFunctionScanner<Instruction>
{
    public X86FunctionScanner(GameAssemblyModule gameAssemblyModule) : base(gameAssemblyModule)
    {
    }

    public static IStaticInstructionProvider<Instruction> CreateStaticInstructionProvider(GameAssemblyModule gameAssemblyModule)
    {
        var architecture = new X86Architecture();
        var stream = gameAssemblyModule.Stream;
        var bitness = IntPtr.Size * 8;

        return new X86DecoderInstructionProvider(architecture, stream, bitness);
    }

    public static IStaticSuccessorResolver<Instruction> CreateStaticSuccessorResolver() => new X86StaticSuccessorResolver();
}
