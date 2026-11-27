using System.Windows;
using AnimeShelf.Core.Models;
using AnimeShelf.Core.Services;
namespace AnimeShelf;
public partial class MatchWindow : Window {
 private readonly AniListClient client;
 public AnimeCandidate? Selected {get; private set;}
 private sealed record Result(AnimeCandidate Candidate) { public string Display => $"{Candidate.Romaji}  ·  {Candidate.Year?.ToString() ?? "—"}  ·  {Candidate.Format ?? "—"}  ·  ID {Candidate.Id}"; }
 public MatchWindow(AniListClient client,string title) { InitializeComponent(); this.client=client; QueryBox.Text=title; }
 private async void Search_Click(object sender,RoutedEventArgs e) { try { Message.Text=Localization.Get("Searching"); var text=QueryBox.Text.Trim(); var results=int.TryParse(text,out var id) ? await client.SearchAsync(id:id) : await client.SearchAsync(title:text); Results.ItemsSource=results.Select(x=>new Result(x)).ToArray(); Message.Text=$"{results.Count} {Localization.Get("Results")}."; } catch(Exception ex) { Message.Text=$"{Localization.Get("SearchError")}: {ex.Message}"; } }
 private void Select_Click(object sender,RoutedEventArgs e) { if(Results.SelectedItem is Result result) { Selected=result.Candidate; DialogResult=true; } }
}

