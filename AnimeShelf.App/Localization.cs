using System.Windows;
namespace AnimeShelf;
public static class Localization {
 private static ResourceDictionary? current;
 public static string CurrentLanguage { get; private set; } = "pt-BR";
 public static void Apply(string language) {
  var lang=language is "en" or "es" ? language : "pt-BR"; CurrentLanguage=lang;
  var resources=System.Windows.Application.Current.Resources.MergedDictionaries;
  if(current is not null) resources.Remove(current);
  current=new ResourceDictionary { Source=new Uri($"pack://application:,,,/AnimeShelf.App;component/Languages/{lang}.xaml",UriKind.Absolute) };
  resources.Add(current);
 }
 public static string Get(string key) => System.Windows.Application.Current.TryFindResource("L_"+key) as string ?? key;
}

