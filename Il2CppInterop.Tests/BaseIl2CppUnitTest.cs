using Il2CppInterop.Bindings.Structs;
using Xunit.Abstractions;

namespace Il2CppInterop.Tests;

[Collection("IL2CPP")]
public abstract class BaseIl2CppUnitTest : IAssemblyFixture<Il2CppRuntimeInitializer>
{
    protected unsafe BaseIl2CppUnitTest(ITestOutputHelper outputHelper)
    {
        Il2CppThread.Attach();
        Il2CppRuntimeInitializer.LogEvent = outputHelper.WriteLine;
    }
}
