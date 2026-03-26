using Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace DBTool.Commons
{
    public static class ControlExtensions
    {

        public static void GenerateListView(this ListView listView, string[] columnNames,
            string[] bindingNames, int[] widths, bool hasCheckbox = false, CheckBox checkbox = null,
            RoutedEventHandler checkboxCheckHandler = null, RoutedEventHandler heckBoxUncheckHandler = null)
        {
            listView.View = new GridView();

            if (hasCheckbox)
            {

                var extraColumn = new GridViewColumn
                {

                    Header = new ContentControl
                    {
                        Content = checkbox,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    Width = 0,
                    CellTemplate = CreateCheckBoxTemplate(checkboxCheckHandler, heckBoxUncheckHandler),
                };
                (listView.View as GridView).Columns.Add(extraColumn);
            }

            for (int i = 0; i < columnNames.Length; i++)
            {
                string columnName = columnNames[i];
                string bindingName = bindingNames[i];

                GridViewColumn gridViewIdColumn = new GridViewColumn();
                gridViewIdColumn.Header = new ContentControl
                {
                    Content = columnName,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                gridViewIdColumn.DisplayMemberBinding = new Binding(bindingName);

                (listView.View as GridView).Columns.Add(gridViewIdColumn);
            }
        }

        public static void AddDataGridColumn(this DataGrid dataGrid, CustObj obj)
        {
            DataGridLength dataGridLength = new DataGridLength(1,DataGridLengthUnitType.SizeToHeader);

            dataGrid.ColumnWidth = dataGridLength;


            var items = obj.Objects.Select(x => x.Object);

            for (int aa = 0; aa < obj.Objects.Count; aa++)
            {
                obj.Objects[aa].Object["RowNumber"] = aa + 1;
            }

           dataGrid.Columns.Add(new DataGridTextColumn
           {
                Header = new ContentControl
                {
                    Content = "#",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width= 20
                },
                Binding = new Binding("RowNumber"),
            });

            int i = 0;
            foreach (var itemColumn in obj.Fields)
            {
                string columnName = itemColumn.Key;
                DataGridTextColumn dataGridTextColumn = new DataGridTextColumn();

                dataGridTextColumn.Header = new ContentControl
                {
                    Content = columnName

                };

                dataGridTextColumn.Binding = new Binding($"{columnName}");

                dataGrid.Columns.Add(dataGridTextColumn);
                i++;
            }

        }

        public static void ShowCheckBoxColumn(this ListView listView, bool isColumnVisible = true)
        {
            var gridView = listView.View as GridView;
            if (gridView != null)
            {
                if (!isColumnVisible)
                {
                    // Option 1: Set width to 0 (hides visually)
                    gridView.Columns[0].Width = 0;

                    // Option 2: Remove column completely
                    // gridView.Columns.Remove(checkBoxColumn);
                }
                else
                {
                    // Option 1: Restore width
                    gridView.Columns[0].Width = 35;

                    // Option 2: Add column back
                    // gridView.Columns.Add(checkBoxColumn);
                }
            }
        }
        public static void GenerateListView(this ListView listView, CustObj obj, int width = 150,
            bool hasCheckbox = false, CheckBox checkbox = null,
            RoutedEventHandler checkboxCheckHandler = null, RoutedEventHandler heckBoxUncheckHandler = null)
        {
            listView.View = new GridView();


            if (hasCheckbox)
            {

                var extraColumn = new GridViewColumn
                {

                    Header = new ContentControl
                    {
                        Content = checkbox,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    Width = 0,
                    CellTemplate = CreateCheckBoxTemplate(checkboxCheckHandler, heckBoxUncheckHandler),
                };

                (listView.View as GridView).Columns.Add(extraColumn);
            }

            var items = obj.Objects.Select(x => x.Object);

            for (int aa = 0; aa < obj.Objects.Count; aa++)
            {
                obj.Objects[aa].Object["RowNumber"] = (aa + 1).ToString();
            }


            //  (listView.View as GridView).Columns.Add(CreateColumn("#", $"[RowNumber]", 150));
            (listView.View as GridView).Columns.Add(new GridViewColumn
            {
                Header = new ContentControl
                {
                    Content = "#",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                DisplayMemberBinding = new Binding("[RowNumber]"),
                CellTemplate = CreateCellTemplate(0)
            });


            int i = 0;
            foreach (var itemColumn in obj.Fields)
            {
                string columnName = itemColumn.Key;
                GridViewColumn gridViewIdColumn = new GridViewColumn();
                gridViewIdColumn.Header =
                    new ContentControl
                    {
                        Content = columnName,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                gridViewIdColumn.Width = double.NaN;
                gridViewIdColumn.CellTemplate = CreateCellTemplate(i);


                gridViewIdColumn.DisplayMemberBinding = new Binding($"[{columnName}]");

                //DataTemplate headerTemplate = new DataTemplate();
                //FrameworkElementFactory tb = new FrameworkElementFactory(typeof(TextBlock));
                //tb.SetBinding(TextBlock.TextProperty, new Binding());
                //tb.SetValue(TextBlock.XmlSpaceProperty, "preserve");
                //headerTemplate.VisualTree = tb;

                //gridViewIdColumn.HeaderTemplate = headerTemplate;

                (listView.View as GridView).Columns.Add(gridViewIdColumn);


                // (listView.View as GridView).Columns.Add(CreateColumn(columnName, $"[{columnName}]", 150));
                i++;
            }

            //Binding itemSource = new Binding
            //{
            //    Source = obj.Objects.Select(x => x.Object)
            //};

            ICollectionView listCollectionView;

            listCollectionView = CollectionViewSource.GetDefaultView(obj.Objects.Select(x => x.Object));
            listView.ItemsSource = null;
            listView.ItemsSource = listCollectionView;

            // listView.SetBinding(ListView.ItemsSourceProperty, itemSource);

            foreach (var objITem in listView.Items)
            {
                var item = listView.ItemContainerGenerator.ContainerFromItem(objITem) as ListViewItem;
                //if (item != null)
                //{
                //    item.Tag = "Some value"; // assign anything
                //}
            }
        }

        private static DataTemplate CreateCellTemplate(int index)
        {
            string xaml =
                $@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                    <Border BorderBrush='Black' BorderThickness='0,0,1,0' HorizontalAlignment='Stretch'>
                        <TextBlock Text='{{Binding [{index}]}}' TextWrapping='NoWrap'   HorizontalAlignment='Stretch'/>
                    </Border>
               </DataTemplate>";

            return (DataTemplate)XamlReader.Parse(xaml);
        }

        private static DataTemplate CreateCheckBoxTemplate(RoutedEventHandler checkBoxCheck, RoutedEventHandler checkBoxUncheck)
        {
            // Create a DataTemplate with a CheckBox
            var template = new DataTemplate();

            var factory = new FrameworkElementFactory(typeof(CheckBox));
            factory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            var binding = new Binding("IsSelected")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            factory.SetBinding(CheckBox.IsCheckedProperty, binding);

            // Optional: handle Checked/Unchecked in code
            factory.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(checkBoxCheck));
            factory.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(checkBoxUncheck));

            template.VisualTree = factory;
            return template;
        }

        private static void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb != null)
            {
            }
        }

        private static void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;

        }


    }

    public static class DictionaryExtensions
    {
        public static ObservableCollection<KeyValuePair<TKey, TValue>> ToObservableCollection<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary)
        {
            return new ObservableCollection<KeyValuePair<TKey, TValue>>(dictionary.ToList());
        }
    }
}
