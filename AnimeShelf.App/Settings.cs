using System.IO;
using System.Text.Json;
namespace AnimeShelf;
public sealed class Settings {
 public List<string> Roots { get; set; } = new();
 public string Theme { get; set; } = "Sistema";
 public string Language { get; set; } = "pt-BR";
 public bool TranslationEnabled { get; set; } = false;
 public string TranslationLanguage { get; set; } = "pt";
 public string TranslationEndpoint { get; set; } = "https://libretranslate.com";
 public string TranslationApiKey { get; set; } = "";
 public bool AnimationStudiosUpdated { get; set; } = false;
 public bool CacheCoversLocally { get; set; } = true;
 public bool MarkOnOpen { get; set; } = true;
 public static string DataDirectory => Environment.GetEnvironmentVariable("ANIMESHELF_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnimeShelf");
 public static Settings Load() { try { var p=Path.Combine(DataDirectory,"settings.json"); return File.Exists(p) ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(p)) ?? new() : new(); } catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return new(); } }
 public void Save() { Directory.CreateDirectory(DataDirectory); File.WriteAllText(Path.Combine(DataDirectory,"settings.json"),JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true})); }
}







