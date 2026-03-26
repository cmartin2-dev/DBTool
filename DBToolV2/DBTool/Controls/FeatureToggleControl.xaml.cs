using DBTool.Commons;
using DBTool.Connect;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using Entities;
using Newtonsoft.Json;
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
    /// Interaction logic for FeatureToggleControl.xaml
    /// </summary>
    public partial class FeatureToggleControl : UserControl
    {

        CancellationTokenSource sourceToken = null;
        CancellationToken token;

        private bool isRunning = false;

        public DispatcherTimer _timer;
        public Stopwatch _stopwatch;
        public double elapsedSeconds;

        Label lblProgress;
        Label lblTotalCount;


        Task<RequestResponse> task = null;
        public FeatureToggleControl()
        {
            InitializeComponent();

            AddLstViewContextMenu();

             lblProgress = lstResult.lblProgress;
            //var elapsedSeconds = lstResult.elapsedSeconds;
             lblTotalCount = lstResult.lblTotalCount;

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20);
            _timer.Tick += _timer_Tick; ;

            lstResult.dataGrid1.IsReadOnly = true;
            lstResult.HideListView();
            
        }

        private void AddLstViewContextMenu()
        {
            var cmCopyRow = new MenuItem { Name = "CopyRow", Header = "Copy Row" };

            cmCopyRow.Click += MenuItem_Click;

            lstResult.AddContextMenu(cmCopyRow);

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                if (item.Name.ToLower() == "copyrow")
                    CopySelectedRow();
            }

        }

        private void CopySelectedRow()
        {
            try
            {
                if (lstResult.dataGrid1.SelectedItems != null && lstResult.dataGrid1.SelectedItems.Count > 0)
                {

                    string jsonString = JsonConvert.SerializeObject(lstResult.dataGrid1.SelectedItems, Newtonsoft.Json.Formatting.Indented);

                    jsonString = Utilities.BeautifyJson(jsonString);

                    Clipboard.SetText(jsonString);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        private void btnGetToggle_Click(object sender, RoutedEventArgs e)
        {
            var aaa = this.DataContext;
            grpDetail.DataContext = null;
            Execute();
        }

        private async void Execute()
        {
            var datacontext = this.DataContext;
            if (datacontext != null)
            {

                if (!isRunning)
                {
                    sourceToken = new CancellationTokenSource();
                    token = sourceToken.Token;

                    btnGetToggle.Content = "Executing";
                    RegionTenant rt = this.DataContext as RegionTenant;
                    if (rt != null && rt.Region != null)
                    {
                        isRunning = true;

                        if (!sourceToken.IsCancellationRequested)
                        {
                            await MainExecute();
                        }

                    }


                    isRunning = false;
                    btnGetToggle.Content = "Get Feature Toggle";
                }
                else
                {
                    if (task != null)
                    {
                        sourceToken.Cancel();
                        isRunning = false;

                        btnGetToggle.Content = "Get Feature Toggle";
                    }
                }
            }




        }

        private async void ExecuteSave(string script)
        {
            var datacontext = this.DataContext;
            if (datacontext != null)
            {
                if (script != null)
                {
                    if (!isRunning)
                    {
                        sourceToken = new CancellationTokenSource();
                        token = sourceToken.Token;

                        //btnGetToggle.Content = "Executing";
                        RegionTenant rt = this.DataContext as RegionTenant;
                        if (rt != null && rt.Region != null)
                        {
                            isRunning = true;

                            if (!sourceToken.IsCancellationRequested)
                            {
                                await MainExecuteSave(script);
                            }

                        }


                        isRunning = false;
                        //btnGetToggle.Content = "▶️ Execute";
                    }
                    else
                    {
                        if (task != null)
                        {
                            sourceToken.Cancel();
                            isRunning = false;

                           // btnGetToggle.Content = "▶️ Execute";
                        }
                    }
                }
            }
        }

        private async Task MainExecuteSave(string script)
        {
            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(this.DataContext as RegionTenant);

            requestQuery.Query = script;

            task = requestQuery.GetRequestQuery();
            await task;

            if (task != null)
            {
                RequestResponse _response = task.Result;
                if (_response.isSuccess)
                {
                    MessageBox.Show("App feature saved successfully.", "Saved", MessageBoxButton.OK);
                }
            }
        }

        private async Task MainExecute()
        {

            ListViewResultControl listViewResultControl = lstResult;

            DataGrid dgGrid = lstResult.dataGrid1;

            lstResult.SetTag("FeatureToggle");

            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(this.DataContext as RegionTenant);
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;

            string query = @"SELECT [APPFEATUREID],[KEY],[DISPLAYNAME],[ENABLED],[VISIBLE],[CREATEDATE],[MODIFYDATE],[FEATURETYPE],
[DESCRIPTION],[SEQ],[MODIFYID],[REQCUSTOMERACTION],[DEPENDENCYID],[VERSION],[EXPIRYDATE],[OVERRIDESID],[FEATUREID],
[ROWVERSION],[RELEASEEXPIRY],[RELEASEENABLED],[CSAVAILABILITYMODE] FROM SCAH.APPFEATURE";

            requestQuery.Query = query;

            _timer.IsEnabled = true;
            _timer.Start();
            _stopwatch.Restart();
            _stopwatch.Start();

            lstResult.ShowLoading();
            task = requestQuery.GetRequestQuery();
            await task;



            if (task != null)
            {
                RequestResponse _response = task.Result;
                if (_response.isSuccess)
                {
                    CustObj result = _response.CustObj as CustObj;

                    int itemCount = result.Objects.Count;
  

                    listViewResultControl.LoadData(result);
                    lstResult.loadingControl.lblTotalCount = new Label();
                    lstResult.ShowListView();
                    lblTotalCount.Content = $"Total Count : {itemCount}";

                    lblProgress.Content = $"Status: Done - {elapsedSeconds}";
                }
                else
                {
                    if (_response.ErrorMessage.ToLower().Contains("cancel"))
                    {
                        lblProgress.Content = $"Status: Cancelled - {elapsedSeconds}";
                        lstResult.loadingControl.txtErrorName.Content = "Cancelled";

                    }
                    else
                    {
                        lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                        lstResult.loadingControl.txtErrorName.Content = "Error";
                        lstResult.loadingControl.txtErrorDetail.Text = _response.ErrorMessage;

                    }
                    
                    lstResult.ShowError();
                }
            }
            else
            {
                lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                lstResult.ShowError();
                lstResult.loadingControl.txtErrorName.Content = "Error";
            }

            _timer.IsEnabled = false;
            _timer.Stop();
            _stopwatch.Stop();
        }

        private void lstResult_SelectionChangedExecute(object sender, SelectionChangedEventArgs e)
        {
            var item = lstResult.dataGrid1.SelectedItem;
            if (item != null)
            {

                grpDetail.DataContext = item ;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
           var itemData = grpDetail.DataContext as IDictionary<string,object>;
            if (itemData != null)
            {
                int appFeatureId = Convert.ToInt32(itemData["APPFEATUREID"]);
                string key = itemData["KEY"] != null ? itemData["KEY"].ToString() : "";
                string displayName = itemData["DISPLAYNAME"] != null ? itemData["DISPLAYNAME"].ToString() : "";
                bool enabled = Convert.ToBoolean(itemData["ENABLED"]);
                bool visible = Convert.ToBoolean(itemData["VISIBLE"]);
                string description = itemData["DESCRIPTION"] != null ? itemData["DESCRIPTION"].ToString() : "";
                string version = itemData["VERSION"] != null ? itemData["VERSION"].ToString() : "";
                string expiryDate = itemData["EXPIRYDATE"] != null ? itemData["EXPIRYDATE"].ToString() : null;

                if(string.IsNullOrWhiteSpace(key))
                {
                    MessageBox.Show("Key is required","ERROR",MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

              string script =  Scripts.UpdateAppFeature(enabled, visible, version, expiryDate, key,displayName,description);

                ExecuteSave(script);

            }
        }
    }
}
