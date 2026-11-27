using System.IO;
using System.Windows;
using AnimeShelf.Core.Models;
using AnimeShelf.Core.Services;
namespace AnimeShelf;
public partial class EditAnimeWindow : Window {
 private readonly LibraryEntry entry; private readonly CatalogStore store; private readonly CoverCache covers; private readonly string originalTitle; private readonly string originalSynopsis;
 private string? selectedFile; private bool resetCover;
 public EditAnimeWindow(LibraryEntry entry,CatalogStore store,CoverCache covers,System.Windows.Media.ImageSource? image) { InitializeComponent(); ThemedWindows.ApplyTitle(this); this.entry=entry; this.store=store; this.covers=covers; originalTitle=entry.CustomTitle ?? entry.Romaji ?? entry.SearchTitle; originalSynopsis=entry.CustomDescription ?? entry.Metadata?.Description ?? ""; TitleBox.Text=originalTitle; SynopsisBox.Text=originalSynopsis; Preview.Source=image; CoverInfo.Text=Localization.Get("CustomCoverHint"); }
 private void Choose_Click(object sender,RoutedEventArgs e) { var picker=new Microsoft.Win32.OpenFileDialog { Filter="Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp|All files|*.*" }; if(picker.ShowDialog(this)!=true) return; var preview=CoverCache.Load(picker.FileName); if(preview is null) { System.Windows.MessageBox.Show(Localization.Get("InvalidCover"),Localization.Get("EditAnime")); return; } selectedFile=picker.FileName; resetCover=false; Preview.Source=preview; CoverInfo.Text=Path.GetFileName(selectedFile); }
 private void ResetCover_Click(object sender,RoutedEventArgs e) { selectedFile=null; resetCover=true; Preview.Source=null; CoverInfo.Text=Localization.Get("CoverResetHint"); }
 private void Cancel_Click(object sender,RoutedEventArgs e) => Close();
 private async void Save_Click(object sender,RoutedEventArgs e) { string? imported=null; try {
  var title=TitleBox.Text.Trim(); var synopsis=SynopsisBox.Text.Trim();
  var customTitle=title.Length==0 ? null : title==originalTitle ? entry.CustomTitle : title;
  var customSynopsis=synopsis.Length==0 ? null : synopsis==originalSynopsis ? entry.CustomDescription : synopsis;
  if(selectedFile is not null) imported=await covers.ImportCustomAsync(selectedFile);
  var cover=imported ?? (resetCover ? null : entry.CustomCoverPath);
  await store.SaveCustomAsync(entry.Id,customTitle,customSynopsis,cover); DialogResult=true;
 } catch(Exception ex) { if(imported is not null) try { covers.DeleteCustom(imported); } catch(IOException) { } System.Windows.MessageBox.Show(ex.Message,Localization.Get("EditAnime"),MessageBoxButton.OK,MessageBoxImage.Warning); } }
}
