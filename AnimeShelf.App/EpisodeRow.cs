using System.ComponentModel;
using System.Runtime.CompilerServices;
using AnimeShelf.Core.Models;
namespace AnimeShelf;
public sealed class EpisodeRow : INotifyPropertyChanged {
 public EpisodeFile Episode {get;}
 public string Label => $"{(Episode.Number is int n ? $"{Localization.Get("EpisodeSingular")} {n:00}" : Localization.Get("EpisodeSingular"))} — {System.IO.Path.GetFileName(Episode.Path)}";
 private bool watched;
 public bool Watched { get=>watched; set { if(watched==value) return; watched=value; PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(nameof(Watched))); } }
 public EpisodeRow(EpisodeFile episode) { Episode=episode; watched=episode.Watched; }
 public event PropertyChangedEventHandler? PropertyChanged;
}

