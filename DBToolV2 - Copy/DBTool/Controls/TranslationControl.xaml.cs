using Connect;
using DBTool.Commons;
using DBTool.Connect;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using DocumentFormat.OpenXml.Wordprocessing;
using Entities;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
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
using CheckBox = System.Windows.Controls.CheckBox;
using Label = System.Windows.Controls.Label;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Task = System.Threading.Tasks.Task;
using UserControl = System.Windows.Controls.UserControl;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for TranslationControl.xaml
    /// </summary>
    public partial class TranslationControl : UserControl
    {
        private DispatcherTimer _timer;
        private Stopwatch _stopwatch;
        private double elapsedSeconds;

        private DispatcherTimer _timer1;
        private Stopwatch _stopwatch1;
        private double elapsedSeconds1;

        private DispatcherTimer _timer2;
        private Stopwatch _stopwatch2;
        private double elapsedSeconds2;


        private DispatcherTimer _timer3;
        private Stopwatch _stopwatch3;
        private double elapsedSeconds3;


        private DispatcherTimer _timer4;
        private Stopwatch _stopwatch4;
        private double elapsedSeconds4;

        private Label lblProgress;
        private Label lblProgress1;
        private Label lblProgress2;
        private Label lblProgress3;
        private Label lblProgress4;

        private bool isRunning = false;
        private bool isCompareRunning = false;

        CancellationTokenSource sourceToken = null;
        CancellationToken token;

        List<Language> SelectedLanguage;
        List<IDictionary<string, object>> localeToScript;
        string selectedSchemaName = string.Empty;

        Task<RequestResponse> task;

        string compareExcelFilename = string.Empty;


        ExcelConnect excelConnect = null;
        Task<CustObj> ExcelObj = null;
        public TranslationControl()
        {
            InitializeComponent();

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer.Tick += _timer_Tick;

            _stopwatch1 = new Stopwatch();
            _timer1 = new DispatcherTimer();
            _timer1.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer1.Tick += _timer1_Tick;


            _stopwatch2 = new Stopwatch();
            _timer2 = new DispatcherTimer();
            _timer2.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer2.Tick += _timer2_Tick;

            _stopwatch3 = new Stopwatch();
            _timer3 = new DispatcherTimer();
            _timer3.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer3.Tick += _timer3_Tick;

            _stopwatch4 = new Stopwatch();
            _timer4 = new DispatcherTimer();
            _timer4.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer4.Tick += _timer4_Tick;

            lstSchema.dataGrid1.SelectionMode = DataGridSelectionMode.Single;
            lstResult.dataGrid1.SelectionMode = DataGridSelectionMode.Single;
            lstLanguage.dataGrid1.SelectionMode = DataGridSelectionMode.Single;

            lblProgress = lstResult.lblProgress;
            lblProgress1 = lstSchema.lblProgress;
            lblProgress2 = lstLocale.lblProgress;
            lblProgress3 = lstCompareDBData.lblProgress;
            //  lstResult.HideListView();
            //  lstSchema.HideListView();

            SetDataContext();
            GetRegions();
            LoadLanguage();
            LoadExcludedKeys();

            SelectedLanguage = new List<Language>();
            localeToScript = new List<IDictionary<string, object>>();

            lstResult.AddContextMenu();
            lstSchema.AddContextMenu();
            lstLanguage.AddContextMenu();
            LstLocaleContextMenu();

            lstCompareDBData.AddContextMenu();
        }

        private void LstLocaleContextMenu()
        {
            var cmCopyRow = new MenuItem { Name = "CopyRow", Header = "Copy Row" };

            cmCopyRow.Click += MenuItem_Click;

            lstLocale.AddContextMenu(cmCopyRow);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                if (item.Name.ToLower() == "copyrow")
                {
                    var list = lstLocale.dataGrid1.SelectedItems.Cast<ExpandoObject>().Cast<IDictionary<string, object>>().ToList();
                    Utilities.CopySelectedRow(list);
                }
            }

        }



        private void LoadExcludedKeys()
        {
            var excludedKeys = (this.DataContext as SettingsObject).ExcludedKeys;
            txtExcludedKeys.Text = excludedKeys.ToString();

        }

        private void LoadLanguage()
        {

            ObservableCollection<Language> languages = (this.DataContext as SettingsObject).Languages;

            lstLanguage.CustomColumns.Clear();

            lstLanguage.CustomColumns.Add("IsSelected", "checkbox");
            lstLanguage.CustomColumns.Add("Name", "Name");
            lstLanguage.CustomColumns.Add("Culture", "Culture");
            lstLanguage.LoadData(languages, rowCheckBox_Checked, rowCheckBox_Unchecked);


        }

        private void rowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                Language language = checkBox.DataContext as Language;
                if (language != null)
                {
                    RunRowUncheckedEvent(language);
                }
            }
        }

        private void rowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                Language language = checkBox.DataContext as Language;
                if (language != null)
                {
                    // regionTenant.Region = cmbRegion.SelectedItem as Region;
                    RunRowCheckedEvent(language);
                }
            }
        }

        private void RunRowCheckedEvent(Language language)
        {

            if (!SelectedLanguage.Contains(language))
                SelectedLanguage.Add(language);
        }

        private void RunRowUncheckedEvent(Language language)
        {
            if (SelectedLanguage.Contains(language))
                SelectedLanguage.Remove(language);
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        private void _timer1_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds1 = _stopwatch1.Elapsed.TotalSeconds;

            lblProgress1.Content = $"Status: Running - {elapsedSeconds1}";
        }

        private void _timer2_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds2 = _stopwatch2.Elapsed.TotalSeconds;

            lblProgress2.Content = $"Status: Running - {elapsedSeconds2}";
        }

        private void _timer3_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds3 = _stopwatch3.Elapsed.TotalSeconds;

            lblProgress3.Content = $"Status: Running - {elapsedSeconds3}";
        }


        private void _timer4_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds4 = _stopwatch4.Elapsed.TotalSeconds;

            lblProgress4.Content = $"Status: Running - {elapsedSeconds4}";
        }



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
        public void SetDataContext()
        {
            this.DataContext = StaticFunctions.AppConnection.settingsObject;
        }

        private async void cmbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Entities.Region item = cmbRegion.SelectedItem as Entities.Region;
            if (item != null)
            {
                lstResult.ClearItems();
                lstSchema.ClearItems();

                cmbRegion.IsEnabled = false;

                lstResult.SetTag(item.RegionName);

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

                            tenantList.ForEach(x => x.Region = item);

                            string[] ColumnNames = { "", "Name" };
                            string[] bindingNames = { "tenantId" };

                            lstResult.CustomColumns.Clear();

                            lstResult.CustomColumns.Add("tenantId", "Name");
                            lstResult.LoadData(tenantList);


                            lblProgress.Content = $"Status: Done - {elapsedSeconds}";
                            lstResult.ShowListView();
                            lstSchema.ShowListView();
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

            cmbRegion.IsEnabled = true;
        }

        private void lstViewEnvironment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstResult.dataGrid1.SelectedItem != null)
            {
                RegionTenant regionTenant = lstResult.dataGrid1.SelectedItem as RegionTenant;
                TryFetchSchema(regionTenant);
            }
        }

        private async Task TryFetchSchema(RegionTenant regionTenant)
        {
            try
            {
                lstSchema.ShowLoading();
                var task = FetchSchema(regionTenant);
            }
            catch { }
        }

        private async Task FetchSchema(RegionTenant regionTenant)
        {

            string query = @"SELECT 0 AS 'DATASCHEMASID','SCAH' as 'MODULECODE','SCAH' AS 'NAME', 'SCAH' AS 'SCHEMANAME', '' AS 'DBVERSION' UNION SELECT DATASCHEMASID,MODULECODE,NAME,SCHEMANAME,DBVERSION FROM SCAH.DATASCHEMAS WHERE ISREADY = 1";

            RequestQuery requestQuery = new RequestQuery();
            requestQuery.Query = query;
            requestQuery.SetDetails(regionTenant);

            _timer1.Start();
            _stopwatch1.Restart();
            _stopwatch1.Start();

            var task = requestQuery.GetRequestQuery();

            await task;

            _timer1.Stop();
            _stopwatch1.Stop();
            if (task.Result != null)
            {
                var response = task.Result;
                if (response.isSuccess)
                {
                    var schemaCustObj = response.CustObj;

                    CheckBox checkBox = new CheckBox();
                    lstSchema.loadingControl.lblTotalCount = new Label();
                    lstSchema.ShowListView();
                    lstSchema.LoadData(schemaCustObj);

                    lblProgress1.Content = $"Status: Done - {elapsedSeconds}";
                }
                else
                {
                    lstSchema.ShowError();

                    lblProgress1.Content = $"Status: Error - {elapsedSeconds}";
                }
            }
            else
            {
                lstSchema.ShowError();

                lblProgress1.Content = $"Status: Error - {elapsedSeconds}";

            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (lstResult.dataGrid1.SelectedItem != null)
            {
                if (lstSchema.dataGrid1.SelectedItem != null)
                {
                    if (SelectedLanguage == null || SelectedLanguage.Count == 0)
                        return;

                    if (!isRunning)
                    {
                        sourceToken = new CancellationTokenSource();
                        token = sourceToken.Token;
                        btnLoad.IsEnabled = false;
                        btnLoad.Content = "Loading...";
                        RegionTenant selectedRegionTenant = lstResult.dataGrid1.SelectedItem as RegionTenant;
                        IDictionary<string, object> selectedSchema = lstSchema.dataGrid1.SelectedItem as IDictionary<string, object>;

                        //settings
                        string removeExlcudedKeys = chkRemoveExcludedKeys.IsChecked.Value ? txtExcludedKeys.Text : string.Empty;
                        bool removeMnemonic = chkRemoveMnemonic.IsChecked.Value ? true : false;
                        bool isNewUpdateOnly = chkNewUpdatedLocaleOnly.IsChecked.Value ? true : false;
                        bool isIncludeLength = chkIncludedLength.IsChecked.Value ? true : false;

                        selectedSchemaName = selectedSchema["SCHEMANAME"].ToString();
                        string query = string.Empty;

                        if (selectedSchemaName.ToLower() == "scah")
                        {
                            query = Scripts.GenerateSCAHLocaleScript(SelectedLanguage, removeExlcudedKeys, removeMnemonic, isNewUpdateOnly, isIncludeLength);
                        }
                        else
                        {
                            query = Scripts.GenerateCultureInfoLocaleScript(SelectedLanguage, selectedSchemaName, isIncludeLength);
                        }

                        Execute(selectedRegionTenant, query);
                    }
                    else
                    {
                        if (task != null)
                        {
                            sourceToken.Cancel();
                            isRunning = false;

                            btnLoad.IsEnabled = true;
                            btnLoad.Content = "Load";
                        }
                    }
                }
            }
        }

        private async void Execute(RegionTenant regionTenant, string query)
        {
            if (!sourceToken.IsCancellationRequested)
            {
                await MainExecute(regionTenant, query);
            }
        }

        private async Task MainExecute(RegionTenant regionTenant, string query)
        {

            DataGrid dgGrid = lstLocale.dataGrid1;


            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(regionTenant);
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;

            requestQuery.Query = query;
            lstLocale.SetTag(regionTenant.tenantId);

            _timer2.IsEnabled = true;
            _timer2.Start();
            _stopwatch2.Restart();
            _stopwatch2.Start();

            task = requestQuery.GetRequestQuery();

            lstLocale.ShowLoading();

            await task;





            if (task != null)
            {
                RequestResponse _response = task.Result;
                if (_response.isSuccess)
                {
                    CustObj result = _response.CustObj as CustObj;

                    int itemCount = result.Objects.Count;
                    //  if (itemCount > 0)
                    //  {

                    // lstViewResult.GenerateListView(result);

                    Utilities.ReProcessTranslation(result);
                    lstLocale.LoadData(result);

                    lstLocale.ShowListView();

                    lblProgress2.Content = $"Status: Done - {elapsedSeconds2}";
                }
                else
                {
                    if (_response.ErrorMessage.ToLower().Contains("cancel"))
                    {
                        lblProgress2.Content = $"Status: Cancelled - {elapsedSeconds2}";
                        lstLocale.loadingControl.txtErrorName.Content = "Cancelled";

                    }
                    else
                    {
                        lblProgress2.Content = $"Status: Error - {elapsedSeconds2}";
                        lstLocale.loadingControl.txtErrorName.Content = "Error";
                        lstLocale.loadingControl.txtErrorDetail.Text = _response.ErrorMessage;

                    }
                    lstLocale.ShowError();
                }
            }
            else
            {
                lblProgress2.Content = $"Status: Error - {elapsedSeconds2}";
                lstLocale.ShowError();
                lstLocale.loadingControl.txtErrorName.Content = "Error";
            }

            _timer2.IsEnabled = false;
            _timer2.Stop();
            _stopwatch2.Stop();

            btnLoad.IsEnabled = true;
            btnLoad.Content = "Load";

        }



        private void HeaderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAllRowCheckBoxes(false);
        }

        private void HeaderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetAllRowCheckBoxes(true);
        }

        private void SetAllRowCheckBoxes(bool isChecked)
        {
            foreach (var item in lstLanguage.dataGrid1.Items)
            {
                if (item is Language language)
                {
                    if (language.IsSelected != isChecked)
                    {
                        language.IsSelected = isChecked;
                        if (isChecked)
                            RunRowCheckedEvent(language);
                        else
                            RunRowUncheckedEvent(language);
                    }
                }
            }
            lstLanguage.dataGrid1.Items.Refresh();
        }

        private void btnSelectLocaleFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel File|*.xlsx";

            if (openFileDialog.ShowDialog() == true)
            {
                compareExcelFilename = openFileDialog.FileName;
                txtCompareLocaleFile.Text = compareExcelFilename;

                if (File.Exists(compareExcelFilename))
                {
                    excelConnect = new ExcelConnect(compareExcelFilename);
                    excelConnect.Connect();

                    List<CustomWorksheet> customWorksheets = excelConnect.availableSheets;

                    //  cmbAvailableSheets.BindingContext = new BindingContext();
                    cmbAvailableSheets.ItemsSource = customWorksheets;

                    cmbAvailableSheets.DisplayMemberPath = "WorkSheetName";
                    cmbAvailableSheets.SelectedValuePath = "Id";
                }

            }
        }

        private async Task LoadSheetAsync()
        {
            try
            {
                if (SelectedLanguage != null && SelectedLanguage.Count > 0)
                {
                    if (excelConnect != null)
                    {
                        if (cmbAvailableSheets.SelectedItem != null)
                        {
                            _timer4.IsEnabled = true;
                            _timer4.Start();
                            _stopwatch4.Restart();
                            _stopwatch4.Start();

                            string joinedLanguages = string.Join(",", SelectedLanguage.Select(x => $"[{x.Culture}]"));
                            bool removeExcludedKeys = chkRemoveExcludedKeys.IsChecked.Value;
                            string filter = "";


                            if (removeExcludedKeys)
                            {

                                filter = $"WHERE [KEY] NOT IN ('{(this.DataContext as SettingsObject).ExcludedKeys}')";
                            }

                            CustomWorksheet worksheet = cmbAvailableSheets.SelectedItem as CustomWorksheet;
                            if (worksheet != null)
                            {

                                bool isSCAH = false;
                                string query = string.Empty;

                                isSCAH = worksheet.WorkSheetColumns.Select(x => x.Name).Contains("KEY");


                                if (isSCAH)
                                    query = $"SELECT [KEY],{joinedLanguages}  FROM [{worksheet.WorkSheetName}] {filter} ORDER BY [KEY]";
                                else
                                    query = $"SELECT TABREF,REFID,{joinedLanguages}  FROM [{worksheet.WorkSheetName}] ORDER BY TABREF,REFID";

                                ExcelObj = excelConnect.ExecuteQuery(query);

                                await ExcelObj;

                                if (ExcelObj != null)
                                {
                                    if (ExcelObj.Result != null)
                                    {
                                        lstCompareExcelSheet.LoadData(ExcelObj.Result);
                                    }
                                }


                            }

                            _timer4.Stop();
                            _stopwatch4.Stop();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _timer4.Stop();
                _stopwatch4.Stop();
            }
        }

        private void btnLoadSheet_Click(object sender, RoutedEventArgs e)
        {
            LoadSheetAsync();
        }

        private void btnCompareExcelGenerateScript_Click(object sender, RoutedEventArgs e)
        {
            if (localeToScript != null && localeToScript.Count > 0)
            {

                if (SelectedLanguage != null && SelectedLanguage.Count > 0)
                {
                    List<string> updateQuery = new List<string>();

                    foreach (var locale in localeToScript)
                    {
                        string localeKey = locale["KEY"].ToString();

                        foreach (var language in SelectedLanguage)
                        {
                            string value = locale[$"{language.Culture}_FILE"].ToString();
                            if (string.IsNullOrEmpty(value))
                                continue;

                            string query = string.Empty;
                            if (selectedSchemaName.ToLower() == "scah")
                                query = Scripts.CreateUpdateScriptForSCAH(value, localeKey, language);
                            else
                            {
                                string tabRef = localeKey.Split("_")[0];
                                string refId = localeKey.Split("_")[1];
                                query = Scripts.CreateUpdateScriptForFSH(value, tabRef, refId, language);
                            }
                            updateQuery.Add(query);
                        }
                    }

                    string compiledQuery = string.Join(Environment.NewLine, updateQuery);
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Text File | *.txt";
                    if (saveFileDialog.ShowDialog().Value)
                    {
                        string filename = saveFileDialog.FileName;
                        Utilities.ExportFile(compiledQuery, filename);
                    }
                }

            }
        }

        private async void ExecuteCompare(RegionTenant regionTenant, string query)
        {
            if (!sourceToken.IsCancellationRequested)
            {
                await MainExecuteCompare(regionTenant, query);
            }
        }

        private async Task MainExecuteCompare(RegionTenant regionTenant, string query)
        {

            DataGrid dgGrid = lstCompareDBData.dataGrid1;


            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(regionTenant);
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;

            requestQuery.Query = query;
            lstLocale.SetTag(regionTenant.tenantId);

            _timer3.IsEnabled = true;
            _timer3.Start();
            _stopwatch3.Restart();
            _stopwatch3.Start();

            task = requestQuery.GetRequestQuery();

            lstCompareDBData.ShowLoading();

            await task;





            if (task != null)
            {
                RequestResponse _response = task.Result;
                if (_response.isSuccess)
                {
                    CustObj result = _response.CustObj as CustObj;

                    int itemCount = result.Objects.Count;

                    Utilities.ReProcessTranslation(result);


                    Utilities.ReProcessTranslation(result);
                    CustObj custObj = null;
                    if (selectedSchemaName.ToLower() == "scah")
                        custObj = Utilities.ProcessSCAHLocale(result.Objects, ExcelObj.Result.Objects, SelectedLanguage);
                    else
                        custObj = Utilities.ProcessFSHCultureInfo(result.Objects, ExcelObj.Result.Objects, SelectedLanguage);

                    lstCompareDBData.LoadData(custObj, true, rowCheckBoxCompare_Checked, rowCheckBoxCompare_Unchecked);

                    //   lstCompareDBData.LoadData(result);

                    lstCompareDBData.ShowListView();

                    lblProgress3.Content = $"Status: Done - {elapsedSeconds3}";
                }
                else
                {
                    if (_response.ErrorMessage.ToLower().Contains("cancel"))
                    {
                        lblProgress3.Content = $"Status: Cancelled - {elapsedSeconds3}";
                        lstCompareDBData.loadingControl.txtErrorName.Content = "Cancelled";

                    }
                    else
                    {
                        lblProgress3.Content = $"Status: Error - {elapsedSeconds3}";
                        lstCompareDBData.loadingControl.txtErrorName.Content = "Error";
                        lstCompareDBData.loadingControl.txtErrorDetail.Text = _response.ErrorMessage;

                    }
                    lstCompareDBData.ShowError();
                }
            }
            else
            {
                lblProgress3.Content = $"Status: Error - {elapsedSeconds3}";
                lstCompareDBData.ShowError();
                lstCompareDBData.loadingControl.txtErrorName.Content = "Error";
            }

            _timer3.IsEnabled = false;
            _timer3.Stop();
            _stopwatch3.Stop();

            btnCompare.IsEnabled = true;
            btnCompare.Content = "Compare";

        }


        private void rowCheckBoxCompare_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {
                IDictionary<string, object> locale = checkBox.DataContext as IDictionary<string, object>;
                if (locale != null)
                {
                    RunRowCompareUncheckedEvent(locale);
                }
            }
        }

        private void rowCheckBoxCompare_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            if (checkBox != null)
            {

                IDictionary<string, object> locale = checkBox.DataContext as IDictionary<string, object>;
                if (locale != null)
                {
                    RunRowCompareCheckedEvent(locale);
                }

            }
        }

        private void RunRowCompareCheckedEvent(IDictionary<string, object> locale)
        {

            if (!localeToScript.Contains(locale))
                localeToScript.Add(locale);
        }

        private void RunRowCompareUncheckedEvent(IDictionary<string, object> locale)
        {
            if (localeToScript.Contains(locale))
                localeToScript.Remove(locale);
        }

        private void SetAllCompareRowCheckBoxes(bool isChecked)
        {
            foreach (var item in lstCompareDBData.dataGrid1.Items)
            {
                if (item is IDictionary<string, object> locale)
                {
                    if (Convert.ToBoolean(locale["IsSelected"]) != isChecked)
                    {
                        locale["IsSelected"] = isChecked;
                        if (isChecked)
                            RunRowCompareCheckedEvent(locale);
                        else
                            RunRowCompareUncheckedEvent(locale);
                    }
                }
            }
            lstLanguage.dataGrid1.Items.Refresh();
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            if (lstResult.dataGrid1.SelectedItem != null)
            {
                if (lstSchema.dataGrid1.SelectedItem != null)
                {
                    if (!isCompareRunning)
                    {
                        sourceToken = new CancellationTokenSource();
                        token = sourceToken.Token;
                        btnCompare.IsEnabled = false;
                        btnCompare.Content = "Comparing...";
                        RegionTenant selectedRegionTenant = lstResult.dataGrid1.SelectedItem as RegionTenant;
                        IDictionary<string, object> selectedSchema = lstSchema.dataGrid1.SelectedItem as IDictionary<string, object>;

                        //settings
                        string removeExlcudedKeys = chkRemoveExcludedKeys.IsChecked.Value ? txtExcludedKeys.Text : string.Empty;
                        bool removeMnemonic = false;
                        bool isNewUpdateOnly = false;
                        bool isIncludeLength = false;

                        selectedSchemaName = selectedSchema["SCHEMANAME"].ToString();
                        string query = string.Empty;

                        if (selectedSchemaName.ToLower() == "scah")
                        {
                            query = Scripts.GenerateSCAHLocaleScript(SelectedLanguage, removeExlcudedKeys, removeMnemonic, isNewUpdateOnly, isIncludeLength);
                        }
                        else
                        {
                            query = Scripts.GenerateCultureInfoLocaleScript(SelectedLanguage, selectedSchemaName, isIncludeLength);
                        }

                        ExecuteCompare(selectedRegionTenant, query);
                    }
                    else
                    {
                        if (task != null)
                        {
                            sourceToken.Cancel();
                            isCompareRunning = false;

                            btnCompare.IsEnabled = true;
                            btnCompare.Content = "Compare";
                        }
                    }
                }
            }
        }

        private void lstCompareDBData_Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            SetAllCompareRowCheckBoxes(true);
        }

        private void lstCompareDBData_UnCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            SetAllCompareRowCheckBoxes(false);
        }
    }
}
