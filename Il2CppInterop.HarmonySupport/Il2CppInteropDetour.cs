using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.MethodInfo;
using Microsoft.Extensions.Logging;
using MonoMod.Cil;
using MonoMod.Core;
using MonoMod.Utils;
using ValueType = Il2CppSystem.ValueType;

namespace Il2CppInterop.HarmonySupport;

internal sealed class Il2CppInteropDetour : ICoreDetourWithClone
{
    private static readonly MethodInfo IL2CPPToManagedStringMethodInfo
        = typeof(IL2CPP).GetMethod(nameof(IL2CPP.Il2CppStringToManaged))!;

    private static readonly MethodInfo ManagedToIL2CPPStringMethodInfo
        = typeof(IL2CPP).GetMethod(nameof(IL2CPP.ManagedStringToIl2Cpp))!;

    private static readonly MethodInfo ObjectBaseToPtrMethodInfo
        = typeof(IL2CPP).GetMethod(nameof(IL2CPP.Il2CppObjectBaseToPtr))!;

    private static readonly MethodInfo ObjectBaseToPtrNotNullMethodInfo
        = typeof(IL2CPP).GetMethod(nameof(IL2CPP.Il2CppObjectBaseToPtrNotNull))!;

    private static readonly MethodInfo ReportExceptionMethodInfo
        = typeof(Il2CppInteropDetour).GetMethod(nameof(ReportException), BindingFlags.NonPublic | BindingFlags.Static)!;

    // Map each value type to correctly sized store opcode to prevent memory overwrite
    // Special case: bool is byte in Il2Cpp
    private static readonly Dictionary<Type, OpCode> StIndOpcodes = new()
    {
        [typeof(byte)] = OpCodes.Stind_I1,
        [typeof(sbyte)] = OpCodes.Stind_I1,
        [typeof(bool)] = OpCodes.Stind_I1,
        [typeof(short)] = OpCodes.Stind_I2,
        [typeof(ushort)] = OpCodes.Stind_I2,
        [typeof(int)] = OpCodes.Stind_I4,
        [typeof(uint)] = OpCodes.Stind_I4,
        [typeof(long)] = OpCodes.Stind_I8,
        [typeof(ulong)] = OpCodes.Stind_I8,
        [typeof(float)] = OpCodes.Stind_R4,
        [typeof(double)] = OpCodes.Stind_R8
    };

    private static readonly List<object> DelegateCache = new();

    private readonly INativeMethodInfoStruct _nativeSourceClone;
    private readonly Delegate _thunkDelegate;

    private ICoreDetour? _detour;
    private ICoreNativeDetour? _nativeDetour;

    public MethodBase Source { get; }
    public INativeMethodInfoStruct NativeSource { get; }
    public MethodBase Target { get; }

    public bool IsApplied { get; }

    public DynamicMethodDefinition SourceMethodCloneIL { get; private set; }
    public MethodInfo SourceMethodClone { get; private set; }

    public Il2CppInteropDetour(MethodBase source, INativeMethodInfoStruct nativeSource, MethodBase target)
    {
        Source = source;
        NativeSource = nativeSource;
        Target = target;

        _nativeSourceClone = UnityVersionHandler.NewMethod();
        unsafe
        {
            Buffer.MemoryCopy(NativeSource.MethodInfoPointer, _nativeSourceClone.MethodInfoPointer, UnityVersionHandler.MethodSize(), UnityVersionHandler.MethodSize());
        }

        SourceMethodCloneIL = CopyOriginal();
        SourceMethodClone = SourceMethodCloneIL.Generate();

        var thunk = GenerateNativeToManagedThunk((MethodInfo)Target).Generate();
        _thunkDelegate = thunk.CreateDelegate(DelegateTypeFactory.CreateDelegateType(thunk, CallingConvention.Cdecl));
    }

    public void Apply()
    {
        if (NativeSource.MethodPointer == default)
        {
            Logger.Instance.LogWarning("Method pointer for {Source} was null", Source.Name);
            return;
        }

        // Unpatch an existing detour if it exists
        if (_nativeDetour != null)
        {
            // Point back to the original method before we unpatch
            _nativeSourceClone.MethodPointer = NativeSource.MethodPointer;
            _nativeDetour.Dispose();
        }

        _detour = DetourFactory.Current.CreateDetour(Source, Target);

        _nativeDetour = DetourFactory.Current.CreateNativeDetour(NativeSource.MethodPointer, Marshal.GetFunctionPointerForDelegate(_thunkDelegate));
        if (!_nativeDetour.HasOrigEntrypoint)
        {
            throw new Exception("HasOrigEntrypoint has to be true");
        }

        _nativeSourceClone.MethodPointer = _nativeDetour.OrigEntrypoint;
    }

    public void Undo()
    {
        _detour?.Undo();
        _nativeDetour?.Undo();
    }

    public void Dispose()
    {
        _detour?.Dispose();
        _nativeDetour?.Dispose();

        Marshal.FreeHGlobal(_nativeSourceClone.Pointer);
    }

    private DynamicMethodDefinition CopyOriginal()
    {
        var dmd = new DynamicMethodDefinition(Source);
        dmd.Definition.Name = "UnhollowedWrapper_" + dmd.Definition.Name;
        var cursor = new ILCursor(new ILContext(dmd.Definition));


        // Remove il2cpp_object_get_virtual_method
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(IL2CPP),
                    nameof(IL2CPP.Il2CppObjectBaseToPtr)),
                x => x.MatchLdsfld(out _),
                x => x.MatchCall(typeof(IL2CPP),
                    nameof(IL2CPP.il2cpp_object_get_virtual_method))))
        {
            cursor.RemoveRange(4);
        }
        else
        {
            cursor.Goto(0)
                .GotoNext(x =>
                    x.MatchLdsfld(Il2CppInteropUtils
                        .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(Source)))
                .Remove();
        }

        // Replace original IL2CPPMethodInfo pointer with a modified one that points to the trampoline
        cursor
            .Emit(Mono.Cecil.Cil.OpCodes.Ldc_I8, _nativeSourceClone.Pointer.ToInt64())
            .Emit(Mono.Cecil.Cil.OpCodes.Conv_I);

        return dmd;
    }

    // Tries to guess whether a function needs a return buffer for the return struct, in all cases except win64 it's undefined behaviour
    private static bool IsReturnBufferNeeded(int size)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // https://learn.microsoft.com/en-us/cpp/build/x64-calling-convention?view=msvc-170#return-values
            return size != 1 && size != 4 && size != 8;
        }

        if (Environment.Is64BitProcess)
        {
            // x64 gcc and clang seem to use a return buffer for everything above 16 bytes
            return size > 16;
        }

        // Looks like on x32 gcc and clang return buffer is always used
        return true;
    }

    private static Type GetReturnType(MethodBase methodOrConstructor)
    {
        return methodOrConstructor is ConstructorInfo ? typeof(void) : ((MethodInfo)methodOrConstructor).ReturnType;
    }

    private DynamicMethodDefinition GenerateNativeToManagedThunk(MethodInfo targetManagedMethodInfo)
    {
        // managedParams are the interop types used on the managed side
        // unmanagedParams are IntPtr references that are used by IL2CPP compiled assembly
        var paramStartIndex = 0;

        var managedReturnType = GetReturnType(Source);
        var unmanagedReturnType = managedReturnType.NativeType();

        var returnSize = IntPtr.Size;

        var isReturnValueType = managedReturnType.IsSubclassOf(typeof(ValueType));
        if (isReturnValueType)
        {
            uint align = 0;
            returnSize = IL2CPP.il2cpp_class_value_size(Il2CppClassPointerStore.GetNativeClassPointer(managedReturnType), ref align);
        }

        var hasReturnBuffer = isReturnValueType && IsReturnBufferNeeded(returnSize);
        if (hasReturnBuffer)
            // C compilers seem to return large structs by allocating a return buffer on caller's side and passing it as the first parameter
            // TODO: Handle ARM
            // TODO: Check if this applies to values other than structs
        {
            unmanagedReturnType = typeof(IntPtr);
            paramStartIndex++;
        }

        if (!Source.IsStatic)
        {
            paramStartIndex++;
        }

        var managedParams = Source.GetParameters().Select(x => x.ParameterType).ToArray();
        var unmanagedParams =
            new Type[managedParams.Length + paramStartIndex +
                     1]; // +1 for methodInfo at the end

        if (hasReturnBuffer)
            // With GCC the return buffer seems to be the first param, same is likely with other compilers too
        {
            unmanagedParams[0] = typeof(IntPtr);
        }

        if (!Source.IsStatic)
        {
            unmanagedParams[paramStartIndex - 1] = typeof(IntPtr);
        }

        unmanagedParams[^1] = typeof(Il2CppMethodInfo*);
        Array.Copy(managedParams.Select(TrampolineHelpers.NativeType).ToArray(), 0,
            unmanagedParams, paramStartIndex, managedParams.Length);

        var dmd = new DynamicMethodDefinition("(il2cpp -> managed) " + Source.Name,
            unmanagedReturnType,
            unmanagedParams
        );

        var il = dmd.GetILGenerator();
        il.BeginExceptionBlock();

        // Declare a list of variables to dereference back to the original pointers.
        // This is required due to the needed interop type conversions, so we can't directly pass some addresses as byref types
        var indirectVariables = new LocalBuilder[managedParams.Length];

        if (!Source.IsStatic)
        {
            EmitConvertArgumentToManaged(il, paramStartIndex - 1, Source.DeclaringType, out _);
        }

        for (var i = 0; i < managedParams.Length; ++i)
        {
            EmitConvertArgumentToManaged(il, i + paramStartIndex, managedParams[i], out indirectVariables[i]);
        }

        // Run the managed method
        il.Emit(OpCodes.Call, targetManagedMethodInfo);

        // Store the managed return type temporarily (if there was one)
        LocalBuilder managedReturnVariable = null;
        if (managedReturnType != typeof(void))
        {
            managedReturnVariable = il.DeclareLocal(managedReturnType);
            il.Emit(OpCodes.Stloc, managedReturnVariable);
        }

        // Convert any managed byref values into their relevant IL2CPP types, and then store the values into their relevant dereferenced pointers
        for (var i = 0; i < managedParams.Length; ++i)
        {
            if (indirectVariables[i] == null)
            {
                continue;
            }

            il.Emit(OpCodes.Ldarg_S, i + paramStartIndex);
            il.Emit(OpCodes.Ldloc, indirectVariables[i]);
            var directType = managedParams[i].GetElementType();
            EmitConvertManagedTypeToIL2CPP(il, directType);
            il.Emit(StIndOpcodes.TryGetValue(directType, out var stindOpCodde) ? stindOpCodde : OpCodes.Stind_I);
        }

        // Handle any lingering exceptions
        il.BeginCatchBlock(typeof(Exception));
        il.Emit(OpCodes.Call, ReportExceptionMethodInfo);
        il.EndExceptionBlock();

        // Convert the return value back to an IL2CPP friendly type (if there was a return value), and then return
        if (managedReturnVariable != null)
        {
            if (hasReturnBuffer)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc, managedReturnVariable);
                il.Emit(OpCodes.Call, ObjectBaseToPtrNotNullMethodInfo);
                EmitUnbox(il);
                il.Emit(OpCodes.Ldc_I4, returnSize);
                il.Emit(OpCodes.Cpblk);

                // Return the same pointer to the return buffer
                il.Emit(OpCodes.Ldarg_0);
            }
            else
            {
                il.Emit(OpCodes.Ldloc, managedReturnVariable);
                EmitConvertManagedTypeToIL2CPP(il, managedReturnType);
            }
        }

        il.Emit(OpCodes.Ret);

        return dmd;
    }

    private static void EmitUnbox(ILGenerator il)
    {
        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Conv_I);
        il.Emit(OpCodes.Sizeof, typeof(void*));
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Add);
    }

    private static void ReportException(Exception ex) =>
        Logger.Instance.LogError(ex, "During invoking native->managed trampoline");

    private static void EmitConvertManagedTypeToIL2CPP(ILGenerator il, Type returnType)
    {
        if (returnType == typeof(string))
        {
            il.Emit(OpCodes.Call, ManagedToIL2CPPStringMethodInfo);
        }
        else if (!returnType.IsValueType && returnType.IsSubclassOf(typeof(Il2CppObjectBase)))
        {
            il.Emit(OpCodes.Call, ObjectBaseToPtrMethodInfo);
        }
    }

    private static void EmitConvertArgumentToManaged(ILGenerator il,
        int argIndex,
        Type managedParamType,
        out LocalBuilder variable)
    {
        variable = null;

        if (managedParamType.IsSubclassOf(typeof(ValueType)))
        {
            // Box struct into object first before conversion
            il.Emit(OpCodes.Ldc_I8, Il2CppClassPointerStore.GetNativeClassPointer(managedParamType).ToInt64());
            il.Emit(OpCodes.Conv_I);
            // On x64, struct is always a pointer but it is a non-pointer on x86
            // We don't handle byref structs on x86 yet but we're yet to encounter those
            il.Emit(Environment.Is64BitProcess ? OpCodes.Ldarg : OpCodes.Ldarga_S, argIndex);
            il.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_value_box)));
        }
        else
        {
            il.Emit(OpCodes.Ldarg_S, argIndex);
        }

        if (managedParamType.IsValueType) // don't need to convert blittable types
        {
            return;
        }

        void EmitCreateIl2CppObject(Type originalType)
        {
            var endLabel = il.DefineLabel();
            var notNullLabel = il.DefineLabel();

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue_S, notNullLabel);

            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Br_S, endLabel);

            il.MarkLabel(notNullLabel);
            il.Emit(OpCodes.Call, typeof(Il2CppObjectPool).GetMethod(nameof(Il2CppObjectPool.Get))!.MakeGenericMethod(originalType));

            il.MarkLabel(endLabel);
        }

        void HandleTypeConversion(Type originalType)
        {
            if (originalType == typeof(string))
            {
                il.Emit(OpCodes.Call, IL2CPPToManagedStringMethodInfo);
            }
            else if (originalType.IsSubclassOf(typeof(Il2CppObjectBase)))
            {
                EmitCreateIl2CppObject(originalType);
            }
        }

        if (managedParamType.IsByRef)
        {
            // TODO: directType being ValueType is not handled yet (but it's not that common in games). Implement when needed.
            var directType = managedParamType.GetElementType();

            variable = il.DeclareLocal(directType);

            il.Emit(OpCodes.Ldind_I);

            HandleTypeConversion(directType);

            il.Emit(OpCodes.Stloc, variable);
            il.Emit(OpCodes.Ldloca, variable);
        }
        else
        {
            HandleTypeConversion(managedParamType);
        }
    }
}
