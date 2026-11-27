using System.Text.RegularExpressions;

namespace AnimeShelf.Core.Services;

public static partial class NameParser
{
    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*(?:19|20)\d{2}[^)]*\)", RegexOptions.IgnoreCase)] private static partial Regex Tags();
    [GeneratedRegex(@"\b(?:19|20)\d{2}\b")] private static partial Regex YearPattern();
    [GeneratedRegex(@"\b(?:\d{3,4}p|[xh]\.?26[45]|hevc|av1|10bit|8bit|bluray|blu-ray|bd|dual[ ._-]*audio|web[ ._-]*dl|aac|flac|mkv|mp4|avi|webm)\b", RegexOptions.IgnoreCase)] private static partial Regex Technical();
    [GeneratedRegex(@"\b[0-9a-f]{8}\b", RegexOptions.IgnoreCase)] private static partial Regex Hash();
    [GeneratedRegex(@"(?i)(?:\bS\d{1,2}E|\bEP(?:ISODE)?[ ._-]*|\s-\s*)(\d{1,4})(?:v\d+)?\b|\b(\d{1,4})v\d+\b")] private static partial Regex EpisodePattern();
    public static int? FindYear(string name) => int.TryParse(YearPattern().Match(name).Value, out var y) ? y : null;
    public static int? FindEpisode(string name) { var m = EpisodePattern().Match(System.IO.Path.GetFileNameWithoutExtension(name)); return m.Success && int.TryParse(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value, out var n) ? n : null; }
    public static string Clean(string name)
    {
        name = Tags().Replace(name, " ");
        name = YearPattern().Replace(name, " ");
        name = Technical().Replace(name, " ");
        name = Hash().Replace(name, " ");
        name = EpisodePattern().Replace(name, " ");
        return Regex.Replace(name.Replace('_', ' ').Replace('.', ' '), @"\s+", " ").Trim(' ', '-', '[', ']', '(', ')');
    }
}
