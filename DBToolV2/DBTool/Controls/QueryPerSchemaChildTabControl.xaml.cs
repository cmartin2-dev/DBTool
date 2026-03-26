using DBTool.Commons;
using DBTool.Connect;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Task = System.Threading.Tasks.Task;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for QueryPerSchemaChildTabControl.xaml
    /// </summary>
    public partial class QueryPerSchemaChildTabControl : UserControl
    {
        DataGrid lstViewResult;
        LoadingControl loadingControl;
        Label lblProgress;


        private DispatcherTimer _timer;
        private Stopwatch _stopwatch;
        private double elapsedSeconds;

        public List<string> SelectedSchemas { get; set; }
        public QueryPerSchemaChildTabControl()
        {
            InitializeComponent();

           lstViewResult = lstResult.dataGrid1;
            loadingControl = lstResult.loadingControl;
            lblProgress = lstResult.lblProgress;

            SelectedSchemas = new List<string>();
            AddLstViewContextMenu();

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer.Tick += _timer_Tick;
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        public async Task FetchDetail(RegionTenant _regionTenant)
        {
            string _selectedTenant = _regionTenant.tenantId;
            await TryFetchSchema(_regionTenant, lstViewResult);
        }

        private async Task TryFetchSchema(RegionTenant regionTenant, DataGrid listView)// System.Windows.Controls.ListView listView)
        {
            try
            {
                //  if (!isFetchSchemaRunning)
                //   {
                //btnCheck.Text = "Check All";
                //btnFetch.Text = "Cancel";
                //isFetchSchemaRunning = true;
                lstResult.ShowLoading();
                var task = FetchSchema(regionTenant, listView);
                    //   }
                //    else
                //    {
                //if (task != null)
                //{
                //    sourceToken.Cancel();
                //    isFetchSchemaRunning = false;
                //    tsStat.Text = $"Status : Cancelled - Execution Time : {stopwatch.Elapsed.TotalSeconds}";

                //    btnFetch.Text = "Fetch Schema";
                //}
                //    }
            }
            catch { }
        }

        private async Task FetchSchema(RegionTenant regionTenant, DataGrid listView)// ListView listView)
        {
            //timer1.Enabled = true;
            //stopwatch.Restart();
            //stopwatch.Start();

            //sourceToken = new CancellationTokenSource();
            //token = sourceToken.Token;

            string query = @"SELECT 0 AS 'DATASCHEMASID','SCAH' as 'MODULECODE','SCAH' AS 'NAME', 'SCAH' AS 'SCHEMANAME', '' AS 'DBVERSION' UNION SELECT DATASCHEMASID,MODULECODE,NAME,SCHEMANAME,DBVERSION FROM SCAH.DATASCHEMAS WHERE ISREADY = 1";

            RequestQuery requestQuery = new RequestQuery();
            requestQuery.Query = query;
            requestQuery.SetDetails(regionTenant);
            //requestQuery.sourceToken = sourceToken;
            //requestQuery.token = token;
            _timer.Start();
            _stopwatch.Restart();
            _stopwatch.Start();

            var task = requestQuery.GetRequestQuery();

            await task;

            _timer.Stop();
            _stopwatch.Stop();
            if (task.Result != null)
            {
                var response = task.Result;
                if (response.isSuccess)
                {
                    var schemaCustObj = response.CustObj;

                    CheckBox checkBox = new CheckBox();
                    lstResult.loadingControl.lblTotalCount = new Label();
                    lstResult.ShowListView();
                    lstResult.LoadData(schemaCustObj,true, rowCheckBox_Checked, rowCheckBox_Unchecked);
                   // lstViewResult.GenerateListView(schemaCustObj, 150, hasCheckbox: true, checkbox: checkBox, rowCheckBox_Checked, rowCheckBox_Unchecked);
                   // lstViewResult.ShowCheckBoxColumn();

                    lblProgress.Content = $"Status: Done - {elapsedSeconds}";
                }
                else
                {
                    lstResult.ShowError();

                    lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                    // MessageBox.Show(response.ErrorMessage);

                    //if (response.ErrorMessage.ToLower().Contains("cancel"))
                    //    tsStat.Text = $"Status : Cancelled - Execution Time : {stopwatch.Elapsed.TotalSeconds}";
                    //else
                    //    tsStat.Text = $"Status : Done with error - Execution Time : {stopwatch.Elapsed.TotalSeconds}";
                }
            }
            else
            {
                lstResult.ShowError();

                lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                // MessageBox.Show("Error occured");

                //tsStat.Text = $"Status : Done with error - Execution Time : {stopwatch.Elapsed.TotalSeconds}";

            }
        }

        private void rowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                IDictionary<string, object> item = checkBox.DataContext as IDictionary<string, object>;
                if (item != null)
                {
                    string schema = item["SCHEMANAME"].ToString();
                   RemoveSelectedSchemas(schema);
                }
            }
        }

        private void rowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                IDictionary<string, object> item = checkBox.DataContext as IDictionary<string, object>;
                if (item != null)
                {
                    string schema = item["SCHEMANAME"].ToString();
                    AddSelectedSchemas(schema);
                }
            }
        }

        private void AddSelectedSchemas(string schema)
        {
            if (!SelectedSchemas.Contains(schema))
            {
                SelectedSchemas.Add(schema);
            }
        }

        private void RemoveSelectedSchemas(string schema)
        {
            if (SelectedSchemas.Contains(schema))
            {
                SelectedSchemas.Remove(schema);
            }
        }


        private void AddLstViewContextMenu()
        {

            var globalMenu = (ContextMenu)Application.Current.Resources["SharedListViewContextMenu"];
            var newMenu = Utilities.CloneContextMenu(globalMenu, lstViewResult);

            lstViewResult.ContextMenu = newMenu;


        }

        private void lstResult_Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            foreach(var item in  lstResult.dataGrid1.Items)
            {
                if(item is IDictionary<string, object> dictItem)
                {
                    dictItem["IsSelected"] = true;
                    string schema = dictItem["SCHEMANAME"].ToString();
                    AddSelectedSchemas(schema);
                }
            }

            lstResult.dataGrid1.Items.Refresh();
        }

        private void lstResult_UnCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in lstResult.dataGrid1.Items)
            {
                if (item is IDictionary<string, object> dictItem)
                {
                    dictItem["IsSelected"] = false;
                    string schema = dictItem["SCHEMANAME"].ToString();
                    RemoveSelectedSchemas(schema);

                    // var cellContent = lstResult.dataGrid1.Columns[0].GetCellContent(item);

                    // rowCheckBox_Unchecked(this, new RoutedEventArgs());
                }
            }

            lstResult.dataGrid1.Items.Refresh();
        }
    }
}
