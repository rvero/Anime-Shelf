using System.Text.Json;
using AnimeShelf.Core.Models;
using Microsoft.Data.Sqlite;

namespace AnimeShelf.Core.Services;

public sealed class CatalogStore
{
    private readonly string connectionString;
    public CatalogStore(string databasePath) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!); connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString(); }
    private async Task<SqliteConnection> Open(CancellationToken ct) { var db = new SqliteConnection(connectionString); await db.OpenAsync(ct); return db; }
    public async Task InitializeAsync(CancellationToken ct = default) {
        await using var db = await Open(ct); await using var cmd = db.CreateCommand(); cmd.CommandText = """
        PRAGMA journal_mode=WAL;
        CREATE TABLE IF NOT EXISTS anime (id INTEGER PRIMARY KEY, folder TEXT NOT NULL UNIQUE COLLATE NOCASE, search_title TEXT NOT NULL, missing INTEGER NOT NULL DEFAULT 0, ignored INTEGER NOT NULL DEFAULT 0, manual INTEGER NOT NULL DEFAULT 0, anilist_id INTEGER, romaji TEXT, metadata_json TEXT, cover_path TEXT, cover_url TEXT, added_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
        CREATE TABLE IF NOT EXISTS episodes (id INTEGER PRIMARY KEY, anime_id INTEGER NOT NULL REFERENCES anime(id) ON DELETE CASCADE, path TEXT NOT NULL, number INTEGER, watched INTEGER NOT NULL DEFAULT 0, watched_at TEXT, missing INTEGER NOT NULL DEFAULT 0, UNIQUE(anime_id,path));
        CREATE TABLE IF NOT EXISTS progress (anime_id INTEGER PRIMARY KEY REFERENCES anime(id) ON DELETE CASCADE, status TEXT, personal_score REAL, last_episode INTEGER, updated_at TEXT);
        CREATE TABLE IF NOT EXISTS translations (anilist_id INTEGER NOT NULL, language TEXT NOT NULL, source_hash TEXT NOT NULL, text TEXT NOT NULL, PRIMARY KEY(anilist_id,language,source_hash));
        """; await cmd.ExecuteNonQueryAsync(ct);
        // Additive migration keeps existing associations and watched progress intact.
        var columns=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using(var info=db.CreateCommand()) { info.CommandText="PRAGMA table_info(anime)"; await using var reader=await info.ExecuteReaderAsync(ct); while(await reader.ReadAsync(ct)) columns.Add(reader.GetString(1)); }
        foreach(var column in new[]{"custom_title","custom_description","custom_cover_path"}) if(!columns.Contains(column)) { await using var alter=db.CreateCommand(); alter.CommandText=$"ALTER TABLE anime ADD COLUMN {column} TEXT"; await alter.ExecuteNonQueryAsync(ct); }
    }
    // Upsert discovered files without deleting watched state. Missing flags are deliberately not inferred
    // from partial scans: inaccessible roots must never erase a user's library.
    public async Task SaveScanAsync(IEnumerable<ScannedFolder> folders, CancellationToken ct = default) {
        await using var db=await Open(ct); await using var tx=await db.BeginTransactionAsync(ct);
        foreach(var f in folders) {
            await using var cmd=db.CreateCommand(); cmd.Transaction=(SqliteTransaction)tx; cmd.CommandText="INSERT INTO anime(folder,search_title,missing) VALUES($p,$t,0) ON CONFLICT(folder) DO UPDATE SET search_title=excluded.search_title,missing=0 RETURNING id"; cmd.Parameters.AddWithValue("$p",f.Path); cmd.Parameters.AddWithValue("$t",f.SearchTitle); var id=Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
            foreach(var e in f.Episodes) { await using var ec=db.CreateCommand(); ec.Transaction=(SqliteTransaction)tx; ec.CommandText="INSERT INTO episodes(anime_id,path,number,missing) VALUES($id,$p,$n,0) ON CONFLICT(anime_id,path) DO UPDATE SET number=excluded.number,missing=0"; ec.Parameters.AddWithValue("$id",id); ec.Parameters.AddWithValue("$p",e.Path); ec.Parameters.AddWithValue("$n",(object?)e.Number ?? DBNull.Value); await ec.ExecuteNonQueryAsync(ct); }
        }
        await tx.CommitAsync(ct);
    }
    // Mark absent items only under roots that were enumerated without errors.
    public async Task ReconcileAsync(ScanReport report, CancellationToken ct = default) {
        var scanned=report.Folders.ToDictionary(f=>f.Path,StringComparer.OrdinalIgnoreCase);
        await using var db=await Open(ct); await using var tx=await db.BeginTransactionAsync(ct);
        var entries=new List<(long Id,string Folder)>(); await using(var cmd=db.CreateCommand()) { cmd.Transaction=(SqliteTransaction)tx; cmd.CommandText="SELECT id,folder FROM anime"; await using var reader=await cmd.ExecuteReaderAsync(ct); while(await reader.ReadAsync(ct)) entries.Add((reader.GetInt64(0),reader.GetString(1))); }
        foreach(var entry in entries) {
            bool covered=report.CompleteRoots.Any(root=>entry.Folder.Equals(root,StringComparison.OrdinalIgnoreCase) || entry.Folder.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));
            if(!covered) continue;
            await using(var cmd=db.CreateCommand()) { cmd.Transaction=(SqliteTransaction)tx; cmd.CommandText="UPDATE anime SET missing=$missing WHERE id=$id"; cmd.Parameters.AddWithValue("$id",entry.Id); cmd.Parameters.AddWithValue("$missing",scanned.ContainsKey(entry.Folder)?0:1); await cmd.ExecuteNonQueryAsync(ct); }
            var paths=scanned.TryGetValue(entry.Folder,out var folder) ? folder.Episodes.Select(e=>e.Path).ToHashSet(StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existing=new List<string>(); await using(var cmd=db.CreateCommand()) { cmd.Transaction=(SqliteTransaction)tx; cmd.CommandText="SELECT path FROM episodes WHERE anime_id=$id"; cmd.Parameters.AddWithValue("$id",entry.Id); await using var reader=await cmd.ExecuteReaderAsync(ct); while(await reader.ReadAsync(ct)) existing.Add(reader.GetString(0)); }
            foreach(var path in existing) { await using var cmd=db.CreateCommand(); cmd.Transaction=(SqliteTransaction)tx; cmd.CommandText="UPDATE episodes SET missing=$missing WHERE anime_id=$id AND path=$path"; cmd.Parameters.AddWithValue("$id",entry.Id); cmd.Parameters.AddWithValue("$path",path); cmd.Parameters.AddWithValue("$missing",paths.Contains(path)?0:1); await cmd.ExecuteNonQueryAsync(ct); }
        }
        await tx.CommitAsync(ct);
    }    public async Task AssociateAsync(long libraryId, AnimeCandidate candidate, bool manual, string? coverPath, CancellationToken ct = default) {
        await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="UPDATE anime SET anilist_id=$id,romaji=$title,metadata_json=$json,cover_path=$cover,cover_url=$url,manual=$manual,ignored=0 WHERE id=$library AND ($manual=1 OR manual=0)";
        cmd.Parameters.AddWithValue("$id",candidate.Id); cmd.Parameters.AddWithValue("$title",candidate.Romaji); cmd.Parameters.AddWithValue("$json",JsonSerializer.Serialize(candidate)); cmd.Parameters.AddWithValue("$cover",(object?)coverPath ?? DBNull.Value); cmd.Parameters.AddWithValue("$url",(object?)candidate.CoverUrl ?? DBNull.Value); cmd.Parameters.AddWithValue("$manual",manual ? 1 : 0); cmd.Parameters.AddWithValue("$library",libraryId); await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken ct = default) {
        await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="SELECT id,folder,search_title,missing,ignored,manual,anilist_id,romaji,cover_path,cover_url,metadata_json,(SELECT COUNT(*) FROM episodes e WHERE e.anime_id=anime.id AND e.watched=1 AND e.missing=0),(SELECT COUNT(*) FROM episodes e WHERE e.anime_id=anime.id AND e.missing=0),COALESCE((SELECT status FROM progress p WHERE p.anime_id=anime.id),'Planejo assistir'),custom_title,custom_description,custom_cover_path FROM anime ORDER BY COALESCE(custom_title,romaji,search_title)"; await using var r=await cmd.ExecuteReaderAsync(ct); var list=new List<LibraryEntry>();
        while(await r.ReadAsync(ct)) list.Add(new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetBoolean(3),r.GetBoolean(4),r.GetBoolean(5),r.IsDBNull(6)?null:r.GetInt32(6),r.IsDBNull(7)?null:r.GetString(7),r.IsDBNull(8)?null:r.GetString(8),r.IsDBNull(9)?null:r.GetString(9),r.IsDBNull(10)?null:JsonSerializer.Deserialize<AnimeCandidate>(r.GetString(10)),r.GetInt32(11),r.GetInt32(12),r.GetString(13),r.IsDBNull(14)?null:r.GetString(14),r.IsDBNull(15)?null:r.GetString(15),r.IsDBNull(16)?null:r.GetString(16))); return list;
    }
    public async Task<IReadOnlyList<EpisodeFile>> EpisodesAsync(long id, CancellationToken ct = default) {
        await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="SELECT path,number,watched FROM episodes WHERE anime_id=$id AND missing=0 ORDER BY number IS NULL,number,path"; cmd.Parameters.AddWithValue("$id",id); await using var r=await cmd.ExecuteReaderAsync(ct); var list=new List<EpisodeFile>(); while(await r.ReadAsync(ct)) list.Add(new(r.GetString(0),r.IsDBNull(1)?null:r.GetInt32(1),r.GetBoolean(2))); return list;
    }
    public async Task MarkWatchedAsync(long id, string path, bool watched = true, CancellationToken ct = default) {
        await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="UPDATE episodes SET watched=$watched,watched_at=CASE WHEN $watched=1 THEN CURRENT_TIMESTAMP ELSE NULL END WHERE anime_id=$id AND path=$path"; cmd.Parameters.AddWithValue("$id",id); cmd.Parameters.AddWithValue("$path",path); cmd.Parameters.AddWithValue("$watched",watched ? 1 : 0); await cmd.ExecuteNonQueryAsync(ct);
    }    public async Task SetIgnoredAsync(long id, bool ignored, CancellationToken ct = default) {
        await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="UPDATE anime SET ignored=$ignored WHERE id=$id"; cmd.Parameters.AddWithValue("$ignored",ignored?1:0); cmd.Parameters.AddWithValue("$id",id); await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task UnassociateAsync(long id, CancellationToken ct = default) {
        await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="UPDATE anime SET anilist_id=NULL,romaji=NULL,metadata_json=NULL,cover_path=NULL,cover_url=NULL,manual=1 WHERE id=$id"; cmd.Parameters.AddWithValue("$id",id); await cmd.ExecuteNonQueryAsync(ct);
    }    public async Task SetStatusAsync(long id, string status, CancellationToken ct = default) {
        await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="INSERT INTO progress(anime_id,status,updated_at) VALUES($id,$status,CURRENT_TIMESTAMP) ON CONFLICT(anime_id) DO UPDATE SET status=excluded.status,updated_at=CURRENT_TIMESTAMP"; cmd.Parameters.AddWithValue("$id",id); cmd.Parameters.AddWithValue("$status",status); await cmd.ExecuteNonQueryAsync(ct);
    }    public async Task<string?> GetTranslationAsync(int id,string language,string hash,CancellationToken ct=default) { await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="SELECT text FROM translations WHERE anilist_id=$id AND language=$lang AND source_hash=$hash"; cmd.Parameters.AddWithValue("$id",id); cmd.Parameters.AddWithValue("$lang",language); cmd.Parameters.AddWithValue("$hash",hash); return (string?)await cmd.ExecuteScalarAsync(ct); }
    public async Task SaveTranslationAsync(int id,string language,string hash,string text,CancellationToken ct=default) { await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="INSERT OR REPLACE INTO translations(anilist_id,language,source_hash,text) VALUES($id,$lang,$hash,$text)"; cmd.Parameters.AddWithValue("$id",id); cmd.Parameters.AddWithValue("$lang",language); cmd.Parameters.AddWithValue("$hash",hash); cmd.Parameters.AddWithValue("$text",text); await cmd.ExecuteNonQueryAsync(ct); }    public async Task<IReadOnlyList<string>> HistoryAsync(string episodeLabel="Episódio", CancellationToken ct=default) { await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="SELECT COALESCE(a.custom_title,a.romaji,a.search_title),e.number,e.path,e.watched_at FROM episodes e JOIN anime a ON a.id=e.anime_id WHERE e.watched=1 AND e.watched_at IS NOT NULL ORDER BY e.watched_at DESC LIMIT 500"; await using var reader=await cmd.ExecuteReaderAsync(ct); var result=new List<string>(); while(await reader.ReadAsync(ct)) result.Add($"{reader.GetString(3)}  ·  {reader.GetString(0)}  ·  {(reader.IsDBNull(1)?Path.GetFileName(reader.GetString(2)):$"{episodeLabel} {reader.GetInt32(1)}")}"); return result; }    public async Task SaveCustomAsync(long id,string? title,string? description,string? coverPath,CancellationToken ct=default) {
        await using var db=await Open(ct); await using var cmd=db.CreateCommand(); cmd.CommandText="UPDATE anime SET custom_title=$title,custom_description=$description,custom_cover_path=$cover WHERE id=$id"; cmd.Parameters.AddWithValue("$title",(object?)title ?? DBNull.Value); cmd.Parameters.AddWithValue("$description",(object?)description ?? DBNull.Value); cmd.Parameters.AddWithValue("$cover",(object?)coverPath ?? DBNull.Value); cmd.Parameters.AddWithValue("$id",id); if(await cmd.ExecuteNonQueryAsync(ct)!=1) throw new InvalidOperationException("Anime não encontrado.");
    }}












