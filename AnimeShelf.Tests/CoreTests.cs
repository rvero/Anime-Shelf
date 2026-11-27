using AnimeShelf.Core.Models;
using AnimeShelf.Core.Services;
using Xunit;

namespace AnimeShelf.Tests;
public class CoreTests {
    [Theory]
    [InlineData("[Fansub] Frieren (2023) 1080p x264 BD", "Frieren")]
    [InlineData("Steins.Gate [A1B2C3D4]", "Steins Gate")]
    public void CleansNames(string input, string expected) => Assert.Equal(expected, NameParser.Clean(input));
    [Theory]
    [InlineData("Anime - 05.mkv", 5)]
    [InlineData("Anime S01E05.mp4", 5)]
    [InlineData("Anime EP05.avi", 5)]
    [InlineData("Anime 05v2.webm", 5)]
    public void FindsEpisode(string file, int expected) => Assert.Equal(expected, NameParser.FindEpisode(file));
    [Fact] public void ChoosesBestCandidate() {
        AnimeCandidate Candidate(int id, string title, int year) => new(id,title,year,12,"TV",null,null,null,null,null,null,null,Array.Empty<string>(),Array.Empty<string>());
        var best=CandidateMatcher.Best("Frieren",2023,12,new[]{Candidate(1,"Unrelated",2020),Candidate(2,"Frieren",2023)});
        Assert.Equal(2,best?.Id);
    }
}
