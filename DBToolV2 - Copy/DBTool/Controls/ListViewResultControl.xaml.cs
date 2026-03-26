using Connect;
using DBTool.Commons;
using DBTool.Connect;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Entities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Thickness = System.Windows.Thickness;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for ListViewResultControl.xaml
    /// </summary>
    public partial class ListViewResultControl : UserControl
    {
        public Dictionary<string, string> CustomColumns { get; set; }
        public ICollectionView collectionView;

        public string ControlTag = string.Empty;

        private DispatcherTimer _timer;
        private Stopwatch _stopwatch;
        private double elapsedSeconds;


        public static readonly RoutedEvent SelectionChangedEvent =
EventManager.RegisterRoutedEvent(
    "SelectionChangedExecute",
    RoutingStrategy.Bubble,
    typeof(SelectionChangedEventHandler),
    typeof(ListViewResultControl));

        public static readonly RoutedEvent Checkbox_CheckedEvent =
EventManager.RegisterRoutedEvent(
"Checkbox_Checked",
RoutingStrategy.Bubble,
typeof(RoutedEventHandler),
typeof(ListViewResultControl));

        public static readonly RoutedEvent UnCheckbox_CheckedEvent =
EventManager.RegisterRoutedEvent(
"UnCheckbox_Checked",
RoutingStrategy.Bubble,
typeof(RoutedEventHandler),
typeof(ListViewResultControl));

        public event EventHandler<DataGridCellEditEventArgs> CellEdited;

        public event RoutedEventHandler Checkbox_Checked
        {
            add { AddHandler(Checkbox_CheckedEvent, value); }
            remove { RemoveHandler(Checkbox_CheckedEvent, value); }
        }

        public event RoutedEventHandler UnCheckbox_Checked
        {
            add { AddHandler(UnCheckbox_CheckedEvent, value); }
            remove { RemoveHandler(UnCheckbox_CheckedEvent, value); }
        }

        public event SelectionChangedEventHandler SelectionChangedExecute
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Wrap args
            var args = new DataGridCellEditEventArgs(e.Row.Item, e.Column, e.EditingElement);
            CellEdited?.Invoke(this, args);
        }


        public void SetTag(string controlTag)
        {
            dataGrid1.Tag = controlTag;
        }

        public ListViewResultControl()
        {
            InitializeComponent();
            CustomColumns = new Dictionary<string, string>();
            //     loadingControl.lstViewResult = lstResult;

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer.Tick += _timer_Tick;

            dataGrid1.CellEditEnding += OnCellEditEnding;
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        private DataGridTemplateColumn CheckboxColumn(string bindingPath, RoutedEventHandler checkboxCheckHandler = null,
            RoutedEventHandler uncheckBoxUncheckHandler = null)
        {

            var headerCheckBox = new CheckBox();
            headerCheckBox.Checked += HeaderCheckBox_Checked;
            headerCheckBox.Unchecked += HeaderCheckBox_Unchecked;

            var checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
            checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding(bindingPath)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            // Hook events
            checkBoxFactory.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(checkboxCheckHandler));
            checkBoxFactory.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(uncheckBoxUncheckHandler));

            // Create DataTemplate
            var cellTemplate = new DataTemplate();
            cellTemplate.VisualTree = checkBoxFactory;

            // Create the TemplateColumn
            var templateColumn = new DataGridTemplateColumn
            {
                Header = headerCheckBox,
                CellTemplate = cellTemplate,

            };

            return templateColumn;
        }

        private void HeaderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var args = new RoutedEventArgs(ListViewResultControl.UnCheckbox_CheckedEvent, sender);
            RaiseEvent(args);

            //var checkbox = sender as CheckBox;
            //if (checkbox != null)
            //{
            //    foreach (var item in dataGrid1.Items)
            //    {
            //        if (item is RegionTenant regionTenant)
            //        {
            //            regionTenant.IsSelected = false;

            //            var args = new RoutedEventArgs(ListViewResultControl.Checkbox_CheckedEvent, sender);
            //            RaiseEvent(args);

            //        }
            //        //DataGridRow row = (DataGridRow)dataGrid1.ItemContainerGenerator.ContainerFromItem(item);
            //        //if (row != null)
            //        //{
            //        //    foreach (var column in dataGrid1.Columns)
            //        //    {
            //        //        if (column is DataGridTemplateColumn)
            //        //        {
            //        //            DataGridCellsPresenter presenter = Utilities.FindVisualChild<DataGridCellsPresenter>(row);
            //        //            DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(0);


            //        //            CheckBox chk = Utilities.FindVisualChild<CheckBox>(cell);

            //        //            if (chk != null)
            //        //            {
            //        //                // Do something with the CheckBox
            //        //                chk.IsChecked = false;  // example
            //        //            }
            //        //            break;
            //        //        }
            //        //    }
            //        //}
            //    }

            //    dataGrid1.Items.Refresh();
            //}
        }

        private void HeaderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var args = new RoutedEventArgs(ListViewResultControl.Checkbox_CheckedEvent, sender);
            RaiseEvent(args);
            //var checkbox = sender as CheckBox;
            //if (checkbox != null)
            //{
            //    foreach (var item in dataGrid1.Items)
            //    {
            //        if(item is RegionTenant regionTenant)
            //        {
            //            regionTenant.IsSelected = true;
            //            var args = new RoutedEventArgs(ListViewResultControl.UnCheckbox_CheckedEvent, regionTenant);
            //            RaiseEvent(args);
            //        }
            //        //DataGridRow row = (DataGridRow)dataGrid1.ItemContainerGenerator.ContainerFromItem(item);
            //        //if (row != null)
            //        //{
            //        //    foreach (var column in dataGrid1.Columns)
            //        //    {
            //        //        if (column is DataGridTemplateColumn)
            //        //        {
            //        //            DataGridCellsPresenter presenter = Utilities.FindVisualChild<DataGridCellsPresenter>(row);
            //        //            DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(0);


            //        //            CheckBox chk = Utilities.FindVisualChild<CheckBox>(cell);

            //        //            if (chk != null)
            //        //            {
            //        //                // Do something with the CheckBox
            //        //                chk.IsChecked = true;  // example
            //        //            }
            //        //            break;
            //        //        }
            //        //    }
            //        //}
            //    }

            //    dataGrid1.Items.Refresh();  
            //}
        }

        public void AddContextMenu()
        {
            var globalMenu = (ContextMenu)System.Windows.Application.Current.Resources["SharedListViewContextMenu"];
            var newMenu = Utilities.CloneContextMenu(globalMenu, dataGrid1);


            dataGrid1.ContextMenu = newMenu;
        }

        public void AddContextMenu(MenuItem menuItem = null)
        {
            var globalMenu = (ContextMenu)System.Windows.Application.Current.Resources["SharedListViewContextMenu"];
            var newMenu = Utilities.CloneContextMenu(globalMenu, dataGrid1);
            if (menuItem != null)
                newMenu.Items.Add(menuItem);

            dataGrid1.ContextMenu = newMenu;
        }

        public void AddContextMenu(List<MenuItem> menuItems = null)
        {
            var globalMenu = (ContextMenu)System.Windows.Application.Current.Resources["SharedListViewContextMenu"];
            var newMenu = Utilities.CloneContextMenu(globalMenu, dataGrid1);
            if (menuItems != null)
            {
                foreach (MenuItem menuItem in menuItems)
                    newMenu.Items.Add(menuItem);
            }
            dataGrid1.ContextMenu = newMenu;
        }


        private ObservableCollection<dynamic> ChangeToViewModel(List<IDictionary<string, object>> objItems)
        {
            ObservableCollection<dynamic> Items = null;

            if (objItems is List<IDictionary<string, object>> items)
            {
                Items = new ObservableCollection<dynamic>(items.Select(dict =>
                {
                    dynamic obj = new ExpandoObject();
                    var expandoDict = (IDictionary<string, object>)obj;

                    foreach (var kvp in dict)
                        expandoDict[kvp.Key] = kvp.Value;

                    return obj;
                })
                    );
            }

            return Items;
        }

        public void LoadData(CustObj obj, bool hasCheckbox = false, RoutedEventHandler checkboxCheckHandler = null,
            RoutedEventHandler uncheckBoxUncheckHandler = null)
        {
            dataGrid1.Columns.Clear();

            if (hasCheckbox)
            {

                for (int aa = 0; aa < obj.Objects.Count; aa++)
                {
                    obj.Objects[aa].Object["IsSelected"] = 0;
                }

                DataGridTemplateColumn dataGridTemplateColumn = CheckboxColumn("IsSelected", checkboxCheckHandler, uncheckBoxUncheckHandler);
                dataGrid1.Columns.Add(dataGridTemplateColumn);
            }

            dataGrid1.AddDataGridColumn(obj);

            var convertedObject = ChangeToViewModel(obj.Objects.Select(x => x.Object).ToList());


            dataGrid1.ItemsSource = convertedObject;// obj.Objects.Select(x => x.Object);

            lblTotalCount.Content = $"Total Count : {obj.Objects.Count()}";
        }

        public void LoadData(List<RegionTenant> regionTenants, RoutedEventHandler checkboxCheckHandler = null,
            RoutedEventHandler uncheckBoxUncheckHandler = null)
        {
            dataGrid1.Columns.Clear();



            foreach (var column in CustomColumns)
            {
                if (column.Value == "checkbox")
                {


                    DataGridTemplateColumn dataGridTemplateColumn = CheckboxColumn(column.Key, checkboxCheckHandler, uncheckBoxUncheckHandler);
                    dataGrid1.Columns.Add(dataGridTemplateColumn);
                    dataGridTemplateColumn.Visibility = Visibility.Collapsed;
                    continue;
                }

                DataGridTextColumn dataGridTextColumn = new DataGridTextColumn();
                dataGridTextColumn.Header = new ContentControl { Content = column.Value };
                dataGridTextColumn.Width = 200;

                dataGridTextColumn.IsReadOnly = true;
                dataGrid1.Columns.Add(dataGridTextColumn);

                dataGridTextColumn.Binding = new Binding($"{column.Key}");
            }

            ICollectionView collectionView = CollectionViewSource.GetDefaultView(regionTenants);

            dataGrid1.ItemsSource = null;

            Binding binding = new Binding
            {
                Source = collectionView,
            };

            dataGrid1.SetBinding(DataGrid.ItemsSourceProperty, binding);

            lblTotalCount.Content = $"Total Count : {regionTenants.Count}";
        }

        public void LoadData(ObservableCollection<Language> languages, RoutedEventHandler checkboxCheckHandler = null,
            RoutedEventHandler uncheckBoxUncheckHandler = null)
        {
            dataGrid1.Columns.Clear();



            foreach (var column in CustomColumns)
            {

                if (column.Value == "checkbox")
                {


                    DataGridTemplateColumn dataGridTemplateColumn = CheckboxColumn(column.Key, checkboxCheckHandler, uncheckBoxUncheckHandler);
                    dataGrid1.Columns.Add(dataGridTemplateColumn);
                    //  dataGridTemplateColumn.Visibility = Visibility.Collapsed;
                    continue;
                }

                DataGridTextColumn dataGridTextColumn = new DataGridTextColumn();
                dataGridTextColumn.Header = new ContentControl { Content = column.Value };
                dataGridTextColumn.Width = 75;

                dataGridTextColumn.IsReadOnly = true;
                dataGrid1.Columns.Add(dataGridTextColumn);

                dataGridTextColumn.Binding = new Binding($"{column.Key}");
            }

            ICollectionView collectionView = CollectionViewSource.GetDefaultView(languages);

            dataGrid1.ItemsSource = null;

            Binding binding = new Binding
            {
                Source = collectionView,
            };

            dataGrid1.SetBinding(DataGrid.ItemsSourceProperty, binding);

            lblTotalCount.Content = $"Total Count : {languages.Count}";
        }

        public void LoadData(List<CustomType> customTypes, RoutedEventHandler checkboxCheckHandler = null,
          RoutedEventHandler uncheckBoxUncheckHandler = null, int width = 75)
        {
            dataGrid1.Columns.Clear();



            foreach (var column in CustomColumns)
            {

                if (column.Value == "checkbox")
                {


                    DataGridTemplateColumn dataGridTemplateColumn = CheckboxColumn(column.Key, checkboxCheckHandler, uncheckBoxUncheckHandler);
                    dataGrid1.Columns.Add(dataGridTemplateColumn);
                    //  dataGridTemplateColumn.Visibility = Visibility.Collapsed;
                    continue;
                }

                DataGridTextColumn dataGridTextColumn = new DataGridTextColumn();
                dataGridTextColumn.Header = new ContentControl { Content = column.Value };
                dataGridTextColumn.Width = width;

                dataGridTextColumn.IsReadOnly = true;
                dataGrid1.Columns.Add(dataGridTextColumn);

                dataGridTextColumn.Binding = new Binding($"{column.Key}");
            }

            ICollectionView collectionView = CollectionViewSource.GetDefaultView(customTypes);

            dataGrid1.ItemsSource = null;

            Binding binding = new Binding
            {
                Source = collectionView,
            };

            dataGrid1.SetBinding(DataGrid.ItemsSourceProperty, binding);

            lblTotalCount.Content = $"Total Count : {customTypes.Count}";
        }

        public void LoadData(List<ERDObjectChecker> lstErdObjectCheckers, RoutedEventHandler checkboxCheckHandler = null,
         RoutedEventHandler uncheckBoxUncheckHandler = null, int width = 75)
        {
            dataGrid1.Columns.Clear();



            foreach (var column in CustomColumns)
            {

                if (column.Value == "checkbox")
                {


                    DataGridTemplateColumn dataGridTemplateColumn = CheckboxColumn(column.Key, checkboxCheckHandler, uncheckBoxUncheckHandler);
                    dataGrid1.Columns.Add(dataGridTemplateColumn);
                    //  dataGridTemplateColumn.Visibility = Visibility.Collapsed;
                    continue;
                }

                DataGridTextColumn dataGridTextColumn = new DataGridTextColumn();
                dataGridTextColumn.Header = new ContentControl { Content = column.Value };
                dataGridTextColumn.Width = width;

                dataGridTextColumn.IsReadOnly = true;
                dataGrid1.Columns.Add(dataGridTextColumn);

                dataGridTextColumn.Binding = new Binding($"{column.Key}");
            }

            collectionView = CollectionViewSource.GetDefaultView(lstErdObjectCheckers);

            dataGrid1.ItemsSource = null;

            Binding binding = new Binding
            {
                Source = collectionView,
            };

            dataGrid1.SetBinding(DataGrid.ItemsSourceProperty, binding);

            lblTotalCount.Content = $"Total Count : {lstErdObjectCheckers.Count}";
        }

        public void LoadData(List<QueryLog> queryLogs, RoutedEventHandler checkboxCheckHandler = null,
           RoutedEventHandler uncheckBoxUncheckHandler = null)
        {
            dataGrid1.Columns.Clear();



            foreach (var column in CustomColumns)
            {
                if (column.Value == "checkbox")
                {


                    DataGridTemplateColumn dataGridTemplateColumn = CheckboxColumn(column.Key, checkboxCheckHandler, uncheckBoxUncheckHandler);
                    dataGrid1.Columns.Add(dataGridTemplateColumn);
                    dataGridTemplateColumn.Visibility = Visibility.Collapsed;
                    continue;
                }

                DataGridTextColumn dataGridTextColumn = new DataGridTextColumn();
                dataGridTextColumn.Header = new ContentControl { Content = column.Value };
                dataGridTextColumn.Width = 200;

                dataGridTextColumn.IsReadOnly = true;
                dataGrid1.Columns.Add(dataGridTextColumn);

                dataGridTextColumn.Binding = new Binding($"{column.Key}");
            }

            ICollectionView collectionView = CollectionViewSource.GetDefaultView(queryLogs);

            dataGrid1.ItemsSource = null;

            Binding binding = new Binding
            {
                Source = collectionView,
            };

            dataGrid1.SetBinding(DataGrid.ItemsSourceProperty, binding);

            lblTotalCount.Content = $"Total Count : {queryLogs.Count}";
        }

        public void ShowCheckBox(Visibility visibility)
        {
            foreach (DataGridColumn column in dataGrid1.Columns)
            {
                if (column is DataGridTemplateColumn)
                    column.Visibility = visibility;
            }
        }
        private void dataGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var newArgs = new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems);
            RaiseEvent(newArgs);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid1.ItemsSource);
            if (view == null) return;

            view.Filter = item =>
            {
                if (item == null) return false;

                if (item is IDictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        var value = kvp.Value?.ToString();
                        if (!string.IsNullOrEmpty(value) &&
                            value.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    foreach (var prop in item.GetType().GetProperties())
                    {
                        var value = prop.GetValue(item)?.ToString();
                        if (!string.IsNullOrEmpty(value) && value.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }

                return string.IsNullOrEmpty(txtSearch.Text);
            };


            view.Refresh();
            lblTotalCount.Content = $"Total Count : {view.Cast<object>().Count()}";
        }

        private void dataGrid1_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column is DataGridCheckBoxColumn)
            {
                if (e.Row.Item is RegionTenant item)
                {
                    //Console.WriteLine($"Checkbox edited → {item.Name}, IsActive = {item.IsActive}");
                }
            }
        }

        public void ShowListView()
        {
            dataGrid1.Visibility = Visibility.Visible;
            txtSearch.Visibility = Visibility.Visible;
            if (loadingControl.btnExportToExcel != null)
                loadingControl.btnExportToExcel.Visibility = Visibility.Visible;
            if (loadingControl.btnExportToJson != null)
                loadingControl.btnExportToJson.Visibility = Visibility.Visible;
            if (loadingControl.lblTotalCount != null)
                lblTotalCount.Visibility = Visibility.Visible;
            loadingControl.Errorborder.Visibility = Visibility.Collapsed;
            loadingControl.Doneborder.Visibility = Visibility.Collapsed;

            loadingControl.loadingborder.Visibility = Visibility.Collapsed;
            lblProgress.Visibility = Visibility.Visible;
        }

        public void ShowLoading()
        {

            loadingControl.loadingborder.Visibility = Visibility.Visible;

            dataGrid1.Visibility = Visibility.Collapsed;
            txtSearch.Visibility = Visibility.Collapsed;
            if (loadingControl.btnExportToExcel != null)
                loadingControl.btnExportToExcel.Visibility = Visibility.Collapsed;
            if (loadingControl.btnExportToJson != null)
                loadingControl.btnExportToJson.Visibility = Visibility.Collapsed;
            if (lblTotalCount != null)
                lblTotalCount.Visibility = Visibility.Collapsed;
            loadingControl.Errorborder.Visibility = Visibility.Collapsed;
            loadingControl.Doneborder.Visibility = Visibility.Collapsed;
            lblProgress.Visibility = Visibility.Visible;
        }

        public void ShowError(string errorMsg = "")
        {
            loadingControl.loadingborder.Visibility = Visibility.Collapsed;

            dataGrid1.Visibility = Visibility.Collapsed;

            txtSearch.Visibility = Visibility.Collapsed;
            if (loadingControl.btnExportToExcel != null)
                loadingControl.btnExportToExcel.Visibility = Visibility.Collapsed;
            if (loadingControl.btnExportToJson != null)
                loadingControl.btnExportToJson.Visibility = Visibility.Collapsed;
            if (lblTotalCount != null)
                lblTotalCount.Visibility = Visibility.Collapsed;
            loadingControl.Errorborder.Visibility = Visibility.Visible;
            loadingControl.Doneborder.Visibility = Visibility.Collapsed;

            loadingControl.txtErrorName.Visibility = Visibility.Visible;
            lblProgress.Visibility = Visibility.Collapsed;

        }

        public void ShowDone()
        {
            loadingControl.loadingborder.Visibility = Visibility.Collapsed;

            dataGrid1.Visibility = Visibility.Collapsed;
            txtSearch.Visibility = Visibility.Collapsed;
            if (loadingControl.btnExportToExcel != null)
                loadingControl.btnExportToExcel.Visibility = Visibility.Collapsed;
            if (loadingControl.btnExportToJson != null)
                loadingControl.btnExportToJson.Visibility = Visibility.Collapsed;
            if (lblTotalCount != null)
                lblTotalCount.Visibility = Visibility.Collapsed;
            loadingControl.Errorborder.Visibility = Visibility.Collapsed;
            loadingControl.Doneborder.Visibility = Visibility.Visible;

            lblProgress.Visibility = Visibility.Collapsed;
        }

        public void HideListView(bool hide = true)
        {
            dataGrid1.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            txtSearch.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            // lstViewResult.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            if (loadingControl.btnExportToExcel != null)
                loadingControl.btnExportToExcel.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            if (loadingControl.btnExportToJson != null)
                loadingControl.btnExportToJson.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            if (lblTotalCount != null)
                lblTotalCount.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            lblProgress.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
        }

        public void ClearItems()
        {
            dataGrid1.Columns.Clear();
            dataGrid1.ItemsSource = null;
        }

        public virtual async Task<CustObj> Execute(ExcelConnect excelConnect)
        {
            Task<CustObj> task = null;
            try
            {
                _timer.IsEnabled = true;
                _timer.Start();
                _stopwatch.Restart();
                _stopwatch.Start();

                task = excelConnect.ExecuteQuery();
                await task;


                _timer.Stop();
                _stopwatch.Stop();

                return task.Result;
            }
            catch
            {
                _timer.Stop();
                _stopwatch.Stop();
                return null;
            }

        }

        public virtual async Task<RequestResponse> Execute(RequestQuery requestQuery)
        {
            Task<RequestResponse> task = null;
            try
            {
                _timer.IsEnabled = true;
                _timer.Start();
                _stopwatch.Restart();
                _stopwatch.Start();

                task = requestQuery.GetRequestQuery();
                await task;

                if (task != null)
                {
                    RequestResponse _response = task.Result;
                    if (_response.isSuccess)
                    {
                        lblProgress.Content = $"Status: Done - {elapsedSeconds}";
                    }
                    else
                    {
                        if (_response.ErrorMessage.ToLower().Contains("cancel"))
                        {
                            lblProgress.Content = $"Status: Cancelled - {elapsedSeconds}";
                            loadingControl.txtErrorName.Content = "Cancelled";

                        }
                        else
                        {
                            lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                            loadingControl.txtErrorName.Content = "Error";
                            loadingControl.txtErrorDetail.Text = _response.ErrorMessage;

                        }
                        ShowError();
                    }
                }
                else
                {
                    lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                    ShowError();
                    loadingControl.txtErrorName.Content = "Error";
                }


                _timer.Stop();
                _stopwatch.Stop();

                return task.Result;
            }
            catch
            {
                _timer.Stop();
                _stopwatch.Stop();
                return null;
            }

        }

    }

    public class DataGridCellEditEventArgs : EventArgs
    {
        public object RowItem { get; }
        public DataGridColumn Column { get; }
        public FrameworkElement EditingElement { get; }

        public DataGridCellEditEventArgs(object rowItem, DataGridColumn column, FrameworkElement editingElement)
        {
            RowItem = rowItem;
            Column = column;
            EditingElement = editingElement;
        }
    }
}
