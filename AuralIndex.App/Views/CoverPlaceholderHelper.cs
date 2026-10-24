namespace AuralIndex_App.Views;

internal static class CoverPlaceholderHelper
{
    public static string GetTitleInitials(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "AB";
        }

        var parts = title
            .Split([' ', '-', '"', '\'', '«', '»', ':', '.', ',', '(', ')'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .ToArray();

        if (parts.Length >= 2)
        {
            return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
        }

        var first = parts.Length == 1 ? parts[0] : title.Trim();
        return new string(first.Take(2).Select(char.ToUpperInvariant).ToArray());
    }

    public static string GetGenreMark(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return "AU";
        }

        var parts = genre
            .Split([' ', '-', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .ToArray();

        if (parts.Length >= 2)
        {
            return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
        }

        return new string(parts[0].Take(2).Select(char.ToUpperInvariant).ToArray());
    }
}
