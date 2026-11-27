namespace AnimeShelf.Core.Models;

public sealed record EpisodeFile(string Path, int? Number, bool Watched = false);
public sealed record ScannedFolder(string Path, string SearchTitle, int? Year, IReadOnlyList<EpisodeFile> Episodes);
public sealed record AnimeCandidate(int Id, string Romaji, int? Year, int? Episodes, string? Format,
    string? Description, string? CoverUrl, string? StartDate, string? EndDate, string? Status,
    int? AverageScore, int? Popularity, IReadOnlyList<string> Genres, IReadOnlyList<string> Studios);
public sealed record LibraryEntry(long Id, string Folder, string SearchTitle, bool Missing, bool Ignored,
    bool Manual, int? AniListId, string? Romaji, string? CoverPath, string? CoverUrl, AnimeCandidate? Metadata = null, int WatchedCount = 0, int LocalCount = 0, string UserStatus = "Planejo assistir", string? CustomTitle = null, string? CustomDescription = null, string? CustomCoverPath = null);





