using AnimeShelf.Core.Models;
using AnimeShelf.Core.Services;
using Xunit;
namespace AnimeShelf.Tests;
public class CatalogTests {
 [Fact] public async Task ScanPreservesWatchedAndManualAssociation() {
  var dir=Path.Combine(Path.GetTempPath(),"AnimeShelfTest-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
  try {
   var store=new CatalogStore(Path.Combine(dir,"db.sqlite")); await store.InitializeAsync();
   var folder=new ScannedFolder(Path.Combine(dir,"anime"),"Example",2023,new[]{new EpisodeFile(Path.Combine(dir,"anime","Example - 01.mkv"),1)});
   await store.SaveScanAsync(new[]{folder}); var entry=(await store.ListAsync()).Single();
   await store.MarkWatchedAsync(entry.Id,folder.Episodes[0].Path); await store.SetStatusAsync(entry.Id,"Assistindo");
   AnimeCandidate C(int id)=>new(id,"Example",2023,12,"TV",null,null,null,null,null,null,null,Array.Empty<string>(),Array.Empty<string>());
   await store.AssociateAsync(entry.Id,C(1),true,null); await store.AssociateAsync(entry.Id,C(2),false,null); await store.SaveScanAsync(new[]{folder});
   entry=(await store.ListAsync()).Single(); Assert.Equal(1,entry.AniListId); Assert.True(entry.Manual); Assert.Equal(1,entry.WatchedCount); Assert.Equal("Assistindo",entry.UserStatus); Assert.True((await store.EpisodesAsync(entry.Id)).Single().Watched);
   await store.MarkWatchedAsync(entry.Id,folder.Episodes[0].Path,false); Assert.Equal(0,(await store.ListAsync()).Single().WatchedCount);
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(dir,true); }
 }
 [Fact] public async Task ReconcileOnlyMarksItemsMissingForCompletedRoots() {
  var dir=Path.Combine(Path.GetTempPath(),"AnimeShelfTest-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
  try { var store=new CatalogStore(Path.Combine(dir,"db.sqlite")); await store.InitializeAsync(); var root=Path.Combine(dir,"library"); var path=Path.Combine(root,"anime"); var episode=Path.Combine(path,"01.mkv"); var folder=new ScannedFolder(path,"Anime",null,new[]{new EpisodeFile(episode,1)}); await store.SaveScanAsync(new[]{folder});
   await store.ReconcileAsync(new ScanReport(Array.Empty<ScannedFolder>(),Array.Empty<string>(),new[]{"erro"})); Assert.False((await store.ListAsync()).Single().Missing);
   await store.ReconcileAsync(new ScanReport(Array.Empty<ScannedFolder>(),new[]{root},Array.Empty<string>())); Assert.True((await store.ListAsync()).Single().Missing); Assert.Empty(await store.EpisodesAsync((await store.ListAsync()).Single().Id));
   await store.SaveScanAsync(new[]{folder}); await store.ReconcileAsync(new ScanReport(new[]{folder},new[]{root},Array.Empty<string>())); Assert.False((await store.ListAsync()).Single().Missing); Assert.Single(await store.EpisodesAsync((await store.ListAsync()).Single().Id));
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(dir,true); }
 } [Fact] public async Task TranslationCacheDistinguishesLanguageAndSource() {
  var dir=Path.Combine(Path.GetTempPath(),"AnimeShelfTest-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
  try { var store=new CatalogStore(Path.Combine(dir,"db.sqlite")); await store.InitializeAsync(); await store.SaveTranslationAsync(10,"pt","hash1","Sinopse"); Assert.Equal("Sinopse",await store.GetTranslationAsync(10,"pt","hash1")); Assert.Null(await store.GetTranslationAsync(10,"es","hash1")); Assert.Null(await store.GetTranslationAsync(10,"pt","hash2")); }
  finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(dir,true); }
 } [Fact] public async Task CustomFieldsSurviveAssociationAndRescanAndCanBeReset() {
  var dir=Path.Combine(Path.GetTempPath(),"AnimeShelfTest-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
  try {
   var store=new CatalogStore(Path.Combine(dir,"db.sqlite")); await store.InitializeAsync();
   var folder=new ScannedFolder(Path.Combine(dir,"anime"),"Original",null,new[]{new EpisodeFile(Path.Combine(dir,"anime","01.mkv"),1)});
   await store.SaveScanAsync(new[]{folder}); var entry=(await store.ListAsync()).Single();
   await store.SaveCustomAsync(entry.Id,"Meu título","Minha sinopse","custom-0123456789abcdef0123456789abcdef.jpg");
   var candidate=new AnimeCandidate(1,"AniList",2020,12,"TV","Original",null,null,null,"FINISHED",null,null,Array.Empty<string>(),Array.Empty<string>());
   await store.AssociateAsync(entry.Id,candidate,true,null); await store.SaveScanAsync(new[]{folder}); await store.InitializeAsync();
   entry=(await store.ListAsync()).Single(); Assert.Equal("Meu título",entry.CustomTitle); Assert.Equal("Minha sinopse",entry.CustomDescription); Assert.Equal("custom-0123456789abcdef0123456789abcdef.jpg",entry.CustomCoverPath); Assert.Equal("AniList",entry.Romaji);
   await store.SaveCustomAsync(entry.Id,null,null,null); entry=(await store.ListAsync()).Single(); Assert.Null(entry.CustomTitle); Assert.Null(entry.CustomDescription); Assert.Null(entry.CustomCoverPath); Assert.Equal("AniList",entry.Romaji);
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(dir,true); }
 } [Fact] public async Task UpgradesAnExistingDatabaseWithoutLosingEntries() {
  var dir=Path.Combine(Path.GetTempPath(),"AnimeShelfTest-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir); var path=Path.Combine(dir,"db.sqlite");
  try {
   await using(var connection=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}")) { await connection.OpenAsync(); await using var cmd=connection.CreateCommand(); cmd.CommandText="CREATE TABLE anime (id INTEGER PRIMARY KEY, folder TEXT NOT NULL UNIQUE COLLATE NOCASE, search_title TEXT NOT NULL, missing INTEGER NOT NULL DEFAULT 0, ignored INTEGER NOT NULL DEFAULT 0, manual INTEGER NOT NULL DEFAULT 0, anilist_id INTEGER, romaji TEXT, metadata_json TEXT, cover_path TEXT, cover_url TEXT, added_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP); INSERT INTO anime(id,folder,search_title,romaji) VALUES(5,'C:\\old','Old','Original');"; await cmd.ExecuteNonQueryAsync(); }
   var store=new CatalogStore(path); await store.InitializeAsync(); var item=(await store.ListAsync()).Single(); Assert.Equal(5,item.Id); Assert.Equal("Original",item.Romaji); Assert.Null(item.CustomTitle); await store.SaveCustomAsync(5,"Novo",null,null); Assert.Equal("Novo",(await store.ListAsync()).Single().CustomTitle);
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(dir,true); }
 }}




