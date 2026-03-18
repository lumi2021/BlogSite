namespace BlogSite.Exceptions;

public class TooMuchDomException(string[] query, string directory, string[] found)
    : Exception($"More than one files matching query [{string.Join(", ", query.Select(e => $"'{e}'"))}] in '{directory}'")
{
}
