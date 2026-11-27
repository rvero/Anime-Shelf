using System.Xml.Linq;
using Xunit;
namespace AnimeShelf.Tests;
public class LanguageTests {
 [Fact] public void LanguageDictionariesHaveTheSameUniqueKeys() {
  XNamespace x="http://schemas.microsoft.com/winfx/2006/xaml";
  string[] Keys(string name) => XDocument.Load(Path.Combine(AppContext.BaseDirectory,"Languages",name+".xaml")).Root!.Elements().Select(e=>(string?)e.Attribute(x+"Key")).Where(k=>k is not null).Cast<string>().OrderBy(k=>k,StringComparer.Ordinal).ToArray();
  var pt=Keys("pt-BR"); Assert.Equal(pt.Length,pt.Distinct(StringComparer.Ordinal).Count()); Assert.Equal(pt,Keys("en")); Assert.Equal(pt,Keys("es"));
 }
}
