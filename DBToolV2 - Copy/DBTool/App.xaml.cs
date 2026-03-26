using DBTool.Commons;
using System.Collections;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace DBTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private bool isCell;
        private string selectedCellValue;

        private CancellationTokenSource _cts;
        public ObservableCollection<TabItem> MyTabs { get; }
            = new ObservableCollection<TabItem> ();

        // Close handler
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var mainParent = FindParent<TabControl>(btn);
                var childParen = FindParent<TabItem>(btn);

                if (mainParent.ItemsSource is IList source)
                {
                    source.Remove(childParen); // bound mode
                }

                int ctr = 1;
                foreach (var item in mainParent.ItemsSource)
                {
                    var tabItem = item as TabItem;
                    tabItem.Header = $"Query {ctr}";
                    ctr++;
                }
            }
        }


        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem btn)
            {
                MenuItem item = sender as MenuItem;

                if (item.Header.ToString().ToLower() == "copy value")
                {
                    Clipboard.SetText(selectedCellValue);
                }
            }
        }

        private void ListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if(!isCell)
            {
                e.Handled = true;
            }
            else
            {

            }
        }

        private void ListView_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListView listView = sender as ListView;

            TextBlock selectedCell = listView.InputHitTest(e.GetPosition(listView)) as TextBlock;
            if (selectedCell != null)
            {
                isCell = true;
                selectedCellValue = selectedCell.Text;
            }
            else
            {
                isCell = false;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var textBox = sender as TextBox;
            if (textBox == null) return;

            var listView = FindParent<ListView>(textBox);
            if (listView == null) return;

            var view = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            if (view == null) return;

            string filterText = textBox.Text?.Trim() ?? string.Empty;

            // Debounce filtering to avoid refreshing on every keystroke
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, token); // 300ms delay
                    if (token.IsCancellationRequested) return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Cache column property paths to avoid repeated reflection
                        var propertyPaths = new List<string>();
                        if (listView.View is GridView gridView)
                        {
                            foreach (var col in gridView.Columns)
                            {
                                if (col.DisplayMemberBinding is Binding binding)
                                    propertyPaths.Add(binding.Path.Path);
                            }
                        }

                        view.Filter = item =>
                        {
                            if (string.IsNullOrEmpty(filterText)) return true;

                            if (item is IDictionary<string, object> dict)
                            {
                                return dict.Values.Any(v =>
                                    v?.ToString().IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
                            }
                            else
                            {
                                foreach (var path in propertyPaths)
                                {
                                    var prop = item.GetType().GetProperty(path);
                                    if (prop != null)
                                    {
                                        var value = prop.GetValue(item)?.ToString() ?? string.Empty;
                                        if (value.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                                            return true;
                                    }
                                }
                            }

                            return false;
                        };

                        view.Refresh();
                    });
                }
                catch (TaskCanceledException) { }
            });
        }


        private void Export_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmExportExcel_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

