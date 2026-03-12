namespace Pure.DI.IntegrationTests;

public class HugeCompositionTests
{
#if ROSLYN4_8_OR_GREATER
	[Fact]
	public async Task ShouldSupportHugeComposition()
	{
		// Given
		var source = GetEmbeddedResourceContent();

		// When
		var result = await source.RunAsync(new Options(LanguageVersion.Latest));

		// Then
		result.Success.ShouldBeTrue(result);
	}
#endif

	private static string GetEmbeddedResourceContent()
	{
		var assembly = typeof(HugeCompositionTests).Assembly;
		const string resourceName = "Pure.DI.IntegrationTests.HugeComposition.cs";

		using var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream == null)
		{
			throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
		}

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}