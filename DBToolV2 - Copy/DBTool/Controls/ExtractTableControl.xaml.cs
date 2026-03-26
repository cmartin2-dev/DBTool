using DBTool.Commons;
using DBTool.Connect;
using Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using CheckBox = System.Windows.Controls.CheckBox;
using Label = System.Windows.Controls.Label;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for ExtractTableControl.xaml
    /// </summary>
    public partial class ExtractTableControl : UserControl
    {

        private bool isGetTableRunning = false;

        Task task = null;
        public Label lblProgress;

        CancellationTokenSource sourceToken = null;
        CancellationToken token;

        public DispatcherTimer _timer;
        public Stopwatch _stopwatch;
        public double elapsedSeconds;
        bool isFetchSchemaRunning = false;
        bool isExtractingTableRunning = false;

        bool isDataLakeRunning = false;

        List<IDictionary<string, object>> TableList = new List<IDictionary<string, object>>();

        public ExtractTableControl()
        {
            InitializeComponent();

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20);
            _timer.Tick += _timer_Tick;

            lblProgress = lstViewSchema.lblProgress;

            lstViewSchema.dataGrid1.SelectionMode = DataGridSelectionMode.Single;

        }


        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        private void txtMaxRecord_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void txtMaxRecord_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void txtMaxRecord_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (int.TryParse(textBox.Text, out int value))
            {
                if (value > 5000)
                {
                    textBox.Text = "5000";
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }

        private void btnFetchSchema_ClickAsync(object sender, RoutedEventArgs e)
        {



            RegionTenant regionTenant = this.DataContext as RegionTenant;
            if (regionTenant.Region != null)
                //   GetDataLake();
                // TestCustomConnStr();
                TryFetchSchema(regionTenant, lstViewSchema.dataGrid1);

        }

        Dictionary<int, JObject> mergeDict = new Dictionary<int, JObject>();

        private async Task GetDataLake()
        {
            isDataLakeRunning = true;
            string date = "2025-10-28";
            string styleId = "4521";

            RequestQuery query = new RequestQuery();
            query.Query = $"SELECT * FROM FP_GET_MODIFIED_STYLE_INSIGHT WHERE  SE_MODIFYDATE BETWEEN '{date}T00:00:00.000Z' AND '{date}T23:59:00.670Z'";
            string styleReturn = await query.TestDataLake(this.DataContext as RegionTenant);

            JArray styleArray = JArray.Parse(styleReturn);

            var styleIds = styleArray.Select(x => (string)x["STYLEID"]).Distinct().ToList();


            query.Query = $"SELECT * FROM FP_MODIFIED_STYLEBOM_INSIGHT WHERE STYLEBOM_STYLEID IN ({string.Join(",", styleIds)})";// AND STYLEBOM_MODIFYDATE BETWEEN '{date}T00:00:00.000Z' AND '{date}T23:59:00.670Z'";
            string bomReturn = await query.TestDataLake(this.DataContext as RegionTenant);

            JArray bomArray = JArray.Parse(bomReturn);

            query.Query = $"SELECT * FROM FP_MODIFIED_STYLECOSTING_INSIGHT WHERE STYLECOSTING_STYLEID IN ({string.Join(",", styleIds)})";//; AND STYLECOSTING_MODIFYDATE BETWEEN '{date}T00:00:00.000Z' AND '{date}T23:59:00.670Z'";
            string costingReturn = await query.TestDataLake(this.DataContext as RegionTenant);

            JArray costingArray = JArray.Parse(costingReturn);

            query.Query = $"SELECT * FROM FP_MODIFIED_STYLEMEASUREMENT_INSIGHT WHERE STYLEMEASUREMENT_STYLEID IN ({string.Join(",", styleIds)})";// AND STYLEMEASUREMENT_MODIFYDATE BETWEEN '{date}T00:00:00.000Z' AND '{date}T23:59:00.670Z'";
            string measurementReturn = await query.TestDataLake(this.DataContext as RegionTenant);

            JArray measurementArray = JArray.Parse(measurementReturn);

            JArray jStyleMeasurementArray = new JArray();

            JToken styleMeasurement = null;

            foreach (var style in styleArray)
            {
                styleMeasurement = style.DeepClone();

                var _styleid = style["STYLEID"].ToString();
                var _styleCnt = style["STYLE_CNT"].ToString();

                var stylebom = new JArray { };
                var stylecosting = new JArray { };
                var stlyemeasurement = new JArray { };

                var bomArrays = bomArray.Where(x => (string)x["STYLEBOM_STYLEID"] == _styleid && (string)x["STYLEBOM_CNT"] == _styleCnt).ToList();

                foreach (var bom in bomArrays)
                {
                    stylebom.Add(bom);
                }
                style["STYLEBOM"] = stylebom;

                var costingArrays = costingArray.Where(x => (string)x["STYLECOSTING_STYLEID"] == _styleid && (string)x["STYLECOSTING_CNT"] == _styleCnt).ToList();

                foreach (var costing in costingArrays)
                {
                    stylecosting.Add(costing);
                }
                style["STYLECOSTING"] = stylecosting;

                var measurmeentArrays = measurementArray.Where(x => (string)x["STYLEMEASUREMENT_STYLEID"] == _styleid && (string)x["STYLEMEASUREMENT_CNT"] == _styleCnt).ToList();

                foreach (var measurement in measurmeentArrays)
                {
                    stlyemeasurement.Add(measurement);
                }


                styleMeasurement["STYLEMEASUREMENT"] = stlyemeasurement;
                jStyleMeasurementArray.Add(styleMeasurement);

            }

            //query.Query = $"SELECT * FROM FP_GET_STYLECOSTINGELEMENT_CHANGES WHERE STYLECOSTINGELEMENT_MODIFYDATE BETWEEN '{date}T00:00:00.000Z' AND '{date}T23:59:00.670Z'";
            //string costingReturn = await query.TestDataLake(this.DataContext as RegionTenant);

            //query.Query = $"SELECT * FROM FP_GET_MEASUREMENT_CHANGES WHERE STYLEMEASPOMSIZE_MODIFYDATE BETWEEN '{date}T00:00:00.000Z' AND '{date}T23:59:00.670Z'";
            //string measurementReturn = await query.TestDataLake(this.DataContext as RegionTenant);



            //JArray bomArray = JArray.Parse(bomlineReturn);
            //var costingArray = JArray.Parse(costingReturn);
            //var measurementArray = JArray.Parse(measurementReturn);

            //List<string> bom_styleId = bomArray.Select(x => (string)x["STYLEBOM_STYLEID"]).Distinct().ToList();
            //List<string> costing_styleId = costingArray.Select(x => (string)x["STYLECOSTING_STYLEID"]).Distinct().ToList();
            //List<string> measurement_styleId = measurementArray.Select(x => (string)x["STYLEMEAS_STYLEID"]).Distinct().ToList();

            //List<string> styleIds = bom_styleId.Concat(costing_styleId).Concat(measurement_styleId).Distinct().ToList();

            //query.Query = $"SELECT * FROM FP_GET_STYLES WHERE STYLE_STYLEID IN ({string.Join(",", styleIds)})";
            //string styleReturn = await query.TestDataLake(this.DataContext as RegionTenant);

            //var styleArray = JArray.Parse(styleReturn);

            //MergeToDict(styleArray, bomArray, "bomline");

            isDataLakeRunning = false;
        }

        private void MergeToDict(JArray styleArray, JArray arr, string entity)
        {
            var merged = new JArray();

            foreach (var item in arr)
            {
                // Skip if missing StyleID
                if (item.Value<string>("STYLEBOM_STYLEID") == null) continue;

                if (entity == "bomline")
                {
                    string stylebomStyleid = item.Value<string>("STYLEBOM_STYLEID");
                    var style = styleArray.FirstOrDefault(x => x.Value<string>("STYLE_STYLEID") == stylebomStyleid);
                    if (style != null)
                    {
                        var mergedObj = new JObject(item);
                        foreach (var prop in style.Children<JProperty>())
                        {
                            if (mergedObj[prop.Name] == null)
                                mergedObj[prop.Name] = prop.Value;
                        }
                        merged.Add(mergedObj);
                    }
                }
            }

            arr = merged;

        }

        private async Task TestCustomConnStr()
        {
            string connStr = "Server=fplmdev02-lsnr.stable.infordev.local;Initial Catalog=provisiondb_bugfix;User ID=provisionuser;Password=1Qaz2Wsx;MultiSubnetFailover=True;ConnectRetryCount=3;TrustServerCertificate=True;";

            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(this.DataContext as RegionTenant);

            requestQuery.Query = @"SELECT * FROM DBO.TENANTS";
            RequestResponse task = await requestQuery.GetRequestQueryCustomCS(connStr);

            isDataLakeRunning = false;
        }

        private async Task TryFetchSchema(RegionTenant regionTenant, DataGrid listView)// System.Windows.Controls.ListView listView)
        {
            try
            {
                if (!isFetchSchemaRunning)
                {
                    if (isGetTableRunning)
                        return;

                    if (isExtractingTableRunning)
                        return;

                    isFetchSchemaRunning = true;
                    lblProgress = lstViewSchema.lblProgress;
                    lstViewSchema.ShowLoading();

                    btnFetchSchema.Content = "Executing";
                    task = FetchSchema(regionTenant, listView);

                }
                else
                {
                    if (task != null)
                    {
                        sourceToken.Cancel();
                        isFetchSchemaRunning = false;
                        lblProgress.Content = $"Status : Cancelled - Execution Time : {_stopwatch.Elapsed.TotalSeconds}";

                        btnFetchSchema.Content = "Fetch Schema";
                    }
                }
            }
            catch { }
        }

        private async Task FetchSchema(RegionTenant regionTenant, DataGrid listView)// ListView listView)
        {


            sourceToken = new CancellationTokenSource();
            token = sourceToken.Token;

            string query = @"SELECT 0 AS 'DATASCHEMASID','SCAH' as 'MODULECODE','SCAH' AS 'NAME', 'SCAH' AS 'SCHEMANAME', '' AS 'DBVERSION' UNION SELECT DATASCHEMASID,MODULECODE,NAME,SCHEMANAME,DBVERSION FROM SCAH.DATASCHEMAS WHERE ISREADY = 1";

            RequestQuery requestQuery = new RequestQuery();
            requestQuery.Query = query;
            requestQuery.SetDetails(regionTenant);
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;
            _timer.Start();
            _stopwatch.Restart();
            _stopwatch.Start();

            var task1 = requestQuery.GetRequestQuery();

            await task1;

            _timer.Stop();
            _stopwatch.Stop();
            if (task1.Result != null)
            {
                var response = task1.Result;
                if (response.isSuccess)
                {
                    var schemaCustObj = response.CustObj;

                    CheckBox checkBox = new CheckBox();
                    lstViewSchema.loadingControl.lblTotalCount = new Label();
                    lstViewSchema.ShowListView();
                    lstViewSchema.LoadData(schemaCustObj);

                    lblProgress.Content = $"Status: Done - {elapsedSeconds}";
                }
                else
                {
                    lstViewSchema.ShowError();

                    lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                }
            }
            else
            {
                lstViewSchema.ShowError();

                lblProgress.Content = $"Status: Error - {elapsedSeconds}";

            }
            isFetchSchemaRunning = false;
            btnFetchSchema.Content = "Fetch Schema";
        }

        private void lstViewSchema_SelectionChangedExecute(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnGetTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isGetTableRunning)
                {

                    if (isFetchSchemaRunning)
                        return;

                    if (isExtractingTableRunning)
                        return;

                    if (lstViewSchema.dataGrid1.SelectedItem != null)
                    {

                        IDictionary<string, object> selectedItem = lstViewSchema.dataGrid1.SelectedItem as IDictionary<string, object>;
                        if (selectedItem != null)
                        {
                            string schemaName = selectedItem["SCHEMANAME"].ToString();

                            isGetTableRunning = true;
                            lblProgress = tabResult.lblProgress;
                            tabResult.ShowLoading();

                            btnGetTable.Content = "Executing";
                            task = GetTables(schemaName);

                        }

                    }

                }
                else
                {
                    if (task != null)
                    {
                        sourceToken.Cancel();
                        isGetTableRunning = false;
                        lblProgress.Content = $"Status : Cancelled - Execution Time : {_stopwatch.Elapsed.TotalSeconds}";

                    }
                }
            }
            catch { }


            if (!isGetTableRunning)
            {

                btnGetTable.Content = "Get Table";
            }
        }

        private async Task GetTables(string schemaName)
        {
            sourceToken = new CancellationTokenSource();
            token = sourceToken.Token;
            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(this.DataContext as RegionTenant);

            requestQuery.Query = Scripts.GetSchemaTable(schemaName);
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;

            _timer.Start();
            _stopwatch.Restart();
            _stopwatch.Start();
            Task<RequestResponse> taskExec = requestQuery.GetRequestQuery();

            await taskExec;
            _timer.Stop();
            _stopwatch.Stop();
            if (taskExec.Result != null)
            {
                var response = taskExec.Result;
                if (response.isSuccess)
                {
                    var schemaCustObj = response.CustObj;

                    CheckBox checkBox = new CheckBox();
                    tabResult.loadingControl.lblTotalCount = new Label();
                    tabResult.ShowListView();
                    tabResult.LoadData(schemaCustObj, true, rowCheckBox_Checked, rowCheckBox_Unchecked);

                    tabResult.lblProgress.Content = $"Status: Done - {elapsedSeconds}";
                }
                else
                {
                    tabResult.ShowError();

                    tabResult.lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                }
            }
            else
            {
                tabResult.ShowError();

                tabResult.lblProgress.Content = $"Status: Error - {elapsedSeconds}";

            }
            isGetTableRunning = false;
            btnGetTable.Content = "Get Table";
        }



        private void rowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                IDictionary<string, object> item = checkBox.DataContext as IDictionary<string, object>;
                if (item != null)
                {
                    string schema = item["TABLE"].ToString();
                    RemoveTable(item);
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
                    string schema = item["TABLE"].ToString();
                    AddTable(item);
                }
            }
        }

        private void HeaderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in tabResult.dataGrid1.Items)
            {
                if (item is IDictionary<string, object> dictItem)
                {
                    dictItem["IsSelected"] = false;
                    string schema = dictItem["TABLE"].ToString();
                    RemoveTable(dictItem);
                }
            }

            //   tabResult.dataGrid1.Items.Refresh();
        }

        private void HeaderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in tabResult.dataGrid1.Items)
            {
                if (item is IDictionary<string, object> dictItem)
                {
                    dictItem["IsSelected"] = true;
                    string schema = dictItem["TABLE"].ToString();
                    AddTable(dictItem);
                }
            }

            //   tabResult.dataGrid1.Items.Refresh();
        }

        private void AddTable(IDictionary<string, object> item)
        {
            if (!TableList.Contains(item))
                TableList.Add(item);
        }
        private void RemoveTable(IDictionary<string, object> item)
        {
            TableList.Remove(item);
        }

        private void btnExtractTable_Click(object sender, RoutedEventArgs e)
        {
            if (!isExtractingTableRunning)
            {
                if (isFetchSchemaRunning)
                    return;

                if (isGetTableRunning)
                    return;

                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string location = folderBrowserDialog.SelectedPath;

                    isExtractingTableRunning = true;
                    btnExtractTable.Content = "Executing";
                    task = ExtractTable(location);
                }
            }
            else
            {
                if (task != null)
                {
                    sourceToken.Cancel();
                    isExtractingTableRunning = false;
                    lblProgress.Content = $"Status : Cancelled - Execution Time : {_stopwatch.Elapsed.TotalSeconds}";

                }

            }
        }

        private async Task ExtractTable(string location)
        {
            if (TableList.Count != 0)
            {
                string schemaName = string.Empty;
                RegionTenant regionTenant = this.DataContext as RegionTenant;
                sourceToken = new CancellationTokenSource();
                token = sourceToken.Token;

                lblProgress = tabResult.lblProgress;
                btnExtractTable.Content = "Executing";

                _timer.Start();
                _stopwatch.Restart();
                _stopwatch.Start();

                List<string> fshList = new List<string>();

                foreach (var item in TableList)
                {
                    item["STATUS"] = "In queue";
                }

                foreach (var item in TableList)
                {
                    schemaName = item["SCHEMA"].ToString();
                    string tableName = item["TABLE"].ToString();
                    string columnNames = item["COLUMNS"].ToString();

                    if (!fshList.Contains(schemaName))
                        Directory.CreateDirectory($"{location}\\{schemaName}");

                    string countQuery = string.Format("SELECT COUNT(*) AS 'COUNT' FROM {0}.{1}", schemaName, tableName);

                    RequestQuery requestQuery = new RequestQuery();
                    requestQuery.SetDetails(regionTenant);
                    requestQuery.sourceToken = sourceToken;
                    requestQuery.token = token;

                    requestQuery.Query = countQuery;

                    var countTask = requestQuery.GetRequestQuery();

                    await countTask;

                    if (countTask != null && countTask.Result != null)
                    {
                        if (countTask.Result.isSuccess)
                        {
                            decimal runPerRecord = string.IsNullOrEmpty(txtMaxRecord.Text) ? 5000 : Convert.ToDecimal(txtMaxRecord.Text);
                            decimal count = Convert.ToDecimal(countTask.Result.CustObj.Objects[0].Object["COUNT"]);

                            item["TOTALCOUNT"] = count.ToString();

                            double totalRun = Math.Ceiling(Convert.ToDouble(count / runPerRecord));

                            RequestQuery requestExtractQuery = new RequestQuery();
                            requestExtractQuery.SetDetails(regionTenant);
                            requestExtractQuery.sourceToken = sourceToken;
                            requestExtractQuery.token = token;

                            string processedColumns = string.Join(",", columnNames.Split(",").Select(x => $"[{x.Trim()}]").ToList());

                            long offSet = 0;
                            bool hasError = false;

                            for (int i = 0; i < totalRun; i++)
                            {
                                string query = Scripts.ExtractTableScript(processedColumns, schemaName, tableName, offSet, Convert.ToInt32(runPerRecord));

                                offSet += Convert.ToInt64(runPerRecord);
                                requestExtractQuery.Query = query;

                                var extractDataSetTask = requestExtractQuery.GetRequestQueryDataset();
                                await extractDataSetTask;

                                if (extractDataSetTask != null && extractDataSetTask.Result != null)
                                {
                                    RequestResponse response = extractDataSetTask.Result;
                                    if (response.isSuccess)
                                    {
                                        Utilities.WriteFile(response.DataSet, tableName, i + 1, location + $"\\{schemaName}");
                                    }
                                    else
                                    {

                                        item["STATUS"] = extractDataSetTask.Result.ErrorMessage;
                                        hasError = true;
                                        break;
                                    }

                                }
                                else
                                {
                                    item["STATUS"] = "Error occured";
                                    hasError = true;
                                    break;
                                }

                                if (!hasError)
                                {
                                    item["STATUS"] = "Done";
                                }
                            }
                        }
                        else
                        {
                            item["STATUS"] = countTask.Result.ErrorMessage;
                        }
                    }
                }

                _timer.Stop();
                _stopwatch.Stop();

                if (!Directory.Exists(location + $"\\{schemaName}\\DbTables"))
                    Directory.CreateDirectory(location + $"\\{schemaName}\\DbTables");
                Utilities.CompressFile(location + $"\\{schemaName}\\DbTables", location + $"\\{schemaName}", "FAS");

                isExtractingTableRunning = false;
                btnExtractTable.Content = "Extract Table";
            }
        }
    }
}
