using Il2Cpp.TestGame;
using Il2CppInterop.Bindings;
using Xunit.Abstractions;

namespace Il2CppInterop.Tests.TestGame;

public sealed class InvokeUnitTest : BaseIl2CppUnitTest
{
    public InvokeUnitTest(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void Nothing() => InvokeTest.Nothing();

    [Fact]
    public void Sum() => Assert.Equal(3, InvokeTest.Sum(1, 2));

    [Fact]
    public void Throw() => Assert.Throws<WrappedIl2CppException>(InvokeTest.Throw);
}
