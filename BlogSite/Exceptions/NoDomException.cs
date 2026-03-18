namespace BlogSite.Exceptions;

public class NoDomException(string[] query, string directory)
    : Exception($"Expected file matching [{string.Join(", ", query.Select(e => $"'{e}'"))}] inside '{directory}' but no file found")
{
}
