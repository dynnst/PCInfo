using PCNetworkInspector.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PCNetworkInspector.Helpers
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public static readonly InverseBoolToVisibilityConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b && !b ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value != null ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public static readonly StringToVisibilityConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c)
            => !string.IsNullOrWhiteSpace(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class ListToStringConverter : IValueConverter
    {
        public static readonly ListToStringConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is IEnumerable<string> list)
                return string.Join(", ", list.Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.Empty;
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class BoolToYesNoConverter : IValueConverter
    {
        public static readonly BoolToYesNoConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b ? (b ? "Evet" : "Hayır") : string.Empty;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class DivideConverter : IMultiValueConverter
    {
        public static readonly DivideConverter Instance = new();
        public object Convert(object[] values, Type t, object p, CultureInfo c)
        {
            if (values.Length >= 2 &&
                values[0] is int num &&
                values[1] is int den &&
                den > 0)
                return (double)num / den * 100.0;
            return 0.0;
        }
        public object[] ConvertBack(object value, Type[] t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    // For connection test result items
    public class TestResultItem
    {
        public string Icon { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public Brush IconColor { get; init; } = Brushes.Gray;
        public Brush TextColor { get; init; } = Brushes.Black;
    }

    public class TestResultToItemsConverter : IValueConverter
    {
        public static readonly TestResultToItemsConverter Instance = new();

        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is not ConnectionTestResult result)
                return Array.Empty<TestResultItem>();

            var items = new List<TestResultItem>
            {
                new()
                {
                    Icon = result.NetworkReachable ? "✓" : "✕",
                    Message = result.NetworkReachable ? "Bilgisayara ağ üzerinden erişiliyor" : "Bilgisayara ağ üzerinden ulaşılamıyor",
                    IconColor = result.NetworkReachable ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    TextColor = result.NetworkReachable ? new SolidColorBrush(Color.FromRgb(27, 94, 32)) : new SolidColorBrush(Color.FromRgb(183, 28, 28))
                },
                new()
                {
                    Icon = result.WmiAccessible ? "✓" : "✕",
                    Message = result.WmiAccessible ? "WMI erişimi başarılı" : "WMI erişimi başarısız",
                    IconColor = result.WmiAccessible ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    TextColor = result.WmiAccessible ? new SolidColorBrush(Color.FromRgb(27, 94, 32)) : new SolidColorBrush(Color.FromRgb(183, 28, 28))
                },
                new()
                {
                    Icon = result.AuthorizationSuccess ? "✓" : "✕",
                    Message = result.AuthorizationSuccess ? "Yetkilendirme başarılı" : "Yetkilendirme başarısız",
                    IconColor = result.AuthorizationSuccess ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    TextColor = result.AuthorizationSuccess ? new SolidColorBrush(Color.FromRgb(27, 94, 32)) : new SolidColorBrush(Color.FromRgb(183, 28, 28))
                }
            };

            return items;
        }

        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class ObjectToStringConverter : IValueConverter
    {
        public static readonly ObjectToStringConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value?.ToString() ?? string.Empty;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
