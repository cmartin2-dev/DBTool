using DBTool.Commons;
using Entities;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for QueryListViewControl.xaml
    /// </summary>
    public partial class QueryListViewControl : UserControl
    {
        public DispatcherTimer _timer;
        public Stopwatch _stopwatch;
        public double elapsedSeconds;
        public DataGrid lstViewResult;
        public Label lblProgress;
        public Label lblTotalCount;
        public LoadingControl loadingControl;


        public QueryListViewControl()
        {
            InitializeComponent();

            lblProgress = lstResult.lblProgress;
            lblTotalCount = lstResult.lblTotalCount;
            loadingControl = lstResult.loadingControl;
            lstViewResult = lstResult.dataGrid1;

            AddLstViewContextMenu();


            loadingControl.lblTotalCount = lblTotalCount;


            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20); 
            _timer.Tick += _timer_Tick;
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

             lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        private void btnExportToJson_Click(object sender, RoutedEventArgs e)
        {
            ExportFile(true);
        }

        private void btnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportFile(false);
        }

        private void ExportFile(bool isJson = true)
        {
            try
            {
                RegionTenant regionTenant = this.DataContext as RegionTenant;

                if (lstViewResult.Items.Count > 0)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "JSON File|*.json";
                    if (isJson)
                        saveFileDialog.FileName = $"{regionTenant.tenantId}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.json";
                    else
                        saveFileDialog.FileName = $"{regionTenant.tenantId}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.xlsx";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                        {
                            var custom = lstViewResult.Items.OfType<Dictionary<string, object>>().ToList();
                            string fileName = saveFileDialog.FileName;
                            if (isJson)
                                Utilities.ExportToJsonFile(custom, fileName);
                            else
                                Utilities.ExportToExcelFile(custom, fileName);


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThemedDialog.Show(ex.Message);
            }
        }

        private void CopySelectedRow()
        {
            try
            {
                if (lstViewResult.SelectedItems != null && lstViewResult.SelectedItems.Count > 0)
                {

                    string jsonString = JsonConvert.SerializeObject(lstViewResult.SelectedItems, Newtonsoft.Json.Formatting.Indented);

                    jsonString = Utilities.BeautifyJson(jsonString);

                    Clipboard.SetText(jsonString);
                }
            }
            catch (Exception ex) { ThemedDialog.Show(ex.Message); }
        }

        private void GenerateInsertScript()
        {
            try
            {
                if (lstViewResult.SelectedItems != null && lstViewResult.SelectedItems.Count > 0)
                {
                    Dictionary<string, object> item = lstViewResult.SelectedItems[0] as Dictionary<string, object>;
                    StringBuilder sbColumns = new StringBuilder();
                    StringBuilder sbData = new StringBuilder();

                    sbColumns.Append("(");
                    sbData.Append("(");
                    if (item != null)
                    {
                        foreach (var itemObject in item)
                        {
                            if (itemObject.Key.ToLower() == "rowversion")
                                continue;

                            sbColumns.Append(itemObject.Key + ",");
                            sbData.Append($"{Utilities.ConvertDatatoString(itemObject.Value)},");
                        }

                    }
                    sbData.Remove(sbData.ToString().Length - 1, 1);
                    sbData.Append(")");
                    sbColumns.Remove(sbColumns.ToString().Length - 1, 1);
                    sbColumns.Append(")");

                    StringBuilder combined = new StringBuilder();
                    combined.Append($"{sbColumns.ToString()}{Environment.NewLine}");
                    combined.Append($"VALUES {sbData.ToString()}");

                    Clipboard.SetText(combined.ToString());

                }
            }
            catch (Exception ex) { ThemedDialog.Show(ex.Message); }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                if (item.Name.ToLower() == "copyrow")
                    CopySelectedRow();
                else if (item.Name.ToLower() == "generateinsert")
                    GenerateInsertScript();
            }

        }

        private void AddLstViewContextMenu()
        {
            var cmCopyRow = new MenuItem { Name = "CopyRow", Header = "Copy Row" };
            var cmGenerateInsert = new MenuItem { Name = "GenerateInsert", Header = "Generate Insert" };

            cmCopyRow.Click += MenuItem_Click;
            cmGenerateInsert.Click += MenuItem_Click;

            lstResult.AddContextMenu(cmCopyRow);

            //var globalMenu = (ContextMenu)Application.Current.Resources["SharedListViewContextMenu"];
            //var newMenu = Utilities.CloneContextMenu(globalMenu, lstViewResult);


            //newMenu.Items.Add(cmCopyRow);
            //newMenu.Items.Add(cmGenerateInsert);

            //lstViewResult.ContextMenu = newMenu;


        }


    }

    
}
