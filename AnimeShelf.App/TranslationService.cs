using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AnimeShelf.Core.Services;
namespace AnimeShelf;
public interface ITranslationService { Task<string> TranslateAsync(string source,string target,CancellationToken ct=default); }
public sealed class LibreTranslationService : ITranslationService {
 private readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(25)}; private readonly string endpoint; private readonly string key;
 public LibreTranslationService(string endpoint,string key) { this.endpoint=endpoint.TrimEnd('/'); this.key=key; }
 public async Task<string> TranslateAsync(string source,string target,CancellationToken ct=default) {
  if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri) || (uri.Scheme!=Uri.UriSchemeHttps && !(uri.IsLoopback && uri.Scheme==Uri.UriSchemeHttp))) throw new InvalidOperationException(Localization.Get("TranslationUrl"));
  using var response=await http.PostAsJsonAsync(endpoint+"/translate",new { q=source,source="en",target,format="text",api_key=key },ct); response.EnsureSuccessStatusCode(); using var doc=JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct)); return doc.RootElement.GetProperty("translatedText").GetString() ?? throw new InvalidOperationException(Localization.Get("TranslationEmpty"));
 }
}
public static class TranslationCache {
 public static async Task<string> GetAsync(CatalogStore store,ITranslationService service,int id,string source,string language,CancellationToken ct=default) { var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))); var cached=await store.GetTranslationAsync(id,language,hash,ct); if(cached is not null) return cached; var translated=await service.TranslateAsync(source,language,ct); await store.SaveTranslationAsync(id,language,hash,translated,ct); return translated; }
}
