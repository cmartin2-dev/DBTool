using DocumentFormat.OpenXml.Spreadsheet;
using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DBTool.Commons
{
    public class IsNotNullConverter : IValueConverter
    {
        public static readonly IsNotNullConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isDark)
                return isDark ? Brushes.Green : Brushes.Red;

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IndexToHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
                return $"Query {index + 1}";
            return "Query ?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return 0.0;

            if (!double.TryParse(values[0]?.ToString(), out double value)) return 0.0;
            if (!double.TryParse(values[1]?.ToString(), out double maximum)) return 0.0;
            if (!double.TryParse(values[2]?.ToString(), out double actualWidth)) return 0.0;

            if (maximum <= 0) return 0.0;
            double ratio = Math.Max(0.0, Math.Min(1.0, value / maximum));
            // You may want to subtract padding/margins if your template has them
            return ratio * actualWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SecondsToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double seconds)
            {
                return TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
            }
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class AvalonEditWatermark
    {
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached(
                "Watermark",
                typeof(string),
                typeof(AvalonEditWatermark),
                new PropertyMetadata(null, OnWatermarkChanged));

        public static void SetWatermark(UIElement element, string value)
            => element.SetValue(WatermarkProperty, value);

        public static string GetWatermark(UIElement element)
            => (string)element.GetValue(WatermarkProperty);

        private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextEditor editor) return;

            // Ensure editor is loaded before adding adorner
            if (!editor.IsLoaded)
            {
                editor.Loaded += (s, ev) => ApplyWatermark(editor, e.NewValue as string);
            }
            else
            {
                ApplyWatermark(editor, e.NewValue as string);
            }
        }

        private static void ApplyWatermark(TextEditor editor, string watermarkText)
        {
            var layer = AdornerLayer.GetAdornerLayer(editor);
            if (layer == null) return;

            // Check if watermark already exists
            WatermarkAdorner existing = null;
            var adorners = layer.GetAdorners(editor);
            if (adorners != null)
            {
                foreach (var a in adorners)
                {
                    if (a is WatermarkAdorner wa)
                    {
                        existing = wa;
                        break;
                    }
                }
            }

            if (existing == null)
            {
                existing = new WatermarkAdorner(editor, watermarkText);
                layer.Add(existing);
            }
            else
            {
                existing.SetText(watermarkText);
            }

            // Update visibility initially
            existing.Visibility = string.IsNullOrEmpty(editor.Text) ? Visibility.Visible : Visibility.Collapsed;

            // Hook TextChanged
            editor.TextChanged -= Editor_TextChanged;
            editor.TextChanged += Editor_TextChanged;

            void Editor_TextChanged(object sender, EventArgs args)
            {
                existing.Visibility = string.IsNullOrEmpty(editor.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private class WatermarkAdorner : Adorner
        {
            private readonly TextBlock _textBlock;
            private readonly TextEditor _editor;

            public WatermarkAdorner(TextEditor editor, string text)
                : base(editor)
            {
                _editor = editor;
                _textBlock = new TextBlock
                {
                    Text = text ?? "",
                    Foreground = Brushes.Gray,
                    IsHitTestVisible = false
                };

                AddVisualChild(_textBlock);
            }

            public void SetText(string text) => _textBlock.Text = text ?? "";

            protected override int VisualChildrenCount => 1;
            protected override Visual GetVisualChild(int index) => _textBlock;

            protected override Size ArrangeOverride(Size finalSize)
            {
                if (_editor.TextArea != null)
                {
                    // Get the offset of the text area (skip line numbers margin)
                    var offset = _editor.TextArea.TextView.Margin; // left margin includes line numbers
                    _textBlock.Margin = new Thickness(offset.Left + 2, 2, 0, 0);
                }
                else
                {
                    _textBlock.Margin = new Thickness(4, 2, 0, 0);
                }

                _textBlock.Arrange(new Rect(finalSize));
                return finalSize;
            }
        }

       
    }


}
