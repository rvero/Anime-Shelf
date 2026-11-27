using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace AnimeShelf;
public static class SmoothScroll {
 private static readonly DependencyProperty OffsetProperty=DependencyProperty.RegisterAttached("Offset",typeof(double),typeof(SmoothScroll),new PropertyMetadata(0d,(o,e)=>((ScrollViewer)o).ScrollToVerticalOffset((double)e.NewValue)));
 public static ScrollViewer? FindScrollViewer(DependencyObject root) { if(root is ScrollViewer scroll) return scroll; for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) { var found=FindScrollViewer(VisualTreeHelper.GetChild(root,i)); if(found is not null) return found; } return null; }
 public static void ScrollBy(ScrollViewer viewer,double delta) { var current=viewer.VerticalOffset; var target=Math.Clamp(current+delta,0,viewer.ScrollableHeight); var animation=new DoubleAnimation(current,target,TimeSpan.FromMilliseconds(180)) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut },FillBehavior=FillBehavior.Stop }; viewer.BeginAnimation(OffsetProperty,null); viewer.SetValue(OffsetProperty,target); viewer.BeginAnimation(OffsetProperty,animation,HandoffBehavior.SnapshotAndReplace); }
}
