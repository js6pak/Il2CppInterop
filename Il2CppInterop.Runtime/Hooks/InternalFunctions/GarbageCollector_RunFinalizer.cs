namespace Il2CppInterop.Runtime.Hooks.InternalFunctions;

// internal sealed class GarbageCollector_RunFinalizer : BaseInternalFunction
// {
//     public override string Name => "il2cpp::gc::GarbageCollector::RunFinalizer";
//
//     public override IntPtr Find(IFunctionScanner functionScanner)
//     {
//         var hasFinalizeCheckNode = cfg.Nodes.Single(n =>
//         {
//             var instructions = n.Contents.Instructions;
//             return instructions[^1].Mnemonic == Mnemonic.Je &&
//                    instructions[^2].Mnemonic == Mnemonic.Test &&
//                    instructions[^2].MemoryBase == Register.RCX /* obj param */ &&
//                    instructions[^2].MemoryDisplacement64 == Il2CppClass.Offsets.__bitfield_1 &&
//                    instructions[^2].GetImmediate(1) == Il2CppClass.Offsets.has_finalize;
//         });
//
//         var targetNode = hasFinalizeCheckNode.UnconditionalEdge.Target;
//         GetCalls(targetNode).Single();
//     }
// }
