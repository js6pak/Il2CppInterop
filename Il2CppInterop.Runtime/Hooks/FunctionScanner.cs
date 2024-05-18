using Echo.Code;
using Echo.ControlFlow.Construction.Static;

namespace Il2CppInterop.Runtime.Hooks;

internal interface IFunctionScanner
{
}

internal interface IFunctionScanner<TInstruction> : IFunctionScanner
{
    static abstract IStaticInstructionProvider<TInstruction> CreateStaticInstructionProvider(GameAssemblyModule gameAssemblyModule);
    static abstract IStaticSuccessorResolver<TInstruction> CreateStaticSuccessorResolver();
}

internal abstract class FunctionScanner<TSelf, TInstruction> : IFunctionScanner
    where TSelf : FunctionScanner<TSelf, TInstruction>, IFunctionScanner<TInstruction>
{
    private readonly StaticFlowGraphBuilder<TInstruction> _cfgBuilder;

    protected FunctionScanner(GameAssemblyModule gameAssemblyModule)
    {
        var instructionProvider = TSelf.CreateStaticInstructionProvider(gameAssemblyModule);

        _cfgBuilder = new StaticFlowGraphBuilder<TInstruction>(instructionProvider, TSelf.CreateStaticSuccessorResolver());
    }
}
