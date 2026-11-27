using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using AnimeShelf.Core.Models;
using AnimeShelf.Core.Services;
namespace AnimeShelf;
public partial class EpisodesWindow : Window {
 private readonly CatalogStore store; private readonly LibraryEntry entry; private readonly bool markOnOpen;
 private readonly ObservableCollection<EpisodeRow> rows=new();
 public EpisodesWindow(CatalogStore store, LibraryEntry entry, IReadOnlyList<EpisodeFile> episodes, bool markOnOpen, System.Windows.Media.ImageSource? cover) { InitializeComponent(); this.store=store; this.entry=entry; this.markOnOpen=markOnOpen; Heading.Text=entry.CustomTitle ?? entry.Romaji ?? entry.SearchTitle; Genres.Text=entry.Metadata?.Genres.Count > 0 ? Localization.Get("GenresLabel") + ": " + string.Join(" · ",entry.Metadata.Genres) : Localization.Get("NoGenre"); Synopsis.Text=string.IsNullOrWhiteSpace(entry.CustomDescription ?? entry.Metadata?.Description) ? Localization.Get("NoDescription") : entry.CustomDescription ?? entry.Metadata!.Description; CoverImage.Source=cover; var work=SystemParameters.WorkArea; Width=Math.Min(720,work.Width*0.9); Height=Math.Min(620,work.Height*0.9); MinWidth=Math.Min(450,work.Width); MinHeight=Math.Min(400,work.Height); foreach(var ep in episodes) { var row=new EpisodeRow(ep); row.PropertyChanged+=async (_,_)=>await SaveAsync(row); rows.Add(row); } List.ItemsSource=rows; UpdateStatus(); }
private void UpdateStatus() => Status.Text=$"{rows.Count(x=>x.Watched)}/{rows.Count} {Localization.Get("LocalWatched")}";
 private async Task SaveAsync(EpisodeRow row) { try { await store.MarkWatchedAsync(entry.Id,row.Episode.Path,row.Watched); UpdateStatus(); } catch(Exception ex) { System.Windows.MessageBox.Show(ex.Message,Localization.Get("SaveError"),MessageBoxButton.OK,MessageBoxImage.Warning); } }
 private void Play(EpisodeRow row) { try { Process.Start(new ProcessStartInfo(row.Episode.Path) {UseShellExecute=true}); if(markOnOpen) row.Watched=true; } catch(Exception ex) { System.Windows.MessageBox.Show(ex.Message,Localization.Get("PlayError"),MessageBoxButton.OK,MessageBoxImage.Warning); } }
 private void EpisodeWheel(object sender,System.Windows.Input.MouseWheelEventArgs e) { var scroll=SmoothScroll.FindScrollViewer(List); if(scroll is null || scroll.ScrollableHeight<=0) return; SmoothScroll.ScrollBy(scroll,-e.Delta * 0.75); e.Handled=true; } private void Close_Click(object sender,RoutedEventArgs e) => Close();
 private void PlayButton_Click(object sender,RoutedEventArgs e) { if(sender is System.Windows.Controls.Button { DataContext: EpisodeRow row }) { e.Handled=true; Play(row); } } private void Play_Click(object sender,RoutedEventArgs e) { if(List.SelectedItem is EpisodeRow row) Play(row); }
 private void Next_Click(object sender,RoutedEventArgs e) { var next=rows.FirstOrDefault(x=>!x.Watched); if(next is null) { System.Windows.MessageBox.Show(Localization.Get("NoPending"),"AnimeShelf"); return; } List.SelectedItem=next; Play(next); }
}







