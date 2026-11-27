using System.Xml.Linq;
using Xunit;
namespace AnimeShelf.Tests;
public class ThemeTests {
 [Theory][InlineData("Light.xaml")][InlineData("Dark.xaml")]
 public void ResourceKeysAreUnique(string name) {
  var document=XDocument.Load(Path.Combine(AppContext.BaseDirectory,"Themes",name));
  XNamespace x="http://schemas.microsoft.com/winfx/2006/xaml";
  var keys=document.Root!.Elements().Select(e=>(string?)e.Attribute(x+"Key")).Where(k=>k is not null).ToArray();
  Assert.Equal(keys.Length,keys.Distinct(StringComparer.Ordinal).Count());
  Assert.Contains("OverlayBrush",keys);
 }
}
