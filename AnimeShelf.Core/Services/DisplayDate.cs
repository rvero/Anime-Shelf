using System.Globalization;
namespace AnimeShelf.Core.Services;
public static class DisplayDate {
 public static string Format(string? date,string language) {
  if(string.IsNullOrWhiteSpace(date)) return "—";
  if(DateTime.TryParseExact(date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var full)) return full.ToString(language=="en" ? "MM/dd/yy" : "dd/MM/yy",CultureInfo.InvariantCulture);
  if(DateTime.TryParseExact(date,"yyyy-MM",CultureInfo.InvariantCulture,DateTimeStyles.None,out var month)) return month.ToString("MM/yyyy",CultureInfo.InvariantCulture);
  return date;
 }
}
