using AnimeShelf.Core.Models;
namespace AnimeShelf.Core.Services;
public sealed record ScanReport(IReadOnlyList<ScannedFolder> Folders, IReadOnlyList<string> CompleteRoots, IReadOnlyList<string> Errors);
public sealed class FolderScanner {
 private static readonly HashSet<string> Extensions=new(StringComparer.OrdinalIgnoreCase){".mkv",".mp4",".avi",".webm"};
 public Task<ScanReport> ScanAsync(IEnumerable<string> roots,IProgress<string>? progress=null,CancellationToken cancellationToken=default) => Task.Run(()=> {
  var result=new List<ScannedFolder>(); var complete=new List<string>(); var errors=new List<string>(); var found=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var root in roots.Distinct(StringComparer.OrdinalIgnoreCase)) {
   cancellationToken.ThrowIfCancellationRequested(); var full=Path.GetFullPath(root); var ok=true; var pending=new Stack<string>();
   if(!Directory.Exists(full)) { errors.Add($"Pasta indisponível: {full}"); continue; } pending.Push(full);
   while(pending.Count>0) { cancellationToken.ThrowIfCancellationRequested(); var dir=pending.Pop(); try {
    progress?.Report(dir); var files=Directory.EnumerateFiles(dir).Where(f=>Extensions.Contains(Path.GetExtension(f))).Select(f=>new EpisodeFile(f,NameParser.FindEpisode(f))).OrderBy(e=>e.Number??int.MaxValue).ThenBy(e=>e.Path).ToArray();
    if(files.Length>0 && found.Add(dir)) { var name=Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)); result.Add(new(dir,NameParser.Clean(name),NameParser.FindYear(name),files)); }
    foreach(var child in Directory.EnumerateDirectories(dir)) if((File.GetAttributes(child)&FileAttributes.ReparsePoint)==0) pending.Push(child);
   } catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { ok=false; errors.Add($"Não foi possível ler {dir}: {ex.Message}"); } }
   if(ok) complete.Add(full.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar));
  }
  return new ScanReport(result,complete,errors);
 },cancellationToken);
}
