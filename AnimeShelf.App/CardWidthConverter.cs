using System.Globalization;
using System.Windows.Data;
namespace AnimeShelf;
public sealed class CardWidthConverter : IValueConverter {
 public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
  if(value is not double width || width<=0) return 310d;
  var desired=parameter is string text && double.TryParse(text,System.Globalization.NumberStyles.Float,CultureInfo.InvariantCulture,out var parsed) ? parsed : 350;
  if(parameter is not null) { var viewport=Math.Max(150,width-32); var count=Math.Max(1,(int)(viewport/(desired+10))); return Math.Max(100,Math.Floor(viewport/count)-11); }
  var usable=Math.Max(220,width-50); var columns=Math.Max(1,(int)(usable/desired));
  return Math.Max(140,usable/columns-1);
 }
 public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>System.Windows.Data.Binding.DoNothing;
}






public sealed class PosterHeightConverter : IValueConverter {
 public object Convert(object value,Type targetType,object parameter,CultureInfo culture) => value is double width && double.IsFinite(width) ? width * 1.5 : 360d;
 public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>System.Windows.Data.Binding.DoNothing;
}


