using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
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
using System.Xml;
using System.Windows.Forms;
using Entities;
using UserControl = System.Windows.Controls.UserControl;
using DBTool.Commons;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using System.Diagnostics;
using DBTool.Connect;
using Newtonsoft.Json.Linq;
using MessageBox = System.Windows.MessageBox;
using TabControl = System.Windows.Controls.TabControl;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for QueryPerSchemaControl.xaml
    /// </summary>
    public partial class QueryPerSchemaControl : UserControl
    {

        private double _fontSize = 14;   // starting font size
        private const double MinFontSize = 8;
        private const double MaxFontSize = 72;
        public QueryPerSchemaControl()
        {

            InitializeComponent();

        }

        public void AddTab(RegionTenant regionTenant)
        {
            TabItem tabItem = new TabItem();
            tabItem.Header = regionTenant.tenantId;

            var roundedStyle = (Style)System.Windows.Application.Current.Resources["BaseTabItem"];
            tabItem.Style = roundedStyle;

            QueryPerSchemaChildTabControl childTabControl = new QueryPerSchemaChildTabControl();

            //  childTabControl.lstResult.lstResult.Tag = tabItem.Header;

            childTabControl.DataContext = regionTenant;

            childTabControl.FetchDetail(regionTenant);

            tabItem.Content = childTabControl;

            bool exists = schemaTab.Items
    .OfType<TabItem>()
    .Any(tab => tab.Header?.ToString() == tabItem.Header);

            if (!exists)
            {

                schemaTab.Items.Add(tabItem);
                schemaTab.SelectedItem = tabItem;
            }
        }

        public void RemoveTab(RegionTenant tenant)
        {
            TabItem tabItem = schemaTab.Items.OfType<TabItem>().FirstOrDefault(t => t.Header?.ToString() == tenant.tenantId);

            if (tabItem != null)
            {
                schemaTab.Items.Remove(tabItem);
            }
            TabItem tabItemResult = tabResult.Items.OfType<TabItem>().FirstOrDefault(t => t.Header?.ToString() == tenant.tenantId);

            if (tabItemResult != null)
            {
                tabResult.Items.Remove(tabItemResult);
            }
        }

        public void ClearQueryPerSchema()
        {
            schemaTab.Items.Clear();
            tabResult.Items.Clear();
            txtQueryControl.txtQuery.Text = string.Empty;
        }

        private void rdoExecute_Click(object sender, RoutedEventArgs e)
        {

        }

        private TabItem CreateTabResult(TabItem itemTab, string query)
        {
            TabItem tenantTab = new TabItem() { Header = itemTab.Header };
            QueryPerSchemaChildTabControl child = itemTab.Content as QueryPerSchemaChildTabControl;

            TabControl tenantTabControl = new TabControl();

            var roundedStyle = (Style)System.Windows.Application.Current.Resources["BaseTabItem"];
            tenantTab.Style = roundedStyle;

            RegionTenant regionTenant = child.DataContext as RegionTenant;

            if (chkSCAH.IsChecked == true)
            {
                TabItem tabItem = new TabItem() { Header = "SCAH" };
                tabItem.Style = roundedStyle;

                QueryPerSchemaChildTabLstResultControl queryPerSchemaChildTabLstResultControl = new QueryPerSchemaChildTabLstResultControl();
                queryPerSchemaChildTabLstResultControl.lstViewResult.SetTag(regionTenant.tenantId + "_SCAH");
                tabItem.Content = queryPerSchemaChildTabLstResultControl;

                //    queryPerSchemaChildTabLstResultControl.lstViewResult.lstResult.Tag = $"{itemTab.Header}_{schema}_";

                queryPerSchemaChildTabLstResultControl.Excute(regionTenant, query, "SCAH");

                tenantTabControl.Items.Add(tabItem);
            }
            else
            {

                foreach (string schema in child.SelectedSchemas)
                {
                    TabItem tabItem = new TabItem() { Header = schema };
                    tabItem.Style = roundedStyle;

                    QueryPerSchemaChildTabLstResultControl queryPerSchemaChildTabLstResultControl = new QueryPerSchemaChildTabLstResultControl();
                    queryPerSchemaChildTabLstResultControl.lstViewResult.SetTag(regionTenant.tenantId + "_" + schema);
                    tabItem.Content = queryPerSchemaChildTabLstResultControl;

                    //    queryPerSchemaChildTabLstResultControl.lstViewResult.lstResult.Tag = $"{itemTab.Header}_{schema}_";

                    queryPerSchemaChildTabLstResultControl.Excute(regionTenant, query, schema);

                    tenantTabControl.Items.Add(tabItem);
                }
            }

            tenantTab.Content = tenantTabControl;

            return tenantTab;
        }

        private void txtQueryControl_Execute(object sender, RoutedEventArgs e)
        {
            tabResult.Items.Clear();

            string query = txtQueryControl.txtQuery.Text;

            if (string.IsNullOrWhiteSpace(query))
                return;

            if (schemaTab.Items != null && schemaTab.Items.Count > 0)
            {
                foreach (var item in schemaTab.Items)
                {
                    TabItem itemTab = item as TabItem;
                    if (itemTab != null)
                    {
                        QueryPerSchemaChildTabControl child = itemTab.Content as QueryPerSchemaChildTabControl;
                        if (child != null)
                        {
                            if (child.SelectedSchemas.Count == 0 && chkSCAH.IsChecked == false)
                            {
                                ThemedDialog.Show("Please select at least 1 schema per environment", "Error");
                                return;
                            }
                            else
                            {

                                TabItem tab = CreateTabResult(itemTab, query);
                                tabResult.Items.Add(tab);
                                tabResult.SelectedItem = tab;

                            }
                        }
                    }
                }
            }
        }



        private void chkSelectAllSchemas_Checked(object sender, RoutedEventArgs e)
        {
            SelectAllSchemasInAllTabs(true);
        }

        private void chkSelectAllSchemas_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectAllSchemasInAllTabs(false);
        }

        private void SelectAllSchemasInAllTabs(bool select)
        {
            if (schemaTab.Items == null) return;

            foreach (var item in schemaTab.Items)
            {
                if (item is TabItem tabItem && tabItem.Content is QueryPerSchemaChildTabControl child)
                {
                    // Select/deselect all schemas in this tenant's grid
                    foreach (var gridItem in child.lstResult.dataGrid1.Items)
                    {
                        if (gridItem is IDictionary<string, object> dictItem)
                        {
                            dictItem["IsSelected"] = select ? 1 : 0;
                            string schema = dictItem.ContainsKey("SCHEMANAME") ? dictItem["SCHEMANAME"]?.ToString() : null;
                            if (!string.IsNullOrEmpty(schema))
                            {
                                if (select && !child.SelectedSchemas.Contains(schema))
                                    child.SelectedSchemas.Add(schema);
                                else if (!select)
                                    child.SelectedSchemas.Remove(schema);
                            }
                        }
                    }
                    child.lstResult.dataGrid1.Items.Refresh();
                }
            }
        }

        private void btnExtractReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tabResult == null || tabResult.Items == null || tabResult.Items.Count == 0)
                {
                    ThemedDialog.Show("No results to export.", "Export");
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel File|*.xlsx",
                    FileName = $"QueryReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() != true) return;

                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Report");
                    int currentRow = 1;
                    bool headerWritten = false;
                    int headerColOffset = 2; // Tenant + Schema columns

                    foreach (var item in tabResult.Items)
                    {
                        if (item is TabItem maintab && maintab.Content is TabControl tabControl1)
                        {
                            string tenantName = maintab.Header?.ToString() ?? "";

                            foreach (TabItem schemaTab in tabControl1.Items)
                            {
                                string schemaName = schemaTab.Header?.ToString() ?? "";

                                if (schemaTab.Content is QueryPerSchemaChildTabLstResultControl schemaControl)
                                {
                                    var grid = schemaControl.lstViewResult.dataGrid1;
                                    string json = Utilities.ExportToJson(grid);
                                    var dt = Utilities.JsonToDataTable(json);

                                    if (dt == null || dt.Rows.Count == 0) continue;

                                    // Write header row once
                                    if (!headerWritten)
                                    {
                                        ws.Cell(currentRow, 1).Value = "Tenant";
                                        ws.Cell(currentRow, 2).Value = "Schema";
                                        for (int c = 0; c < dt.Columns.Count; c++)
                                        {
                                            ws.Cell(currentRow, c + headerColOffset + 1).Value = dt.Columns[c].ColumnName;
                                        }

                                        // Style header
                                        var headerRange = ws.Range(currentRow, 1, currentRow, dt.Columns.Count + headerColOffset);
                                        headerRange.Style.Font.Bold = true;
                                        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1F2937");
                                        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

                                        currentRow++;
                                        headerWritten = true;
                                    }

                                    // Write data rows
                                    foreach (System.Data.DataRow row in dt.Rows)
                                    {
                                        ws.Cell(currentRow, 1).Value = tenantName;
                                        ws.Cell(currentRow, 2).Value = schemaName;
                                        for (int c = 0; c < dt.Columns.Count; c++)
                                        {
                                            ws.Cell(currentRow, c + headerColOffset + 1).Value = row[c]?.ToString() ?? "";
                                        }
                                        currentRow++;
                                    }
                                }
                            }
                        }
                    }

                    // Auto-fit columns
                    ws.Columns().AdjustToContents();

                    // Add filters
                    if (currentRow > 2)
                        ws.Range(1, 1, currentRow - 1, ws.LastColumnUsed().ColumnNumber()).SetAutoFilter();

                    workbook.SaveAs(saveDialog.FileName);
                }

                ThemedDialog.Show("Report exported successfully.", "Export");
            }
            catch (Exception ex)
            {
                ThemedDialog.Show($"Export failed: {ex.Message}", "Error");
            }
        }

        private void btnExtract_Click(object sender, RoutedEventArgs e)
        {

            List<ExportFileObject> exportFiles = new List<ExportFileObject>();

            if (tabResult != null && tabResult.Items != null && tabResult.Items.Count > 0)
            {
                foreach (var item in tabResult.Items)
                {
                    try
                    {
                        if (item is TabItem maintab)
                        {
                            if (maintab.Content is TabControl tabControl1)
                            {
                                if (tabControl1.Items != null && tabControl1.Items.Count > 0)
                                {
                                    foreach (TabItem tabControlTabItem in tabControl1.Items)
                                    {
                                        if (tabControlTabItem.Content is QueryPerSchemaChildTabLstResultControl schemaControl)
                                        {
                                            ExportFileObject exportFileObject = new ExportFileObject();
                                            exportFileObject.TableName = schemaControl.lstViewResult.dataGrid1.Tag.ToString();
                                            exportFileObject.JSON = Utilities.ExportToJson(schemaControl.lstViewResult.dataGrid1);
                                            exportFileObject.DataTable = Utilities.JsonToDataTable(exportFileObject.JSON);

                                            exportFiles.Add(exportFileObject);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { continue; }


                }




                if (exportFiles != null && exportFiles.Count > 0)
                {
                    Utilities.ExportMultipleGridToExcel(exportFiles);
                }
            }

        }
    }
}
