namespace Build.Core.Doc;

static class StringExtensions
{
    public static string GetValidRelease(string release) =>
        new(release.Replace(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());

    private static IEnumerable<char> Replace(this IEnumerable<char> str, Func<char, char?> replacer) =>
        str.Select(replacer).Where(c => c.HasValue).Select(c => c!.Value);
}