using DBTool.Commons;
using DBTool.Connect;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Binding = System.Windows.Data.Binding;
using Label = System.Windows.Controls.Label;
using ListView = System.Windows.Controls.ListView;
using TabControl = System.Windows.Controls.TabControl;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for EnvironmentControl.xaml
    /// </summary>
    public partial class EnvironmentControl : System.Windows.Controls.UserControl
    {
        public static readonly RoutedCommand CreateTabCommand = new RoutedCommand();

        private DispatcherTimer _timer;
        private Stopwatch _stopwatch;
        private double elapsedSeconds;

        private ObservableCollection<TabItem> queryTabs;

        private RegionTenant selectedRegionTenant = new RegionTenant();
        TabItem selectedTab;

        private System.Windows.Controls.CheckBox headerCheckBox;

        private Label lblProgress;

        private ListView lstViewEnvironment;

        private DataGrid gridEnvironment;

        private ICollectionView environmentCollectionView;
        public EnvironmentControl()
        {
            InitializeComponent();
            queryTabs = new ObservableCollection<TabItem>();


            System.Windows.Data.Binding binding = new System.Windows.Data.Binding
            {
                Source = queryTabs
            };

            MainTabQueryControl.SetBinding(System.Windows.Controls.TabControl.ItemsSourceProperty, binding);

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer.Tick += _timer_Tick;

            this.PreviewKeyDown += EnvironmentControl_PreviewKeyDown;

            lblProgress = lstResult.lblProgress;
            gridEnvironment = lstResult.dataGrid1;
            gridEnvironment.SelectionMode = DataGridSelectionMode.Single;
            //   lstViewEnvironment = lstResult.lstResult;

            lstResult.HideListView();

            GetRegions();
            CreateTab();
            AddQueryListContextMenu();

            dropVersionControl.DataContext = selectedRegionTenant;
            featureToggleControl.DataContext = selectedRegionTenant;
            extractTableControl.DataContext = selectedRegionTenant;

        }




        public void SetDataContext()
        {

            this.DataContext = StaticFunctions.AppConnection.settingsObject;
        }

        private void EnvironmentControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.T)
            {
                // Execute the command
                CreateTabCommand.Execute(null, this);
                e.Handled = true; // prevent further bubbling
            }
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        #region codes


        private void GetRegions()
        {


            cmbRegion.DisplayMemberPath = "RegionName";
            cmbRegion.SelectedValuePath = "Id";

            System.Windows.Data.Binding binding = new System.Windows.Data.Binding
            {
                Source = StaticFunctions.AppConnection.settingsObject.Regions
            };

            cmbRegion.SetBinding(System.Windows.Controls.ComboBox.ItemsSourceProperty, binding);
        }

        private async void cmbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Entities.Region item = cmbRegion.SelectedItem as Entities.Region;
            if (item != null)
            {
                cmbRegion.IsEnabled = false;

                lstResult.SetTag(item.RegionName);

                queryPerSchemaControl.ClearQueryPerSchema();
                //   lstViewEnvironment.Tag = item.RegionName;
                lstResult.ShowLoading();

                _timer.Start();
                _stopwatch.Restart();
                _stopwatch.Start();
                RequestQuery requestQuery = new RequestQuery();
                var selectedHeaderEnvironment = (this.DataContext as SettingsObject).Headers.FirstOrDefault(x => x.Id == item.HeaderEnvironmentId);

                if (selectedHeaderEnvironment != null)
                {

                    lstResult.loadingControl.lblTotalCount = new Label();
                    requestQuery.SetDetails(item, selectedHeaderEnvironment);
                    Task<RequestResponse> task = requestQuery.GetRequestEnvironment();

                    await task;

                    _timer.Stop();
                    _stopwatch.Stop();
                    if (task != null)
                    {
                        RequestResponse _response = task.Result;
                        if (_response.isSuccess)
                        {
                            List<RegionTenant> tenantList = _response.TenantList as List<RegionTenant>;

                            tenantList.ForEach(x =>
                            {
                                x.Region = item;
                                var tenant = StaticFunctions.GetTenants().FirstOrDefault(y => y.TenantId == x.tenantId)?.TenantName;
                                x.TenantName = tenant == string.Empty ? "" : tenant;
                            });

                            string[] ColumnNames = { "", "Name" };
                            string[] bindingNames = { "tenantId" };

                            lstResult.CustomColumns.Clear();


                            lstResult.CustomColumns.Add("IsSelected", "checkbox");
                            lstResult.CustomColumns.Add("tenantId", "Tenant Id");

                            lstResult.CustomColumns.Add("TenantName", "Tenant Name");
                            lstResult.LoadData(tenantList, rowCheckBox_Checked, rowCheckBox_Unchecked);

                            if (selectedTab.Name.ToLower() == "queryperschematab")
                                lstResult.ShowCheckBox(Visibility.Visible);

                            //environmentCollectionView = CollectionViewSource.GetDefaultView(tenantList);
                            //lstViewEnvironment.ItemsSource = null;
                            //lstViewEnvironment.ItemsSource = environmentCollectionView;
                            //GenerateColumns();


                            lblProgress.Content = $"Status: Done - {elapsedSeconds}";
                            lstResult.ShowListView();
                        }
                        else
                        {
                            lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                            lstResult.ShowError();
                        }
                    }
                    else
                    {
                        lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                        lstResult.ShowError();
                    }
                }
            }
            else
            {
                lstResult.ClearItems();
                queryPerSchemaControl.ClearQueryPerSchema();
                lblProgress.Content = "Status: Idle";
            }

            cmbRegion.IsEnabled = true;
        }

        private void GenerateColumns()
        {

            string[] ColumnNames = { "Name" };
            string[] bindingNames = { "tenantId" };
            int[] widths = { 250 };




            headerCheckBox = new System.Windows.Controls.CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            };

            headerCheckBox.Checked += HeaderCheckBox_Checked;
            headerCheckBox.Unchecked += HeaderCheckBox_Unchecked;

            lstViewEnvironment.GenerateListView(ColumnNames, bindingNames, widths,
                true, headerCheckBox, rowCheckBox_Checked, rowCheckBox_Unchecked);
            if (selectedTab.Name.ToLower() == "queryperschematab")
                lstViewEnvironment.ShowCheckBoxColumn(true);
        }

        private void rowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                RegionTenant tenant = checkBox.DataContext as RegionTenant;
                if (tenant != null)
                {
                    RemoveQueryPerSchemaTab(tenant);
                }
            }
        }

        private void rowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                RegionTenant regionTenant = checkBox.DataContext as RegionTenant;
                if (regionTenant != null)
                {
                    regionTenant.Region = cmbRegion.SelectedItem as Region;
                    RunRowCheckedEvent(regionTenant);
                }
            }
        }

        private void RunRowCheckedEvent(RegionTenant regionTenant)
        {
            Utilities.CheckAccess(regionTenant);
            AddQueryPerSchemaTab(regionTenant);
        }

        private void RunRowUncheckedEvent(RegionTenant regionTenant)
        {
            RemoveQueryPerSchemaTab(regionTenant);
        }

        private void AddQueryPerSchemaTab(RegionTenant regionTenant)
        {
            queryPerSchemaControl.AddTab(regionTenant);//.schemaTab.Items.Add(tabItem);
        }

        private void RemoveQueryPerSchemaTab(RegionTenant tenant)
        {
            queryPerSchemaControl.RemoveTab(tenant);
        }

        private void SetAllRowCheckBoxes(bool isChecked)
        {
            foreach (var item in lstResult.dataGrid1.Items)
            {
                if (item is RegionTenant regionTenant)
                {
                    if (regionTenant.IsSelected != isChecked)
                    {
                        regionTenant.IsSelected = isChecked;
                        if (isChecked)
                            RunRowCheckedEvent(regionTenant);
                        else
                            RunRowUncheckedEvent(regionTenant);
                    }
                }
            }
            //foreach (var item in lstViewEnvironment.Items)
            //{
            //    var container = lstViewEnvironment.ItemContainerGenerator.ContainerFromItem(item) as System.Windows.Controls.ListViewItem;
            //    if (container != null)
            //    {
            //        var cb = FindVisualChild<System.Windows.Controls.CheckBox>(container);
            //        if (cb != null)
            //            cb.IsChecked = isChecked;
            //    }
            //}
            lstResult.dataGrid1.Items.Refresh();
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    return t;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void HeaderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAllRowCheckBoxes(false);
        }

        private void HeaderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetAllRowCheckBoxes(true);
        }

        private void ScrollViewer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.T && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                CreateTab();
            }
        }


        private void CreateTab()
        {
            if (selectedRegionTenant == null)
                return;

            QueryControl queryControl = new QueryControl();

            TabItem tab = new TabItem();
            tab.Header = $"Query {queryTabs.Count + 1}";
            tab.Content = queryControl;

            var roundedStyle = (Style)System.Windows.Application.Current.Resources["ClosableTabItem"];
            tab.Style = roundedStyle;
            tab.IsSelected = true;
            selectedRegionTenant.Region = cmbRegion.SelectedItem as Region;

            tab.DataContext = selectedRegionTenant;



            queryTabs.Add(tab);

        }

        public void CreateTabWithQuery(string query)
        {
            // Create a query tab even without a selected tenant
            QueryControl queryControl = new QueryControl();

            TabItem tab = new TabItem();
            tab.Header = $"Query {queryTabs.Count + 1}";
            tab.Content = queryControl;

            var roundedStyle = (Style)System.Windows.Application.Current.Resources["ClosableTabItem"];
            tab.Style = roundedStyle;
            tab.IsSelected = true;

            if (selectedRegionTenant != null)
            {
                if (cmbRegion.SelectedItem is Region region)
                    selectedRegionTenant.Region = region;
                tab.DataContext = selectedRegionTenant;
            }

            queryTabs.Add(tab);

            // Set the query text after the control is loaded
            queryControl.Loaded += (s, e) =>
            {
                queryControl.txtQueryControl.txtQuery.Text = query;
            };

            // Switch to Query tab
            tabMainControl.SelectedIndex = 0;
        }

        private void lstViewEnvironment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = gridEnvironment.SelectedItem;

            selectedRegionTenant = item as RegionTenant;
            if (selectedRegionTenant != null)
            {
                selectedRegionTenant.Region = cmbRegion.SelectedItem as Region;

                System.Windows.Controls.UserControl selectedTab = MainTabQueryControl.SelectedContent as System.Windows.Controls.UserControl;
                if (selectedTab != null)
                {
                    selectedTab.DataContext = selectedRegionTenant;
                }
                featureToggleControl.DataContext = selectedRegionTenant;
                dropVersionControl.DataContext = selectedRegionTenant;
                extractTableControl.DataContext = selectedRegionTenant;
                upgradeControl.DataContext = selectedRegionTenant;
                customControl.DataContext = selectedRegionTenant;

                Utilities.CheckAccess(selectedRegionTenant);
            }
        }

        #endregion

        private void CreateTabCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CreateTab();
        }



        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedTab = tabMainControl.SelectedItem as TabItem;

            if (selectedTab != null)
            {

                //clearing queryperschematab
                var item = tabMainControl.Items
                       .OfType<TabItem>()
                       .FirstOrDefault(t => t.Content is QueryPerSchemaControl);

                //    lstViewEnvironment.ShowCheckBoxColumn(false);

                lstResult.ShowCheckBox(Visibility.Collapsed);

                if (selectedTab.Name.ToLower() == "queryperschematab")
                {
                    lstResult.ShowCheckBox(Visibility.Visible);
                    //   lstViewEnvironment.ShowCheckBoxColumn(true);
                }
                else if (selectedTab.Name.ToLower() == "querytab")
                {
                    TabControl queryTabmain = selectedTab.Content as TabControl;
                    if (queryTabmain != null)
                    {
                        var selectedQueryItem = queryTabmain.SelectedContent;
                        if (selectedQueryItem != null)
                        {
                            QueryControl selectedTabItem = selectedQueryItem as QueryControl;
                            if (selectedTabItem != null)
                            {
                                RegionTenant tabRegionTenant = selectedTabItem.DataContext as RegionTenant;
                                Utilities.CheckAccess(tabRegionTenant);
                            }

                        }
                    }
                }
            }
        }

        private void AddQueryListContextMenu()
        {
            var cmQueryList = new MenuItem { Header = "Query" };

            var cvs = new CollectionViewSource { Source = StaticFunctions.AppConnection.settingsObject.Queries };
            var FilteredQueries = cvs.View;

            Binding menuItems = new Binding
            {
                Source = FilteredQueries
            };

            FilteredQueries.Filter = item =>
            {
                var query = item as Entities.Query;
                return query != null && !query.IsPrivate && !query.IsUser;
            };

            cmQueryList.ItemContainerStyle = new Style(typeof(MenuItem))
            {
                Setters = {
                    new Setter(MenuItem.HeaderProperty, new Binding("Name")) ,


                }

            };

            cmQueryList.SetBinding(MenuItem.ItemsSourceProperty, menuItems);

            cmQueryList.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(SubmenuItem_Click));

            var generateODataSwagger = new MenuItem { Header = "Generate Odata Swagger" };

            List<MenuItem> menuItemLst = new List<MenuItem>();

            menuItemLst.Add(cmQueryList);
            menuItemLst.Add(generateODataSwagger);

            lstResult.AddContextMenu(menuItemLst);

        }

        private void SubmenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menuItem)
            {
                var clickedItem = menuItem.DataContext;

                // If you know it has "Name" property
                var queryString = (clickedItem as Entities.Query).QueryString;

                if (selectedTab != null && selectedTab.Name.ToLower() == "querytab")
                {
                    var item = selectedTab.Content as TabControl;
                    if (item != null)
                    {
                        TabItem tabItem = item.SelectedItem as TabItem;
                        if (tabItem != null)
                        {
                            QueryControl selectedQueryControl = tabItem.Content as QueryControl;
                            if (selectedQueryControl != null)
                            {
                                selectedQueryControl.txtQueryControl.txtQuery.Text = queryString;
                            }
                        }

                    }
                }
            }
        }

        private void lstResult_Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in lstResult.dataGrid1.ItemsSource)
            {
                var prop = item.GetType().GetProperty("IsSelected");
                if (prop != null)
                {
                    prop.SetValue(item, true, null);
                }
            }

            lstResult.dataGrid1.Items.Refresh();
        }

        private void lstResult_UnCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in lstResult.dataGrid1.ItemsSource)
            {
                var prop = item.GetType().GetProperty("IsSelected");
                if (prop != null)
                {
                    prop.SetValue(item, false, null);
                    RemoveQueryPerSchemaTab(item as RegionTenant);
                }
            }

            lstResult.dataGrid1.Items.Refresh();
        }

        private void tabMainControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt) && e.Key == Key.H)
            {
                if (CustomCSTab.Visibility == Visibility.Hidden)
                {
                    CustomCSTab.Visibility = Visibility.Visible;
                }
                else
                {
                    CustomCSTab.Visibility = Visibility.Hidden;
                }
            }
        }

        private bool _isLeftPanelCollapsed = false;
        private GridLength _savedLeftPanelWidth = new GridLength(300);

        private void BtnTogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (_isLeftPanelCollapsed)
            {
                // Expand
                LeftPanelColumn.MinWidth = 180;
                LeftPanelColumn.Width = _savedLeftPanelWidth;
                LeftPanelBorder.Visibility = Visibility.Visible;
                var path = FindToggleArrow();
                if (path != null) path.Data = System.Windows.Media.Geometry.Parse("M8,0 L0,6 L8,12");
                _isLeftPanelCollapsed = false;
            }
            else
            {
                // Collapse — save current actual width
                _savedLeftPanelWidth = new GridLength(LeftPanelColumn.ActualWidth);
                LeftPanelColumn.MinWidth = 0;
                LeftPanelColumn.Width = new GridLength(0);
                LeftPanelBorder.Visibility = Visibility.Collapsed;
                var path = FindToggleArrow();
                if (path != null) path.Data = System.Windows.Media.Geometry.Parse("M0,0 L8,6 L0,12");
                _isLeftPanelCollapsed = true;
            }
        }

        private System.Windows.Shapes.Path? FindToggleArrow()
        {
            var template = btnTogglePanel.Template;
            return template?.FindName("toggleArrow", btnTogglePanel) as System.Windows.Shapes.Path;
        }
    }
}
