namespace Pure.DI.Tests;

using Core;

public class NameFormatterTests
{
    [Theory]
    [InlineData("Abc", "Abc")]
    [InlineData("Xyz.Abc", "XyzAbc")]
    [InlineData("", "")]
    [InlineData(".", "_")]
    [InlineData("Xyz.Abc<T>", "XyzAbc_T")]
    [InlineData("Xyz.Abc`[T]", "XyzAbc_T")]
    [InlineData("Xyz.Abc`[T,T1]", "XyzAbc_T_T1")]
    [InlineData("Xyz.Abc`[T, T1]", "XyzAbc_T_T1")]
    [InlineData("global::Xyz.Abc`[T, T1]", "globalXyzAbc_T_T1")]
    public void ShouldConvertToValidIdentifier(string text, string expectedResult)
    {
        // Given

        // When
        var actualResult = NameFormatter.ToValidIdentifier(text);

        // Then
        actualResult.ShouldBe(expectedResult);
    }
}