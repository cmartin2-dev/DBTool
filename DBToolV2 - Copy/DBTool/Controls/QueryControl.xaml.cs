using DBTool.Commons;
using DBTool.Connect;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using DocumentFormat.OpenXml.Spreadsheet;
using Entities;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for QueryControl.xaml
    /// </summary>
    public partial class QueryControl : UserControl
    {
        private double _fontSize = 14;   // starting font size
        private const double MinFontSize = 8;
        private const double MaxFontSize = 72;

        private bool isRunning = false;

        Task<RequestResponse> task = null;


        CancellationTokenSource sourceToken = null;
        CancellationToken token;

        HeaderEnvironment dataContextHeaderEnvironment;

        public ICollectionView FilteredQueries { get; set; }


        private ObservableCollection<TabItem> queryTabs;

        private string selectedValueCell = string.Empty;

        private bool isCell = false;


        public QueryControl()
        {
            InitializeComponent();

           

            queryTabs = new ObservableCollection<TabItem>();

            Binding binding = new Binding
            {
                Source = queryTabs
            };
            tabQueryResultControl.SetBinding(TabControl.ItemsSourceProperty, binding);

        }

        private void txtQueryControl_Execute(object sender, RoutedEventArgs e)
        {
            Execute();
        }


        private QueryListViewControl CreateTabQueryResult()
        {
            QueryListViewControl queryListControl = new QueryListViewControl();

            TabItem tab = new TabItem();
            tab.Header = $"Query {queryTabs.Count + 1}";
            tab.Content = queryListControl;

            var roundedStyle = (Style)Application.Current.Resources["ClosableTabItem"];
            tab.Style = roundedStyle;
            tab.IsSelected = true;
            // selectedRegionTenant.Region = cmbRegion.SelectedItem as Region;



            // tab.DataContext = selectedRegionTenant;

            queryTabs.Add(tab);

            return queryListControl;
        }





        private async void Execute()
        {

            var datacontext = this.DataContext;
            if (datacontext != null)
            {

                if (string.IsNullOrWhiteSpace(txtQueryControl.txtQuery.Text))
                    return;



                if (!isRunning)
                {
                    string queryStr = txtQueryControl.txtQuery.Text;
                    sourceToken = new CancellationTokenSource();
                    token = sourceToken.Token;

                    string[] batches = txtQueryControl.CleanQuery();
                    queryTabs.Clear();
                    //  tabQueryResultControl.Height = 650;

                    foreach (string querybatch in batches)
                    {
                        if (string.IsNullOrWhiteSpace(querybatch)) continue;

                        // rdoExecute.IsEnabled = false;
                        txtQueryControl.rdoExecute.Content = "Executing";
                        RegionTenant rt = this.DataContext as RegionTenant;
                        if (rt != null && rt.Region != null)
                        {
                            isRunning = true;



                            if (!sourceToken.IsCancellationRequested)
                            {
                                await MainExecute(querybatch, CreateTabQueryResult());
                            }

                        }
                        //  rdoExecute.IsEnabled = true;

                    }



                    isRunning = false;
                    txtQueryControl.rdoExecute.Content = "▶️ Execute";
                }
                else
                {
                    if (task != null)
                    {
                        sourceToken.Cancel();
                        isRunning = false;
                        //   lblProgress.Content = $"Status: Cancelled - {elapsedSeconds}";

                        txtQueryControl.rdoExecute.Content = "▶️ Execute";
                    }
                }
            }

        }

         private async Task MainExecute(string query, QueryListViewControl queryListViewControl)
        {
            QueryListViewControl currentList = queryListViewControl;



           // var lstViewResult = currentList.lstResult.dataGrid1;
           // lstViewResult.Tag = (this.DataContext as RegionTenant).tenantId;
            var lblTotalCount = currentList.lblTotalCount;
            var lblProgress = currentList.lblProgress;
            var elapsedSeconds = currentList.elapsedSeconds;

            ListViewResultControl listViewResultControl = currentList.lstResult;

            DataGrid dgGrid = currentList.lstResult.dataGrid1;


            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(this.DataContext as RegionTenant);
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;

            requestQuery.Query = query;
            listViewResultControl.SetTag((this.DataContext as RegionTenant).tenantId);

            currentList._timer.IsEnabled = true;
            currentList._timer.Start();
            currentList._stopwatch.Restart();
            currentList._stopwatch.Start();

            task = requestQuery.GetRequestQuery();

            currentList.lstResult.ShowLoading();

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

                    listViewResultControl.LoadData(result);
                     
                        currentList.lstResult.ShowListView();
                        lblTotalCount.Content = $"Total Count : {itemCount}";
                    //}
                    //else
                    //{
                    //    int count = result.Objects.Count > result.RowsAffected ? result.Objects.Count : result.RowsAffected;
                    //    lblTotalCount.Content = $"Total Count : {count}";
                    //    currentList.loadingControl.ShowDone();
                    //    currentList.loadingControl.txtRowsAffected.Text = $"{count} rows affected.";

                    //}

                    lblProgress.Content = $"Status: Done - {currentList.elapsedSeconds}";
                }
                else
                {
                    if (_response.ErrorMessage.ToLower().Contains("cancel"))
                    {
                        lblProgress.Content = $"Status: Cancelled - {currentList.elapsedSeconds}";
                        currentList.loadingControl.txtErrorName.Content = "Cancelled";

                    }
                    else
                    {
                        lblProgress.Content = $"Status: Error - {currentList.elapsedSeconds}";
                        currentList.loadingControl.txtErrorName.Content = "Error";
                        currentList.loadingControl.txtErrorDetail.Text = _response.ErrorMessage;

                    }
                    currentList.lstResult.ShowError();
                }
            }
            else
            {
                lblProgress.Content = $"Status: Error - {currentList.elapsedSeconds}";
                currentList.lstResult.ShowError();
                currentList.loadingControl.txtErrorName.Content = "Error";
            }

            currentList._timer.IsEnabled = false;
            currentList._timer.Stop();
            currentList._stopwatch.Stop();

        }



    }
}
