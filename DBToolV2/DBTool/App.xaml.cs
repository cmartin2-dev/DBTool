using System.Collections;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Controls;
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
    }
}

