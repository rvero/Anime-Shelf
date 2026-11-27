using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices;
namespace AnimeShelf;
public static class ThemedWindows {
 [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
 public static void ApplyTitle(Window window) { window.SourceInitialized += (_,_)=> { try { var dark=System.Windows.Application.Current.Resources["BackgroundBrush"] is System.Windows.Media.SolidColorBrush brush && brush.Color.R<80; int value=dark?1:0; var handle=new WindowInteropHelper(window).Handle; DwmSetWindowAttribute(handle,20,ref value,sizeof(int)); } catch(DllNotFoundException) { } }; }
 public static void ShowInfo(Window owner,string title,IEnumerable<string> lines) {
  var list=new System.Windows.Controls.ListBox { ItemsSource=lines.ToArray(),Margin=new Thickness(16),BorderThickness=new Thickness(0) };
  list.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty,"SurfaceBrush"); list.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty,"TextBrush");
  var window=new Window { Title=title,Owner=owner,ShowInTaskbar=false,Width=680,Height=440,WindowStartupLocation=WindowStartupLocation.CenterOwner,Content=list };
  window.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty,"SurfaceBrush"); window.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty,"TextBrush"); ApplyTitle(window); window.ShowDialog();
 }
}

