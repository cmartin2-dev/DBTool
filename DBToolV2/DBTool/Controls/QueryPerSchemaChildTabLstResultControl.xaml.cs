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
using System.Text.RegularExpressions;
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
    /// Interaction logic for QueryPerSchemaChildTabLstResultControl.xaml
    /// </summary>
    public partial class QueryPerSchemaChildTabLstResultControl : UserControl
    {
        string queryStr = string.Empty;
        RegionTenant _regionTenant = null;
        Task<RequestResponse> task = null;
        string _schema = string.Empty;

        LoadingControl loadingControl;
        DataGrid lstResult;

        public CustObj result;

        Label lblProgress;

        private DispatcherTimer _timer;
        private Stopwatch _stopwatch;
        private double elapsedSeconds;

        public QueryPerSchemaChildTabLstResultControl()
        {
            InitializeComponent();

         //   lstResult = lstViewResult.lstResult;
            loadingControl = lstViewResult.loadingControl;
            lblProgress = lstViewResult.lblProgress;
            lstResult = lstViewResult.dataGrid1;
            AddLstViewContextMenu();


            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20); // tick every second
            _timer.Tick += _timer_Tick;
            //lstResult.lstResult = lstResult;
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        public async void Excute(RegionTenant regionTenant, string query, string schema)
        {
            queryStr = query;
            _regionTenant = regionTenant;
            _schema = schema;

            ExecuteQuery();
        }

        private async void ExecuteQuery()
        {
            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(_regionTenant);
            queryStr = Regex.Replace(queryStr, "<%SCHEMA_NAME%>", _schema, RegexOptions.IgnoreCase);

            requestQuery.Query = queryStr;

            _timer.Start();
            _stopwatch.Restart();
            _stopwatch.Start();

            lstViewResult.ShowLoading();
            task = requestQuery.GetRequestQuery();

            await task;

            _timer.Stop();
            _stopwatch.Stop();

            if (task != null)
            {
                RequestResponse _response = task.Result;
                if (_response.isSuccess)
                {
                    result = _response.CustObj as CustObj;

                    int itemCount = result.Objects.Count;
                    lstViewResult.loadingControl.lblTotalCount = new Label();
                    lstViewResult.ShowListView();
                    lstViewResult.LoadData(result);
                   // lstResult.GenerateListView(result);

                    lblProgress.Content = $"Status: Done - {elapsedSeconds}";

                }
                else
                {
                    lstViewResult.ShowError();

                    lblProgress.Content = $"Status: Error - {elapsedSeconds}";
                }
            }
            else
            {
                lstViewResult.ShowError();

                lblProgress.Content = $"Status: Error - {elapsedSeconds}";
            }
        }

        private void AddLstViewContextMenu()
        {

            var globalMenu = (ContextMenu)Application.Current.Resources["SharedListViewContextMenu"];
            var newMenu = Utilities.CloneContextMenu(globalMenu, lstResult);

            lstViewResult.AddContextMenu();

            //lstResult.ContextMenu = newMenu;


        }

    }
}
