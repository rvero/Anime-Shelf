using System.Diagnostics;
using System.IO;
using System.Windows;
using AnimeShelf.Core.Services;
namespace AnimeShelf;
public partial class SettingsWindow : Window {
 private readonly Settings settings; private readonly CatalogStore store; public bool RedownloadRequested {get; private set;}
 public SettingsWindow(Settings settings,CatalogStore store) { InitializeComponent(); ThemedWindows.ApplyTitle(this); this.settings=settings; this.store=store; InterfaceLanguage.SelectedIndex=settings.Language=="en" ? 1 : settings.Language=="es" ? 2 : 0; Roots.ItemsSource=settings.Roots.ToArray(); Mark.IsChecked=settings.MarkOnOpen; CacheCovers.IsChecked=settings.CacheCoversLocally; TranslateEnabled.IsChecked=settings.TranslationEnabled; Endpoint.Text=settings.TranslationEndpoint; TargetLanguage.Text=settings.TranslationLanguage; ApiKey.Text=settings.TranslationApiKey; }
 private void Remove_Click(object sender,RoutedEventArgs e) { if(Roots.SelectedItem is not string root) return; settings.Roots.Remove(root); Roots.ItemsSource=settings.Roots.ToArray(); }
 private void OpenCovers_Click(object sender,RoutedEventArgs e) { try { var path=Path.Combine(Settings.DataDirectory,"covers"); Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo(path){UseShellExecute=true}); } catch(Exception ex) { System.Windows.MessageBox.Show(ex.Message,"AnimeShelf"); } }
 private void ClearCovers_Click(object sender,RoutedEventArgs e) { if(System.Windows.MessageBox.Show(Localization.Get("ClearConfirm"),Localization.Get("Covers"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return; try { var dir=Path.Combine(Settings.DataDirectory,"covers"); if(Directory.Exists(dir)) foreach(var file in Directory.EnumerateFiles(dir,"*.jpg").Where(f=>int.TryParse(Path.GetFileNameWithoutExtension(f),out _))) File.Delete(file); System.Windows.MessageBox.Show(Localization.Get("CacheCleared"),"AnimeShelf"); } catch(Exception ex) { System.Windows.MessageBox.Show(ex.Message,"AnimeShelf"); } }
 private void Redownload_Click(object sender,RoutedEventArgs e) { if(System.Windows.MessageBox.Show(Localization.Get("RedownloadConfirm"),Localization.Get("Covers"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes) return; try { var dir=Path.Combine(Settings.DataDirectory,"covers"); if(Directory.Exists(dir)) foreach(var file in Directory.EnumerateFiles(dir,"*.jpg").Where(f=>int.TryParse(Path.GetFileNameWithoutExtension(f),out _))) File.Delete(file); RedownloadRequested=true; settings.Language=(InterfaceLanguage.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "pt-BR"; settings.CacheCoversLocally=CacheCovers.IsChecked==true; settings.MarkOnOpen=Mark.IsChecked==true; settings.TranslationEnabled=TranslateEnabled.IsChecked==true; settings.TranslationEndpoint=Endpoint.Text.Trim(); settings.TranslationLanguage=TargetLanguage.Text.Trim(); settings.TranslationApiKey=ApiKey.Text.Trim(); settings.Save(); Close(); } catch(Exception ex) { System.Windows.MessageBox.Show(ex.Message,"AnimeShelf"); } } private async void Restore_Click(object sender,RoutedEventArgs e) { try { foreach(var item in (await store.ListAsync()).Where(x=>x.Ignored)) await store.SetIgnoredAsync(item.Id,false); System.Windows.MessageBox.Show(Localization.Get("IgnoredRestored"),"AnimeShelf"); } catch(Exception ex) { System.Windows.MessageBox.Show(ex.Message,"AnimeShelf"); } }
 private void Close_Click(object sender,RoutedEventArgs e) { try { settings.Language=(InterfaceLanguage.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "pt-BR"; settings.CacheCoversLocally=CacheCovers.IsChecked==true; settings.MarkOnOpen=Mark.IsChecked==true; settings.TranslationEnabled=TranslateEnabled.IsChecked==true; settings.TranslationEndpoint=Endpoint.Text.Trim(); settings.TranslationLanguage=TargetLanguage.Text.Trim(); settings.TranslationApiKey=ApiKey.Text.Trim(); settings.Save(); Close(); } catch(Exception ex) { System.Windows.MessageBox.Show(ex.Message,"AnimeShelf"); } }
}







