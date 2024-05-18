using Xunit.Abstractions;

namespace Il2CppInterop.Tests.Generic;

public sealed class StringConversionsTest : BaseIl2CppUnitTest
{
    public StringConversionsTest(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void Roundtrip()
    {
        var text = "test";
        Assert.Equal(text, (string)(Il2Cpp.System.String)text);
    }
}
