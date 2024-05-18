using Il2Cpp.TestGame;
using Xunit.Abstractions;

namespace Il2CppInterop.Tests.TestGame;

public sealed class RoundtripUnitTest : BaseIl2CppUnitTest
{
    public RoundtripUnitTest(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public void Object()
    {
        var value = new Il2Cpp.System.Object();
        Assert.Equal(value, RoundtripTest.Object(value));
    }

    // [Fact]
    // public void ObjectByRef()
    // {
    //     var value = new Il2Cpp.System.Object();
    //     Assert.Equal(value, RoundtripTest.ObjectByRef(ref value));
    // }

    [Theory]
    [InlineData(123)]
    public void Int(int value) => Assert.Equal(value, RoundtripTest.Int(value));

    [Theory]
    [InlineData(123)]
    public void IntByRef(int value) => Assert.Equal(value, RoundtripTest.IntByRef(ref value));

    [Theory]
    [InlineData("test")]
    public void String(string value) => Assert.Equal(value, RoundtripTest.String(value));

    [Theory]
    [InlineData(NormalEnum.A), InlineData(NormalEnum.B), InlineData(NormalEnum.C)]
    public void Enum(NormalEnum value) => Assert.Equal(value, RoundtripTest.Enum(value));

    [Fact]
    public void NormalStruct()
    {
        var value = new NormalStruct
        {
            A = 123,
            B = true,
            C = 456,
        };
        Assert.Equal(value, RoundtripTest.NormalStruct(value));
    }


    [Fact]
    public unsafe void Pointer()
    {
        var number = 123;
        var pointer = &number;
        Assert.Equal((nint)pointer, (nint)RoundtripTest.Pointer(pointer));
    }

    // [Fact]
    // public unsafe void Type()
    // {
    //     Assert.Equal(
    //         (IntPtr)Il2CppReflectionType.From(Il2CppClassPointerStore<Il2Cpp.System.Object>.Pointer),
    //         (IntPtr)RoundtripTest.Type<Il2Cpp.System.Object>().Pointer
    //     );
    // }
}
