using System.Windows.Media.Imaging;
using AnimeShelf.Core.Models;
namespace AnimeShelf;
public sealed class AnimeCard : System.ComponentModel.INotifyPropertyChanged {
 private string? translated; private bool showTranslated;
 public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
 public void ToggleTranslation(string? text=null) { if(text is not null) translated=text; showTranslated=!showTranslated && translated is not null; PropertyChanged?.Invoke(this,new(nameof(Description))); }
 public LibraryEntry Entry { get; }
 private BitmapImage? cover;
 public BitmapImage? Cover { get => cover; set { cover=value; PropertyChanged?.Invoke(this,new(nameof(Cover))); } }
 public bool CoverLoading {get; set;}
 public string Format => Entry.Metadata?.Format ?? "Não identificado";
 public string UserStatus => Entry.UserStatus;
 public string Year => Entry.Metadata?.Year?.ToString() ?? "Desconhecido";
 public int SortScore => Entry.Metadata?.AverageScore ?? -1;
 public int SortPopularity => Entry.Metadata?.Popularity ?? -1;
 public string Title => Entry.CustomTitle ?? Entry.Romaji ?? Entry.SearchTitle;
 public string Dates => $"{Localization.Get("AiredLabel")}: {AnimeShelf.Core.Services.DisplayDate.Format(Entry.Metadata?.StartDate,Localization.CurrentLanguage)} {Localization.Get("FromTo")} {AnimeShelf.Core.Services.DisplayDate.Format(Entry.Metadata?.EndDate,Localization.CurrentLanguage)}";
 public string Episodes => $"{Localization.Get("EpisodesLabel")}: {Entry.Metadata?.Episodes?.ToString() ?? "—"}";
 public string Genres => $"{Localization.Get("GenresLabel")}: {string.Join(", ", Entry.Metadata?.Genres ?? Array.Empty<string>())}";
 public string Studios => $"{Localization.Get("StudiosLabel")}: {string.Join(", ", Entry.Metadata?.Studios ?? Array.Empty<string>())}";
 public string Score => $"Nota: {Entry.Metadata?.AverageScore?.ToString() ?? "—"}    Popularidade: {Entry.Metadata?.Popularity?.ToString() ?? "—"}";
 public string Progress => $"{Entry.WatchedCount}/{Entry.Metadata?.Episodes ?? Entry.LocalCount} {Localization.Get("Saved")}";
 public System.Windows.Visibility CompletedVisibility => Entry.Metadata?.Episodes is > 0 && Entry.WatchedCount >= Entry.Metadata.Episodes && Entry.Metadata.Status == "FINISHED" ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
 public double ProgressValue => Entry.Metadata?.Episodes is > 0 ? Math.Min(100,100d*Entry.WatchedCount/Entry.Metadata.Episodes.Value) : Entry.LocalCount > 0 ? 100d*Entry.WatchedCount/Entry.LocalCount : 0;
 public string Description => (showTranslated ? translated : Entry.CustomDescription ?? Entry.Metadata?.Description) ?? Localization.Get("NoSynopsisCard");
 public AnimeCard(LibraryEntry entry,BitmapImage? cover) { Entry=entry; Cover=cover; }
}












