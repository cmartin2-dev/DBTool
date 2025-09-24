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
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var listView = FindParent<ListView>(textBox);
            if (listView == null) return;

            var view = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            if (view == null) return;

            string filterText = textBox.Text?.Trim() ?? string.Empty;

            view.Filter = item =>
            {
                if (string.IsNullOrEmpty(filterText))
                    return true;

                // Look at all GridViewColumns
                if (listView.View is GridView gridView)
                {
                    foreach (var col in gridView.Columns)
                    {
                        // Works only if using DisplayMemberBinding
                        if (col.DisplayMemberBinding is Binding binding)
                        {
                            if (item is IDictionary<string, object> dict)
                            {
                                foreach (var value in dict.Values)
                                {
                                    var text = value?.ToString() ?? string.Empty;
                                    if (text.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                                        return true;
                                }
                            }
                            else
                            {

                                var prop = item.GetType().GetProperty(binding.Path.Path);
                                if (prop != null)
                                {
                                    var value = prop.GetValue(item)?.ToString() ?? string.Empty;
                                    if (value.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                                        return true;
                                }
                            }
                        }
                    }
                }

                return false;
            };

            view.Refresh();
        }
    }
}

