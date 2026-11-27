using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AnimeShelf.Core.Models;
namespace AnimeShelf;
public sealed class MainViewModel : INotifyPropertyChanged {
 public ObservableCollection<AnimeCard> Items {get;} = new();
 private string status=Localization.Get("Ready");
 public string Status { get => status; set { status=value; OnPropertyChanged(); } }
 public event PropertyChangedEventHandler? PropertyChanged;
 private void OnPropertyChanged([CallerMemberName] string? name=null) => PropertyChanged?.Invoke(this,new(name));
}

