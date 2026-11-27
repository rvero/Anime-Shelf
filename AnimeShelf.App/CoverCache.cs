using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
namespace AnimeShelf;
public sealed class CoverCache {
 private readonly HttpClient client = new() { Timeout=TimeSpan.FromSeconds(30) };
 private readonly SemaphoreSlim limit = new(4);
 private readonly string folder=Path.Combine(Settings.DataDirectory,"covers");
 public string? Existing(string? relative) { if(string.IsNullOrEmpty(relative) || Path.GetFileName(relative)!=relative) return null; var p=Path.Combine(folder,relative); return Valid(p) ? p : null; }
 private static bool Valid(string path) { try { using var stream=File.OpenRead(path); var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.OnLoad); return decoder.Frames.Count>0 && decoder.Frames[0].PixelWidth>0; } catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or NotSupportedException or FileFormatException) { return false; } }
 public async Task<string?> DownloadAsync(int id,string? url,CancellationToken ct=default) { if(string.IsNullOrWhiteSpace(url)) return null; Directory.CreateDirectory(folder); var name=$"{id}.jpg"; var path=Path.Combine(folder,name); if(Valid(path)) return name; await limit.WaitAsync(ct); try { if(Valid(path)) return name; var bytes=await client.GetByteArrayAsync(url,ct); var tmp=Path.Combine(folder,$"{id}.{Guid.NewGuid():N}.tmp"); try { await File.WriteAllBytesAsync(tmp,bytes,ct); if(!Valid(tmp)) throw new InvalidDataException(Localization.Get("InvalidCover")); File.Move(tmp,path,true); return name; } finally { if(File.Exists(tmp)) File.Delete(tmp); } } finally { limit.Release(); } }
 public static BitmapImage? Load(string? path) { if(path is null) return null; try { var image=new BitmapImage(); image.BeginInit(); image.CacheOption=BitmapCacheOption.OnLoad; image.DecodePixelWidth=320; image.UriSource=new Uri(path); image.EndInit(); image.Freeze(); return image; } catch(Exception ex) when(ex is IOException or NotSupportedException or FileFormatException) { return null; } }
 public async Task<BitmapImage?> LoadOnlineAsync(string? url,CancellationToken ct=default) {
  if(!Uri.TryCreate(url,UriKind.Absolute,out var uri) || uri.Scheme!=Uri.UriSchemeHttps) return null;
  await limit.WaitAsync(ct); try { using var request=new HttpRequestMessage(HttpMethod.Get,uri); request.Headers.CacheControl=new System.Net.Http.Headers.CacheControlHeaderValue {NoCache=true,NoStore=true}; using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct); response.EnsureSuccessStatusCode(); if(response.Content.Headers.ContentLength > 10*1024*1024) throw new InvalidDataException(Localization.Get("CoverLarge")); using var source=await response.Content.ReadAsStreamAsync(ct); using var buffer=new MemoryStream(); var bytes=new byte[81920]; int read; while((read=await source.ReadAsync(bytes,ct))>0) { if(buffer.Length+read>10*1024*1024) throw new InvalidDataException(Localization.Get("CoverLarge")); await buffer.WriteAsync(bytes.AsMemory(0,read),ct); } buffer.Position=0; var bitmap=new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption=BitmapCacheOption.OnLoad; bitmap.DecodePixelWidth=320; bitmap.StreamSource=buffer; bitmap.EndInit(); bitmap.Freeze(); return bitmap; } finally { limit.Release(); }
 } public async Task<string> ImportCustomAsync(string source,CancellationToken ct=default) {
  Directory.CreateDirectory(folder); var name=$"custom-{Guid.NewGuid():N}.jpg"; var target=Path.Combine(folder,name);
  try { await using(var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read,81920,true)) { if(input.Length>20*1024*1024) throw new InvalidDataException(Localization.Get("CoverLarge")); await using var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true); await input.CopyToAsync(output,ct); } if(!Valid(target)) throw new InvalidDataException(Localization.Get("InvalidCover")); return name; } catch { if(File.Exists(target)) File.Delete(target); throw; }
 }
 public void DeleteCustom(string? name) { if(name is null || !System.Text.RegularExpressions.Regex.IsMatch(name,@"^custom-[0-9a-f]{32}\.jpg$",System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return; var path=Path.Combine(folder,name); if(File.Exists(path)) File.Delete(path); }}



