using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PCNetworkInspector.Helpers
{
    /// <summary>
    /// Returns NavButtonActiveStyle when SelectedPageIndex == ConverterParameter, else NavButtonStyle.
    /// </summary>
    public class NavStyleConverter : IValueConverter
    {
        public static readonly NavStyleConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int current &&
                parameter is string pageStr &&
                int.TryParse(pageStr, out int page))
            {
                var key = current == page ? "NavButtonActiveStyle" : "NavButtonStyle";
                if (Application.Current.Resources[key] is Style style)
                    return style;
            }

            return Application.Current.Resources["NavButtonStyle"] as Style
                   ?? new Style(typeof(Button));
        }

        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    /// <summary>
    /// Returns Visible when SelectedPageIndex == ConverterParameter, else Collapsed.
    /// </summary>
    public class PageVisibilityConverter : IValueConverter
    {
        public static readonly PageVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int current &&
                parameter is string pageStr &&
                int.TryParse(pageStr, out int page))
            {
                return current == page ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
