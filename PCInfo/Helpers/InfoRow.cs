using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCNetworkInspector.Helpers
{
    /// <summary>
    /// A simple label-value row control for displaying info pairs.
    /// </summary>
    public class InfoRow : Control
    {
        static InfoRow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoRow),
                new FrameworkPropertyMetadata(typeof(InfoRow)));
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(InfoRow),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(InfoRow),
                new PropertyMetadata(null));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
