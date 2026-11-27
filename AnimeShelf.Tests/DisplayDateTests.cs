using AnimeShelf.Core.Services;
using Xunit;
namespace AnimeShelf.Tests;
public class DisplayDateTests {
 [Theory]
 [InlineData("2024-03-12","pt-BR","12/03/24")]
 [InlineData("2024-03-12","es","12/03/24")]
 [InlineData("2024-03-12","en","03/12/24")]
 [InlineData("2024-03","pt-BR","03/2024")]
 [InlineData("2024","en","2024")]
 [InlineData(null,"en","—")]
 public void FormatsWithoutInventingMissingParts(string? date,string language,string expected) => Assert.Equal(expected,DisplayDate.Format(date,language));
}
