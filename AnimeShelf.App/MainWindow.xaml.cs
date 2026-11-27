using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AnimeShelf.Core.Models;
using AnimeShelf.Core.Services;

namespace AnimeShelf;
public partial class MainWindow : Window {
 private readonly MainViewModel vm = new();
 private readonly Settings settings = Settings.Load();
 private readonly CatalogStore store = new(Path.Combine(Settings.DataDirectory,"catalog.db"));
 private readonly AniListClient aniList = new();
 private readonly CoverCache covers = new();
 private CancellationTokenSource? scanning;
 public MainWindow() { ApplyTheme(settings.Theme); InitializeComponent(); SourceInitialized += (_,_)=>UpdateTitleBar(); var work=SystemParameters.WorkArea; Width=Math.Min(1120,work.Width*0.90); Height=Math.Min(760,work.Height*0.88); MinWidth=Math.Min(480,work.Width); MinHeight=Math.Min(360,work.Height); DataContext=vm; Loaded += async (_,_) => { try { await store.InitializeAsync(); await RefreshAsync(); if(settings.Roots.Count>0) await ScanAsync(); } catch(Exception ex) { ShowError(ex); } }; Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged; Closed += (_,_) => { Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged; scanning?.Cancel(); }; }
 private void Window_ContentRendered(object? sender,EventArgs e) { Opacity=1; } private async Task RefreshAsync() { var entries=await store.ListAsync(); vm.Items.Clear(); foreach(var item in entries.Where(x=>!x.Ignored)) vm.Items.Add(new AnimeCard(item,null)); vm.Status=$"{vm.Items.Count} {Localization.Get("LibraryCount")}"; }
 private async Task ScanAsync() { if(scanning is not null) return; if(settings.Roots.Count==0) { vm.Status=Localization.Get("AddRootHint"); return; } scanning=new(); try { var progress=new Progress<string>(s=>vm.Status=s); var report=await new FolderScanner().ScanAsync(settings.Roots,progress,scanning.Token); var folders=report.Folders; await store.SaveScanAsync(folders,scanning.Token); await store.ReconcileAsync(report,scanning.Token); await AssociateNewAsync(folders,scanning.Token); await RefreshStudiosAsync(scanning.Token); await RestoreCoversAsync(scanning.Token); await RefreshAsync(); vm.Status += $" · {Localization.Get("ScanComplete")}: {folders.Count} {Localization.Get("Folders")}"; if(report.Errors.Count>0) System.Windows.MessageBox.Show(string.Join("\n",report.Errors),Localization.Get("ScanErrors"),MessageBoxButton.OK,MessageBoxImage.Warning); } catch(OperationCanceledException) { vm.Status=Localization.Get("ScanCancelled"); } catch(Exception ex) { ShowError(ex); } finally { scanning.Dispose(); scanning=null; } }
 private async Task AssociateNewAsync(IReadOnlyList<ScannedFolder> folders,CancellationToken ct) {
  var byPath=folders.ToDictionary(f=>f.Path,StringComparer.OrdinalIgnoreCase);
  foreach(var entry in (await store.ListAsync(ct)).Where(x=>!x.Ignored && !x.Manual && x.AniListId is null && byPath.ContainsKey(x.Folder))) {
   ct.ThrowIfCancellationRequested(); var folder=byPath[entry.Folder];
   if(string.IsNullOrWhiteSpace(folder.SearchTitle)) continue;
   try { vm.Status=$"{Localization.Get("SearchAniList")}: {folder.SearchTitle}"; var candidates=await aniList.SearchAsync(folder.SearchTitle,ct:ct); var best=CandidateMatcher.Best(folder.SearchTitle,folder.Year,folder.Episodes.Count,candidates); if(best is not null) { await store.AssociateAsync(entry.Id,best,false,null,ct); if(settings.CacheCoversLocally) { try { var path=await covers.DownloadAsync(best.Id,best.CoverUrl,ct); if(path is not null) await store.AssociateAsync(entry.Id,best,false,path,ct); } catch(Exception coverError) when(coverError is System.Net.Http.HttpRequestException or IOException or UnauthorizedAccessException or NotSupportedException) { } } await RefreshAsync(); } }
   catch(Exception ex) when(ex is System.Net.Http.HttpRequestException or TaskCanceledException or InvalidOperationException) { if(ct.IsCancellationRequested) throw; System.Windows.MessageBox.Show($"{Localization.Get("NoNetwork")}\n\n{ex.Message}",Localization.Get("NoNetworkTitle"),MessageBoxButton.OK,MessageBoxImage.Warning); break; }
  }
 } private async Task RefreshStudiosAsync(CancellationToken ct) {
  if(settings.AnimationStudiosUpdated) return;
  var entries=(await store.ListAsync(ct)).Where(e=>e.AniListId.HasValue).ToArray();
  foreach(var entry in entries) {
   ct.ThrowIfCancellationRequested(); vm.Status=$"{Localization.Get("UpdateStudios")}: {entry.Romaji ?? entry.SearchTitle}";
   try { var candidate=(await aniList.SearchAsync(id:entry.AniListId,ct:ct)).FirstOrDefault(); if(candidate is null) return; await store.AssociateAsync(entry.Id,candidate,entry.Manual,entry.CoverPath,ct); }
   catch(Exception ex) when(ex is System.Net.Http.HttpRequestException or TaskCanceledException or InvalidOperationException) { if(ct.IsCancellationRequested) throw; vm.Status=Localization.Get("StudiosRetry"); return; }
  }
  settings.AnimationStudiosUpdated=true; settings.Save(); await RefreshAsync();
 } private async Task RestoreCoversAsync(CancellationToken ct) { if(!settings.CacheCoversLocally) return;
  var entries=await store.ListAsync(ct);
  foreach(var entry in entries.Where(x=>x.Metadata is not null && covers.Existing(x.CoverPath) is null)) {
   ct.ThrowIfCancellationRequested();
   try { var path=await covers.DownloadAsync(entry.Metadata!.Id,entry.CoverUrl,ct); if(path is not null) await store.AssociateAsync(entry.Id,entry.Metadata,entry.Manual,path,ct); }
   catch(Exception ex) when(ex is System.Net.Http.HttpRequestException or IOException or UnauthorizedAccessException or NotSupportedException) { vm.Status=$"{Localization.Get("CoverMissing")}: {entry.SearchTitle}"; }
  }
 } private async void Scan_Click(object sender,RoutedEventArgs e) => await ScanAsync();
 private void Cancel_Click(object sender,RoutedEventArgs e) => scanning?.Cancel();
 private void OpenCovers_Click(object sender,RoutedEventArgs e) { try { var dir=Path.Combine(Settings.DataDirectory,"covers"); Directory.CreateDirectory(dir); Process.Start(new ProcessStartInfo(dir) {UseShellExecute=true}); } catch(Exception ex) { ShowError(ex); } }
 private void Card_RightClick(object sender,System.Windows.Input.MouseButtonEventArgs e) { if(sender is System.Windows.Controls.ListBoxItem item) item.IsSelected=true; } private void Window_PreviewMouseDown(object sender,System.Windows.Input.MouseButtonEventArgs e) { if(e.OriginalSource is not DependencyObject node) return; var parent=node; while(parent is not null) { if(parent is System.Windows.Controls.ListBoxItem || parent is System.Windows.Controls.Primitives.ScrollBar) return; parent=parent is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D ? System.Windows.Media.VisualTreeHelper.GetParent(parent) : System.Windows.LogicalTreeHelper.GetParent(parent); } LibraryList.SelectedItem=null; } private async void AddRoot_Click(object sender,RoutedEventArgs e) { using var dialog=new System.Windows.Forms.FolderBrowserDialog { Description=Localization.Get("RootPicker"), UseDescriptionForTitle=true }; if(dialog.ShowDialog()!=System.Windows.Forms.DialogResult.OK) return; if(!settings.Roots.Contains(dialog.SelectedPath,StringComparer.OrdinalIgnoreCase)) { settings.Roots.Add(dialog.SelectedPath); try { settings.Save(); } catch(Exception ex) { ShowError(ex); return; } } await ScanAsync(); }
 private void View_Changed(object sender,SelectionChangedEventArgs e) {
  if(!IsLoaded || ViewBox is null || LibraryList is null) return;
  var view=(ViewBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
  bool compact=view=="compact";
  if(compact) {
   var template=new DataTemplate(typeof(AnimeCard)); var panel=new FrameworkElementFactory(typeof(TextBlock)); panel.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("Title")); panel.SetValue(TextBlock.MarginProperty,new Thickness(8,7,8,7)); template.VisualTree=panel; LibraryList.ItemTemplate=template;
  } else LibraryList.ItemTemplate=(DataTemplate)FindResource(view=="covers" ? "CoverCardTemplate" : "DetailCardTemplate");
 } private void LibraryWheel(object sender,System.Windows.Input.MouseWheelEventArgs e) { var scroll=SmoothScroll.FindScrollViewer(LibraryList); if(scroll is null || scroll.ScrollableHeight<=0) return; SmoothScroll.ScrollBy(scroll,-e.Delta*0.75); e.Handled=true; } private void Arrangement_Changed(object sender,SelectionChangedEventArgs e) {
  if(!IsLoaded || SortBox is null) return;
  var view=CollectionViewSource.GetDefaultView(LibraryList.ItemsSource); using(view.DeferRefresh()) { view.GroupDescriptions.Clear(); view.SortDescriptions.Clear();
   var sort=(SortBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(); var order=sort switch { "progress"=>"ProgressValue", _=>"Title" }; view.SortDescriptions.Add(new System.ComponentModel.SortDescription(order,order=="Title" ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending));
  }
 }
 private async void History_Click(object sender,RoutedEventArgs e) { try { var history=await store.HistoryAsync(Localization.Get("EpisodeSingular")); ThemedWindows.ShowInfo(this,Localization.Get("HistoryTitle"),history.Count==0 ? new[]{Localization.Get("EmptyHistory")} : history); } catch(Exception ex) { ShowError(ex); } }
 private void Statistics_Click(object sender,RoutedEventArgs e) { var cards=vm.Items.ToArray(); ThemedWindows.ShowInfo(this,Localization.Get("StatisticsTitle"),new[]{ $"Animes: {cards.Length}", $"{Localization.Get("LocalEpisodes")}: {cards.Sum(x=>x.Entry.LocalCount)}", $"{Localization.Get("WatchedEpisodes")}: {cards.Sum(x=>x.Entry.WatchedCount)}" }); }private void Filter_Changed(object sender,TextChangedEventArgs e) { if(!IsLoaded) return; var filter=FilterBox.Text.Trim(); CollectionViewSource.GetDefaultView(LibraryList.ItemsSource).Filter = item => item is AnimeCard x && (x.Entry.SearchTitle.Contains(filter,StringComparison.OrdinalIgnoreCase) || x.Title.Contains(filter,StringComparison.OrdinalIgnoreCase)); }
 private async void OpenEpisodes_Click(object sender,RoutedEventArgs e) { if(LibraryList.SelectedItem is not AnimeCard card) return; try { var episodes=await store.EpisodesAsync(card.Entry.Id); if(card.Cover is null) { try { card.Cover=card.Entry.CustomCoverPath is not null ? await Task.Run(()=>CoverCache.Load(covers.Existing(card.Entry.CustomCoverPath))) : settings.CacheCoversLocally ? await Task.Run(()=>CoverCache.Load(covers.Existing(card.Entry.CoverPath))) : await covers.LoadOnlineAsync(card.Entry.CoverUrl); } catch(Exception ex) when(ex is System.Net.Http.HttpRequestException or IOException or NotSupportedException or TaskCanceledException or FileFormatException) { } } var win=new EpisodesWindow(store,card.Entry,episodes,settings.MarkOnOpen,card.Cover) { Owner=this }; ModalShade.Visibility=Visibility.Visible; try { win.ShowDialog(); } finally { ModalShade.Visibility=Visibility.Collapsed; } await RefreshAsync(); } catch(Exception ex) { ShowError(ex); } } private void OpenFolder_Click(object sender,RoutedEventArgs e) { if(LibraryList.SelectedItem is AnimeCard card) try { Process.Start(new ProcessStartInfo(card.Entry.Folder) {UseShellExecute=true}); } catch(Exception ex) { ShowError(ex); } }
 private async void Cover_Loaded(object sender,RoutedEventArgs e) {
  if(sender is not System.Windows.Controls.Image { DataContext: AnimeCard card } || card.Cover is not null || card.CoverLoading) return;
  card.CoverLoading=true;
  try { if(card.Entry.CustomCoverPath is not null || settings.CacheCoversLocally) { var path=await Task.Run(()=>covers.Existing(card.Entry.CustomCoverPath ?? card.Entry.CoverPath)); card.Cover=await Task.Run(()=>CoverCache.Load(path)); } else card.Cover=await covers.LoadOnlineAsync(card.Entry.CoverUrl); }
  catch(Exception ex) when(ex is System.Net.Http.HttpRequestException or IOException or NotSupportedException or TaskCanceledException or FileFormatException) { /* A missing cover leaves the placeholder visible. */ }
  finally { card.CoverLoading=false; }
 } private async void Translate_Click(object sender,RoutedEventArgs e) {
  if(LibraryList.SelectedItem is not AnimeCard card || card.Entry.AniListId is not int id || string.IsNullOrWhiteSpace(card.Entry.CustomDescription ?? card.Entry.Metadata?.Description)) return;
  if(!settings.TranslationEnabled) { System.Windows.MessageBox.Show(Localization.Get("TranslationEnableHint"),"AnimeShelf"); return; }
  try { vm.Status=Localization.Get("Translating"); var service=new LibreTranslationService(settings.TranslationEndpoint,settings.TranslationApiKey); var text=await TranslationCache.GetAsync(store,service,id,(card.Entry.CustomDescription ?? card.Entry.Metadata!.Description!),settings.TranslationLanguage); card.ToggleTranslation(text); vm.Status=Localization.Get("Translated"); } catch(Exception ex) { vm.Status=Localization.Get("TranslationUnavailable"); System.Windows.MessageBox.Show(ex.Message,Localization.Get("TranslationError"),MessageBoxButton.OK,MessageBoxImage.Warning); }
 } private async void EditAnime_Click(object sender,RoutedEventArgs e) { if(LibraryList.SelectedItem is not AnimeCard card) return; try { var dialog=new EditAnimeWindow(card.Entry,store,covers,card.Cover) { Owner=this }; ModalShade.Visibility=Visibility.Visible; bool? saved; try { saved=dialog.ShowDialog(); } finally { ModalShade.Visibility=Visibility.Collapsed; } if(saved==true) await RefreshAsync(); } catch(Exception ex) { ShowError(ex); } } private async void Match_Click(object sender,RoutedEventArgs e) {
  if(LibraryList.SelectedItem is not AnimeCard card) return;
  var dialog=new MatchWindow(aniList,card.Entry.SearchTitle) { Owner=this };
  if(dialog.ShowDialog()!=true || dialog.Selected is not AnimeCandidate candidate) return;
  string? path=null;
  try { vm.Status="Baixando capa…"; if(settings.CacheCoversLocally) path=await covers.DownloadAsync(candidate.Id,candidate.CoverUrl); }
  catch(Exception ex) { System.Windows.MessageBox.Show($"{Localization.Get("CoverDownloadError")}\n{ex.Message}","AnimeShelf"); }
  try { await store.AssociateAsync(card.Entry.Id,candidate,true,path); await RefreshAsync(); } catch(Exception ex) { ShowError(ex); }
 } private async void Ignore_Click(object sender,RoutedEventArgs e) { if(LibraryList.SelectedItem is not AnimeCard card) return; try { await store.SetIgnoredAsync(card.Entry.Id,true); await RefreshAsync(); } catch(Exception ex) { ShowError(ex); } }
 private async void Unassociate_Click(object sender,RoutedEventArgs e) { if(LibraryList.SelectedItem is not AnimeCard card) return; try { await store.UnassociateAsync(card.Entry.Id); await RefreshAsync(); } catch(Exception ex) { ShowError(ex); } } private async void Settings_Click(object sender,RoutedEventArgs e) { var window=new SettingsWindow(settings,store) { Owner=this }; window.ShowDialog(); Localization.Apply(settings.Language); if(window.RedownloadRequested) { try { await RestoreCoversAsync(CancellationToken.None); } catch(Exception ex) { ShowError(ex); } } await RefreshAsync(); }
 private void Library_Click(object sender,RoutedEventArgs e) => LibraryList.Focus();
 [System.Runtime.InteropServices.DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
 private void UpdateTitleBar() { try { var hwnd=new System.Windows.Interop.WindowInteropHelper(this).Handle; if(hwnd==IntPtr.Zero) return; var dark=settings.Theme=="Escuro" || (settings.Theme=="Sistema" && Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")?.GetValue("AppsUseLightTheme") is int x && x==0); int value=dark?1:0; DwmSetWindowAttribute(hwnd,20,ref value,sizeof(int)); } catch(DllNotFoundException) { } } private void OnSystemPreferenceChanged(object sender,Microsoft.Win32.UserPreferenceChangedEventArgs e) { if(settings.Theme=="Sistema") Dispatcher.BeginInvoke(()=>{ ApplyTheme("Sistema"); UpdateTitleBar(); }); } private void ApplyTheme(string theme) { if(theme=="Sistema") { using var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"); theme=key?.GetValue("AppsUseLightTheme") is int value && value==0 ? "Escuro" : "Claro"; } var uri=new Uri($"pack://application:,,,/AnimeShelf.App;component/Themes/{(theme=="Escuro" ? "Dark" : "Light")}.xaml",UriKind.Absolute); System.Windows.Application.Current.Resources.MergedDictionaries.Clear(); System.Windows.Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source=uri }); Localization.Apply(settings.Language); }
 private void ChangeTheme(string theme) { try { settings.Theme=theme; ApplyTheme(theme); UpdateTitleBar(); settings.Save(); } catch(Exception ex) { ShowError(ex); } }
 private void SystemTheme_Click(object sender,RoutedEventArgs e)=>ChangeTheme("Sistema");
 private void Light_Click(object sender,RoutedEventArgs e)=>ChangeTheme("Claro");
 private void Dark_Click(object sender,RoutedEventArgs e)=>ChangeTheme("Escuro");
 private async void Import_Click(object sender,RoutedEventArgs e) {
  if(scanning is not null) { System.Windows.MessageBox.Show(Localization.Get("ScanBeforeImport"),"AnimeShelf"); return; }
  var dialog=new Microsoft.Win32.OpenFileDialog { Filter="Backup AnimeShelf (*.zip)|*.zip" }; if(dialog.ShowDialog()!=true) return;
  if(System.Windows.MessageBox.Show(Localization.Get("ImportConfirm"),Localization.Get("ImportTitle"),MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
  try { vm.Status=Localization.Get("Importing"); await BackupService.ImportAsync(dialog.FileName); await store.InitializeAsync(); await RefreshAsync(); vm.Status=Localization.Get("Imported"); } catch(Exception ex) { ShowError(ex); }
 } private async void Export_Click(object sender,RoutedEventArgs e) { var dialog=new Microsoft.Win32.SaveFileDialog { Filter="Backup AnimeShelf (*.zip)|*.zip",FileName="AnimeShelf-backup.zip" }; if(dialog.ShowDialog()!=true) return; try { vm.Status=Localization.Get("Exporting"); await BackupService.ExportAsync(store,dialog.FileName); vm.Status=Localization.Get("Exported"); } catch(Exception ex) { ShowError(ex); } } private void Exit_Click(object sender,RoutedEventArgs e)=>Close();
 private void About_Click(object sender,RoutedEventArgs e)=>System.Windows.MessageBox.Show(Localization.Get("AboutText"),Localization.Get("About"));
 private void ShowError(Exception ex) { vm.Status=Localization.Get("Error"); System.Windows.MessageBox.Show(ex.Message,"AnimeShelf",MessageBoxButton.OK,MessageBoxImage.Warning); }
}
































