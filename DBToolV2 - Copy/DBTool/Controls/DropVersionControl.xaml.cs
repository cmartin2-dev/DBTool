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
    /// Interaction logic for DropVersionControl.xaml
    /// </summary>
    public partial class DropVersionControl : UserControl
    {
        CancellationTokenSource sourceToken = null;
        CancellationToken token;

        private bool isRunning = false;

        public DispatcherTimer _timer;
        public Stopwatch _stopwatch;
        public double elapsedSeconds;

        Label lblProgress;
        Label lblTotalCount;

        private bool isDeleteRunning;


        public List<IDictionary<string, object>> SelectedSchemas { get; set; }
        Task<RequestResponse> task = null;

        public DropVersionControl()
        {
            InitializeComponent();

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20);
            _timer.Tick += _timer_Tick;


            lblProgress = lstResult.lblProgress;
            //var elapsedSeconds = lstResult.elapsedSeconds;
            lblTotalCount = lstResult.lblTotalCount;

            lstResult.HideListView();
            SelectedSchemas = new List<IDictionary<string, object>>();
            isDeleteRunning = false;
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        private void btnGetSchema_Click(object sender, RoutedEventArgs e)
        {
            if (isDeleteRunning)
            {
                MessageBox.Show("Deleting in progress");
                return;
            }

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

                    btnGetSchema.Content = "Executing";
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
                    btnGetSchema.Content = "Get Versions";
                }
                else
                {
                    if (task != null)
                    {
                        sourceToken.Cancel();
                        isRunning = false;

                        btnGetSchema.Content = "Get Versions";
                    }
                }
            }




        }

        private async Task MainExecute()
        {

            ListViewResultControl listViewResultControl = lstResult;

            DataGrid dgGrid = lstResult.dataGrid1;

            lstResult.SetTag("DropVersion");

            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(this.DataContext as RegionTenant);
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;

            string query = @"SELECT DATASCHEMASID, NAME, SCHEMANAME, STATUSMSG, '' AS [STATUS] FROM SCAH.DATASCHEMAS";

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


                    listViewResultControl.LoadData(result, hasCheckbox: true, rowCheckBox_Checked, rowCheckBox_Unchecked);
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

        private void rowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                IDictionary<string, object> item = checkBox.DataContext as IDictionary<string, object>;
                if (item != null)
                {
                    string schema = item["SCHEMANAME"].ToString();
                    RemoveSelectedSchemas(item as IDictionary<string, object>);
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
                    AddSelectedSchemas(item as IDictionary<string, object>);
                }
            }
        }

        private void lstResult_Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in lstResult.dataGrid1.Items)
            {
                if (item is IDictionary<string, object> dictItem)
                {
                    dictItem["IsSelected"] = true;
                    string schema = dictItem["SCHEMANAME"].ToString();
                    AddSelectedSchemas(dictItem);
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
                    RemoveSelectedSchemas(dictItem);

                    // var cellContent = lstResult.dataGrid1.Columns[0].GetCellContent(item);

                    // rowCheckBox_Unchecked(this, new RoutedEventArgs());
                }
            }

            lstResult.dataGrid1.Items.Refresh();
        }


        private void AddSelectedSchemas(IDictionary<string, object> schema)
        {
            if (!SelectedSchemas.Contains(schema))
            {
                SelectedSchemas.Add(schema);
            }
        }

        private void RemoveSelectedSchemas(IDictionary<string, object> schema)
        {
            if (SelectedSchemas.Contains(schema))
            {
                SelectedSchemas.Remove(schema);
            }
        }

        private void btnDropSchema_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                MessageBox.Show("Query in progress");
                return;
            }

            RegionTenant rt = this.DataContext as RegionTenant;

            if (rt == null) return;

            if (SelectedSchemas != null && SelectedSchemas.Count > 0)
            {
                if (isRunning)
                {
                    MessageBox.Show("Query in progress");
                    return;
                }

                if (!isDeleteRunning)
                {
                    btnDropSchema.Content = "Cancel";
                    isDeleteRunning = true;
                    ExecuteDrop();
                }
                else
                {
                    if (task != null)
                    {
                        sourceToken.Cancel();
                        isDeleteRunning = false;
                        lblProgress.Content = $"Status : Cancelled - Execution Time : {_stopwatch.Elapsed.TotalSeconds}";

                        btnDropSchema.Content = "Drop version";
                    }
                }

            }
        }

        private async void ExecuteDrop()
        {
            _timer.IsEnabled = true;
            _stopwatch.Restart();
            _stopwatch.Start();

            sourceToken = new CancellationTokenSource();
            token = sourceToken.Token;

            //set to queue
            foreach (var schema in SelectedSchemas)
            {
                schema["STATUS"] = "In Queue";
            }

            foreach (var schema in SelectedSchemas)
            {
                string schemaName = schema["SCHEMANAME"].ToString();
                string dataSchemasId = schema["DATASCHEMASID"].ToString();

                string deleteUserScript = Scripts.DeleteUser(schemaName);
                string dropSchemaScript = Scripts.DropSchema(schemaName);
                string deleteDataSchema = Scripts.DeleteDataSchema(dataSchemasId);

                bool hasError = false;

                schema["STATUS"] = "Deleting users";
                RequestResponse requestResponse = await ExecuteDropVersion(deleteUserScript, Scripts.ErrorDeletingUsers);
                if (!requestResponse.isSuccess)
                {
                    schema["STATUS"] = requestResponse.ErrorMessage;
                    hasError = true;
                    continue;
                }

                schema["STATUS"] = "Dropping schema";
                requestResponse = await ExecuteDropVersion(dropSchemaScript, Scripts.ErrorDroppingSchema);
                if (!requestResponse.isSuccess)
                {
                    schema["STATUS"] = requestResponse.ErrorMessage;
                    hasError = true;
                    continue;
                }
                schema["STATUS"] = "Deleting schema in table";
                requestResponse = await ExecuteDropVersion(deleteDataSchema, Scripts.ErrorDeletingSchemaInTable);
                if (!requestResponse.isSuccess)
                {
                    schema["STATUS"] = requestResponse.ErrorMessage;
                    hasError = true;
                    continue;
                }

                if (!hasError)
                    schema["STATUS"] = "Done";

            }


            _timer.IsEnabled = false;
            _stopwatch.Stop();

            isDeleteRunning = false;

            btnDropSchema.Content= "Drop version";


        }

        private async Task<RequestResponse> ExecuteDropVersion(string query, string errorMsg)
        {
            RequestResponse response = new RequestResponse();

            RequestQuery requestQuery = new RequestQuery();
            requestQuery.Query = query;

            requestQuery.SetDetails(this.DataContext as RegionTenant);
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;

            task = requestQuery.GetRequestQuery();

            await task;

            if (task.Result != null)
            {
                response = task.Result;
                if (response.isSuccess)
                {

                }
                else
                {
                    if (response.ErrorMessage.ToLower().Contains("cancel"))
                        response.ErrorMessage = $"Cancelled";
                    else
                        response.ErrorMessage = $"{errorMsg} - {response.ErrorMessage}";
                }
            }
            else
            {
                response.ErrorMessage = errorMsg;
            }
            return response;

        }
    }
}
