using System.IO;
using System.IO.Compression;
using System.Text.Json;
using AnimeShelf.Core.Services;
using Microsoft.Data.Sqlite;
namespace AnimeShelf;
public static class BackupService {
 public static async Task ExportAsync(CatalogStore store,string target,CancellationToken ct=default) {
  var temp=Path.Combine(Path.GetTempPath(),"AnimeShelf-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
  try {
   var db=Path.Combine(Settings.DataDirectory,"catalog.db"); var snapshot=Path.Combine(temp,"catalog.db");
   await using(var source=new SqliteConnection(new SqliteConnectionStringBuilder { DataSource=db,Mode=SqliteOpenMode.ReadOnly }.ToString()))
   await using(var dest=new SqliteConnection(new SqliteConnectionStringBuilder { DataSource=snapshot }.ToString())) { await source.OpenAsync(ct); await dest.OpenAsync(ct); source.BackupDatabase(dest); }
   await File.WriteAllTextAsync(Path.Combine(temp,"biblioteca.json"),JsonSerializer.Serialize(await store.ListAsync(ct),new JsonSerializerOptions { WriteIndented=true }),ct);
   var covers=Path.Combine(Settings.DataDirectory,"covers"); if(Directory.Exists(covers)) { var copy=Path.Combine(temp,"covers"); Directory.CreateDirectory(copy); foreach(var file in Directory.EnumerateFiles(covers,"*.jpg")) { ct.ThrowIfCancellationRequested(); File.Copy(file,Path.Combine(copy,Path.GetFileName(file))); } }
   // Write to a separate file so a failed export cannot truncate an existing backup.
   var partial=target+"."+Guid.NewGuid().ToString("N")+".tmp"; try { ZipFile.CreateFromDirectory(temp,partial); File.Move(partial,target,true); } finally { if(File.Exists(partial)) File.Delete(partial); }
  } finally { Directory.Delete(temp,true); }
 }
 public static async Task ImportAsync(string archive,CancellationToken ct=default) {
  var temp=Path.Combine(Path.GetTempPath(),"AnimeShelf-import-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
  try {
   using(var zip=ZipFile.OpenRead(archive)) {
    var database=zip.GetEntry("catalog.db") ?? throw new InvalidDataException(Localization.Get("MissingDb"));
    if(database.Length>512L*1024*1024) throw new InvalidDataException(Localization.Get("DbTooLarge"));
    database.ExtractToFile(Path.Combine(temp,"catalog.db"));
    var coverDir=Path.Combine(temp,"covers"); Directory.CreateDirectory(coverDir); long total=0;
    foreach(var item in zip.Entries.Where(e=>e.FullName.StartsWith("covers/",StringComparison.OrdinalIgnoreCase))) {
     ct.ThrowIfCancellationRequested(); var name=Path.GetFileName(item.FullName); if(!System.Text.RegularExpressions.Regex.IsMatch(name,@"^(?:\d+|custom-[0-9a-f]{32})\.jpg$",System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
     total+=item.Length; if(item.Length>20*1024*1024 || total>1024L*1024*1024) throw new InvalidDataException(Localization.Get("BackupLarge")); item.ExtractToFile(Path.Combine(coverDir,name),true);
    }
   }
   var sourcePath=Path.Combine(temp,"catalog.db");
   await using var source=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=sourcePath,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString()); await source.OpenAsync(ct);
   await using(var check=source.CreateCommand()) { check.CommandText="PRAGMA integrity_check"; if((string?)await check.ExecuteScalarAsync(ct)!="ok") throw new InvalidDataException(Localization.Get("DbCorrupt")); }
   await using(var check=source.CreateCommand()) { check.CommandText="SELECT count(*) FROM sqlite_master WHERE type='table' AND name IN ('anime','episodes','progress','translations')"; if(Convert.ToInt32(await check.ExecuteScalarAsync(ct))!=4) throw new InvalidDataException(Localization.Get("DbIncompatible")); }
   var covers=Path.Combine(Settings.DataDirectory,"covers"); Directory.CreateDirectory(covers); foreach(var file in Directory.EnumerateFiles(Path.Combine(temp,"covers"))) File.Copy(file,Path.Combine(covers,Path.GetFileName(file)),true);
   SqliteConnection.ClearAllPools(); await using var dest=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=Path.Combine(Settings.DataDirectory,"catalog.db"),Pooling=false}.ToString()); await dest.OpenAsync(ct); source.BackupDatabase(dest);
  } finally { Directory.Delete(temp,true); }
 }}


