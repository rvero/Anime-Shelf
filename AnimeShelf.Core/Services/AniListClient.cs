using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnimeShelf.Core.Models;

namespace AnimeShelf.Core.Services;

public sealed class AniListClient : IDisposable
{
    public const string Query = """
        query ($search: String, $id: Int) {
          Page(page: 1, perPage: 10) { media(search: $search, id: $id, type: ANIME) {
            id title { romaji } description(asHtml: false) coverImage { extraLarge large }
            startDate { year month day } endDate { year month day } episodes status averageScore popularity format genres studios { nodes { name isAnimationStudio } }
          } }
        }
        """;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTimeOffset next = DateTimeOffset.MinValue;
    public async Task<IReadOnlyList<AnimeCandidate>> SearchAsync(string? title = null, int? id = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title) && id is null) throw new ArgumentException("Informe título ou ID.");
        await gate.WaitAsync(ct);
        try {
            for (int attempt = 0; attempt < 5; attempt++) {
                var delay = next - DateTimeOffset.UtcNow; if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
                next = DateTimeOffset.UtcNow.AddSeconds(1); // 60/min, below AniList's stated 90/min.
                var query = id.HasValue ? Query.Replace("query ($search: String, $id: Int)", "query ($id: Int)").Replace("media(search: $search, id: $id, type: ANIME)", "media(id: $id, type: ANIME)") : Query.Replace("query ($search: String, $id: Int)", "query ($search: String)").Replace("media(search: $search, id: $id, type: ANIME)", "media(search: $search, type: ANIME)");
                using var response = await http.PostAsJsonAsync("https://graphql.anilist.co", new { query, variables = new { search = title, id } }, ct);
                if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500) { var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)); await Task.Delay(wait, ct); continue; }
                response.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
                if (doc.RootElement.TryGetProperty("errors", out var errors)) throw new InvalidOperationException(errors.ToString());
                return doc.RootElement.GetProperty("data").GetProperty("Page").GetProperty("media").EnumerateArray().Select(Parse).ToArray();
            }
            throw new HttpRequestException("AniList indisponível após tentativas repetidas.");
        } finally { gate.Release(); }
    }
    private static AnimeCandidate Parse(JsonElement x) {
        static string? S(JsonElement e, string key) => e.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        static int? N(JsonElement e, string key) => e.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out int n) ? n : null;
        static string? Date(JsonElement e) { var y=N(e,"year"); var m=N(e,"month"); var d=N(e,"day"); return y is null ? null : m is null ? $"{y:0000}" : d is null ? $"{y:0000}-{m:00}" : $"{y:0000}-{m:00}-{d:00}"; }
        var start=x.GetProperty("startDate"); var end=x.GetProperty("endDate"); var cover=x.GetProperty("coverImage");
        var description=S(x,"description"); if (description is not null) description=WebUtility.HtmlDecode(Regex.Replace(description, "<[^>]*>", " "));
        return new(x.GetProperty("id").GetInt32(), S(x.GetProperty("title"),"romaji") ?? "Sem título romaji", N(start,"year"), N(x,"episodes"), S(x,"format"), description, S(cover,"extraLarge") ?? S(cover,"large"), Date(start), Date(end), S(x,"status"), N(x,"averageScore"), N(x,"popularity"), x.GetProperty("genres").EnumerateArray().Select(g=>g.GetString() ?? "").ToArray(), x.GetProperty("studios").GetProperty("nodes").EnumerateArray().Where(s => s.TryGetProperty("isAnimationStudio", out var flag) && flag.ValueKind == JsonValueKind.True).Select(s=>S(s,"name") ?? "").ToArray());
    }
    public void Dispose() { http.Dispose(); gate.Dispose(); }
}



