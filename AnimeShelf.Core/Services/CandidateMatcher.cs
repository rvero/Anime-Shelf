using System.Text;
using AnimeShelf.Core.Models;

namespace AnimeShelf.Core.Services;

public static class CandidateMatcher
{
    private static string Normalize(string s) => new(s.ToLowerInvariant().Normalize(NormalizationForm.FormD).Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
    public static double Score(string title, int? year, int localEpisodes, AnimeCandidate candidate)
    {
        var a = Normalize(title); var b = Normalize(candidate.Romaji);
        if (a.Length == 0 || b.Length == 0) return 0;
        var row = Enumerable.Range(0, b.Length + 1).ToArray();
        for (int i = 1; i <= a.Length; i++) { int previous = row[0]; row[0] = i; for (int j = 1; j <= b.Length; j++) { int old = row[j]; row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1), previous + (a[i - 1] == b[j - 1] ? 0 : 1)); previous = old; } }
        double score = 1d - (double)row[b.Length] / Math.Max(a.Length, b.Length);
        if (year.HasValue && candidate.Year.HasValue) score += year == candidate.Year ? .1 : -.1;
        if (localEpisodes > 0 && candidate.Episodes.HasValue && localEpisodes > candidate.Episodes) score -= .1;
        return score;
    }
    public static AnimeCandidate? Best(string title, int? year, int localEpisodes, IEnumerable<AnimeCandidate> candidates, double threshold = .55)
        => candidates.Select(c => (Candidate: c, Score: Score(title, year, localEpisodes, c))).Where(x => x.Score >= threshold).OrderByDescending(x => x.Score).Select(x => x.Candidate).FirstOrDefault();
}
