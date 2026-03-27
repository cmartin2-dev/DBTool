using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using System.Text.Json;
using Entities;
using Newtonsoft.Json;
using System.Data;
using ClosedXML.Excel;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;
using System.Windows;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;
using System.Windows.Data;
using System.Collections;
using System.Reflection;
using System.Dynamic;
using System.Windows.Media;
using JsonSerializer = System.Text.Json.JsonSerializer;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.InkML;

namespace DBTool.Commons
{
    public class Utilities
    {

        private static bool isCell;
        private static string selectedCellValue;
        private static ListView currentListView;
        private static DataGrid currentDataGrid;
        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static string BeautifyJson(string json)
        {
            JsonDocument document = JsonDocument.Parse(json);
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            document.WriteTo(writer);

            writer.Flush();



            string finalResult = Encoding.UTF8.GetString(stream.ToArray());
            finalResult = finalResult.Replace("\\\"", @"""");


            return finalResult;
        }

        public static string ConvertDatatoString(object obj)
        {
            string returnObj = string.Empty;
            switch (obj)
            {
                case string _ when obj.GetType() == typeof(string):
                case DateTime when obj.GetType() == typeof(DateTime):
                    return returnObj = $"'{obj}'";
                default:

                    return obj != null ? obj.ToString() : "null";
            }
        }

        public static void ExportToJsonFile(List<Dictionary<string, object>> custom, string filename)
        {

            string json = ProcessExport(custom);
            SaveFile(filename, json);
            string pathName = System.IO.Path.GetDirectoryName(filename);
            //   Process.Start($"{pathName}");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = pathName,
                UseShellExecute = true,
                Verb = "open"
            });

        }

        public static void SaveExportedFile(string filename, string fileContent)
        {
            SaveFile(filename, fileContent);
            string pathName = System.IO.Path.GetDirectoryName(filename);
            //   Process.Start($"{pathName}");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = pathName,
                UseShellExecute = true,
                Verb = "open"
            });

        }

        public static void SaveExportedFileExcel(DataTable dt, string filename)
        {
            ExportToExcel(dt, filename);

            string pathName = System.IO.Path.GetDirectoryName(filename);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = pathName,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public static void ExportToJsonFile(IEnumerable custom, string filename)
        {

            string json = ProcessExport(custom);
            SaveFile(filename, json);
            string pathName = System.IO.Path.GetDirectoryName(filename);
            //   Process.Start($"{pathName}");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = pathName,
                UseShellExecute = true,
                Verb = "open"
            });

        }


        public static void ExportToExcelFile(IEnumerable custom, string filename)
        {
            DataTable dt = null;

            if (custom is List<RegionTenant>)
            {
                //  DataTable dt = new DataTable();// ToDataTable(custom);// ConvertToDataTable(custom);
                List<RegionTenant> list = custom as List<RegionTenant>;


                dt = ToDataTable(list.Select(x => new { TenantName = x.tenantId }).ToList());
            }
            else
            {
                if (custom is List<Dictionary<string, object>>)
                {
                    dt = ConvertToDataTable(custom as List<Dictionary<string, object>>);
                }

                if (dt != null)
                {
                    ExportToExcel(dt, filename);


                    string pathName = System.IO.Path.GetDirectoryName(filename);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = pathName,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }

            if (dt == null)
            {
                DBTool.Controls.ThemedDialog.Show("Unable to export data.", "Error");
            }
        }

        private static string ProcessExport(List<Dictionary<string, object>> custom)
        {
            string final = string.Empty;

            if (custom != null && custom.Count > 0)
            {
                JsonSerializerSettings jsonSettings = new JsonSerializerSettings();
                jsonSettings.CheckAdditionalContent = true;

                var keysToSkip = new HashSet<string> { "RowNumber" };

                // Create a JObject without those keys
                var filtered = custom.Select(a => new JObject(a.Where(x => !keysToSkip.Contains(x.Key))
                    .Select(x => new JProperty(x.Key, x.Value)

                    ))).ToList();

                string jsonString = JsonConvert.SerializeObject(filtered, jsonSettings);

                final = BeautifyJson(jsonString);
            }

            return final;
        }

        private static string ProcessExport(IEnumerable custom)
        {
            string final = string.Empty;

            JsonSerializerSettings jsonSettings = new JsonSerializerSettings();
            jsonSettings.CheckAdditionalContent = true;

            if (custom is List<RegionTenant>)
            {
                List<RegionTenant> regionTenants = custom as List<RegionTenant>;

                var tenants = regionTenants.Select(x => new { TenantNme = x.tenantId }).ToList();
                string jsonString = JsonConvert.SerializeObject(tenants, jsonSettings);

                final = BeautifyJson(jsonString);
            }
            else if (custom is List<Region>)
            {
                List<Region> region = custom as List<Region>;

                var tenants = region.Select(x => new { TenantNme = x.RegionName }).ToList();
                string jsonString = JsonConvert.SerializeObject(tenants, jsonSettings);

                final = BeautifyJson(jsonString);
            }
            else
            {
                if (custom is List<Dictionary<string, object>>)
                {

                    List<Dictionary<string, object>> dict = custom as List<Dictionary<string, object>>;

                    if (custom != null && dict.Count > 0)
                    {


                        var keysToSkip = new HashSet<string> { "RowNumber", "metadata" };

                        // Create a JObject without those keys
                        var filtered = dict.Select(a => new JObject(a.Where(x => !keysToSkip.Contains(x.Key))
                            .Select(x => new JProperty(x.Key, x.Value)

                            ))).ToList();

                        string jsonString = JsonConvert.SerializeObject(filtered, jsonSettings);

                        final = BeautifyJson(jsonString);
                    }
                }
            }


            return final;
        }

        private static void SaveFile(string filename, string content)
        {

            File.WriteAllText(filename, content,Encoding.UTF8);
        }

        public static void ExportToExcelFile(List<Dictionary<string, object>> custom, string filename)
        {
            DataTable dt = ConvertToDataTable(custom);
            ExportToExcel(dt, filename);


            string pathName = System.IO.Path.GetDirectoryName(filename);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = pathName,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private static DataTable ConvertToDataTable(List<Dictionary<string, object>> jsonList)
        {
            DataTable dataTable = new DataTable();



            if (jsonList != null)
            {
                // Add columns
                foreach (var field in jsonList[0])
                {
                    if (field.Key.ToLower() == "rownumber" || field.Key.ToLower() == "metadata")
                        continue;

                    dataTable.Columns.Add(field.Key);
                }

                // Add rows
                foreach (var item in jsonList)
                {
                    var row = dataTable.NewRow();
                    foreach (var field in item)
                    {
                        if (field.Key.ToLower() == "rownumber" || field.Key.ToLower() == "metadata")
                            continue;
                        row[field.Key] = field.Value;
                    }
                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }

        public static DataTable ToDataTable<T>(IEnumerable<T> data)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var table = new DataTable();
            foreach (var prop in properties)
            {
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (var item in data)
            {
                var values = new object[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    values[i] = properties[i].GetValue(item) ?? DBNull.Value;
                }
                table.Rows.Add(values);
            }

            return table;
        }

        private static void ExportToExcel(DataTable dataTable, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Exported");
                worksheet.Cell(1, 1).InsertTable(dataTable);
                workbook.SaveAs(filePath);
            }
        }

        public static void ExportMultipleGridToExcel(List<ExportFileObject> exportFiles)
        {
            if (exportFiles != null && exportFiles.Count > 0)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.FileName = $"Compiled_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.xlsx";

                if (saveFileDialog.ShowDialog() == true)
                {
                    if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                    {

                        //  var custom = sourceCollection;// lstViewResult.Items.OfType<Dictionary<string, object>>().ToList();
                        string fileName = saveFileDialog.FileName;
                        string fileContent = string.Empty;

                        ExportMultipleToExcel(exportFiles, fileName);



                    }
                }
            }

        }

        private static void ExportMultipleToExcel(List<ExportFileObject> exportFiles, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {

                foreach (var exportFile in exportFiles)
                {
                    var worksheet = workbook.Worksheets.Add(exportFile.TableName);
                    worksheet.Cell(1, 1).InsertTable(exportFile.DataTable);
                }


                workbook.SaveAs(filePath);
            }
        }



        public static ContextMenu CloneContextMenu(ContextMenu original, ListView parentListView)
        {

            var clone = new ContextMenu();
            if (original != null)
            {
                foreach (var item in original.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        clone.Items.Add(CloneMenuItem(menuItem));
                    }
                    else
                    {
                        // Non-MenuItem entries (like separators)
                        clone.Items.Add(item);
                    }
                }
            }

            parentListView.PreviewMouseRightButtonDown += ParentListView_PreviewMouseRightButtonDown;
            return clone;


            //var clone = new ContextMenu();
            //if (original != null)
            //{
            //    foreach (var item in original.Items)
            //    {
            //        if (item is MenuItem menuItem)
            //        {
            //            // Deep clone each MenuItem
            //            var newItem = new MenuItem
            //            {
            //                Header = menuItem.Header,
            //                Command = menuItem.Command,
            //                CommandParameter = menuItem.CommandParameter,

            //            };

            //            newItem.Click += NewItem_Click;

            //            clone.Items.Add(newItem);
            //        }
            //    }

            //    parentListView.PreviewMouseRightButtonDown += ParentListView_PreviewMouseRightButtonDown;
            //}
            //return clone;
        }
        public static ContextMenu CloneContextMenu(ContextMenu original, DataGrid parentListView)
        {

            var clone = new ContextMenu();
            if (original != null)
            {
                foreach (var item in original.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        clone.Items.Add(CloneMenuItem(menuItem));
                    }
                    else
                    {
                        // Non-MenuItem entries (like separators)
                        clone.Items.Add(item);
                    }
                }
            }

            parentListView.PreviewMouseRightButtonDown += ParentListView_PreviewMouseRightButtonDown;
            return clone;

        }

        private static MenuItem CloneMenuItem(MenuItem original)
        {
            var newItem = new MenuItem
            {
                Name = original.Name,
                Header = original.Header,
                Command = original.Command,
                Tag = original.Tag,
                CommandParameter = original.CommandParameter,
                Icon = original.Icon, // clone if needed
                IsEnabled = original.IsEnabled
            };

            if (original != null)
            {
                if (original.Items != null && original.Items.Count > 0)
                {
                    // 🔹 Clone submenus recursively
                    foreach (var child in original.Items)
                    {
                        if (child is MenuItem childMenuItem)
                            newItem.Items.Add(CloneMenuItem(childMenuItem));
                        else
                            newItem.Items.Add(child);
                    }
                }
                else
                {
                    newItem.Click += NewItem_Click;
                }

            }
            return newItem;
        }

        private static void ParentListView_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentListView = sender as ListView;
            if (currentListView != null)
            {
                TextBlock selectedCell = currentListView.InputHitTest(e.GetPosition(currentListView)) as TextBlock;
                if (selectedCell != null)
                {
                    isCell = true;
                    selectedCellValue = selectedCell.Text;
                }
                else
                {
                    isCell = false;
                }
            }
            else
            {
                currentDataGrid = sender as DataGrid;
                if (currentDataGrid != null)
                {
                    TextBlock selectedCell = currentDataGrid.InputHitTest(e.GetPosition(currentDataGrid)) as TextBlock;
                    if (selectedCell != null)
                    {
                        isCell = true;
                        selectedCellValue = selectedCell.Text;
                    }
                    else
                    {
                        isCell = false;
                    }
                }
            }
        }

        private static void NewItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is MenuItem btn)
            {
                MenuItem item = sender as MenuItem;

                var contextMenu = item.Parent as ContextMenu
                         ?? (item.Parent as MenuItem)?.Parent as ContextMenu;

                if (contextMenu != null)
                {
                    var parentControl = contextMenu.PlacementTarget;

                    if (item.Name.ToLower() == "cmcopyvalue")
                    {
                        if (!string.IsNullOrEmpty(selectedCellValue))
                            Clipboard.SetText(selectedCellValue);
                    }
                    else if (item.Name.ToLower() == "cmexportjson" || item.Name.ToLower() == "cmexportexcel")
                    {
                        if (item.Tag.ToString().ToLower() == "json")
                        {
                            ExportFile(parentControl, true);
                        }
                        else
                        {
                            ExportFile(parentControl, false);
                        }
                        //if (currentListView.ItemsSource is CollectionView)
                        //{
                        //    var sourceCollection = (currentListView.ItemsSource as CollectionView).SourceCollection;

                        //    if (item.Tag.ToString().ToLower() == "json")
                        //        Utilities.ExportFile(true, sourceCollection);
                        //    else if (item.Tag.ToString().ToLower() == "excel")
                        //        Utilities.ExportFile(false, sourceCollection);
                        //}
                        //else
                        //{
                        //    var sourceCollection = currentListView.Items.OfType<Dictionary<string, object>>().ToList(); //(currentListView.ItemsSource as CollectionView).SourceCollection;

                        //    if (item.Tag.ToString().ToLower() == "json")
                        //        Utilities.ExportFile(true, sourceCollection);
                        //    else if (item.Tag.ToString().ToLower() == "excel")
                        //        Utilities.ExportFile(false, sourceCollection);
                        //}
                    }
                }

            }
        }

        public static List<dynamic> ToDynamicList(List<Dictionary<string, object>> list)
        {
            var result = new List<dynamic>();

            foreach (var dict in list)
            {
                IDictionary<string, object> expando = new ExpandoObject();
                foreach (var kvp in dict)
                {
                    expando[kvp.Key] = kvp.Value;
                }
                result.Add(expando);
            }

            return result;
        }

        public static void ExportFile(string content, string filename)
        {
            SaveFile(filename, content);
            string pathName = System.IO.Path.GetDirectoryName(filename);
            //   Process.Start($"{pathName}");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = pathName,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public static void ExportFile(bool isJson = true, IEnumerable sourceCollection = null)
        {
            try
            {
                if (sourceCollection != null)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    if (isJson)
                    {
                        saveFileDialog.Filter = "JSON File|*.json";
                        saveFileDialog.FileName = $"{currentListView.Tag}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.json";
                    }
                    else
                    {
                        saveFileDialog.Filter = "Excel File|*.xlsx";
                        saveFileDialog.FileName = $"{currentListView.Tag}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.xlsx";
                    }

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                        {
                            var custom = sourceCollection;// lstViewResult.Items.OfType<Dictionary<string, object>>().ToList();
                            string fileName = saveFileDialog.FileName;
                            if (isJson)
                                Utilities.ExportToJsonFile(custom, fileName);
                            //else
                            Utilities.ExportToExcelFile(custom, fileName);


                        }
                    }
                }
                //RegionTenant regionTenant = this.DataContext as RegionTenant;

                //if (lstViewResult.Items.Count > 0)
                //{
                //    SaveFileDialog saveFileDialog = new SaveFileDialog();
                //    saveFileDialog.Filter = "JSON File|*.json";
                //    if (isJson)
                //        saveFileDialog.FileName = $"{regionTenant.tenantId}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.json";
                //    else
                //        saveFileDialog.FileName = $"{regionTenant.tenantId}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.xlsx";

                //    if (saveFileDialog.ShowDialog() == true)
                //    {
                //        if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                //        {
                //            var custom = lstViewResult.Items.OfType<Dictionary<string, object>>().ToList();
                //            string fileName = saveFileDialog.FileName;
                //            if (isJson)
                //                Utilities.ExportToJsonFile(custom, fileName);
                //            else
                //                Utilities.ExportToExcelFile(custom, fileName);


                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                DBTool.Controls.ThemedDialog.Show(ex.Message);
            }
        }

        public static void ExportFile(UIElement uIElement, bool isJson = true)
        {
            try
            {
                DataGrid selectedGrid = uIElement as DataGrid;
                ListView lstView = uIElement as ListView;

                SaveFileDialog saveFileDialog = new SaveFileDialog();

                if (isJson)
                {
                    saveFileDialog.Filter = "JSON File|*.json";
                    if (lstView != null)
                        saveFileDialog.FileName = $"{lstView.Tag}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.json";
                    else
                        saveFileDialog.FileName = $"{selectedGrid.Tag}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.json";
                }
                else
                {
                    saveFileDialog.Filter = "Excel File|*.xlsx";
                    if (lstView != null)
                        saveFileDialog.FileName = $"{lstView.Tag}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.xlsx";
                    else
                        saveFileDialog.FileName = $"{selectedGrid.Tag}_Data_Result_{DateTime.Now.ToString("MMddyyHHmmsstt")}.xlsx";

                }
                if (saveFileDialog.ShowDialog() == true)
                {
                    if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                    {

                        //  var custom = sourceCollection;// lstViewResult.Items.OfType<Dictionary<string, object>>().ToList();
                        string fileName = saveFileDialog.FileName;
                        string fileContent = string.Empty;
                        if (isJson)
                        {
                            if (lstView != null)
                                fileContent = ExportToJson(lstView);
                            else
                                fileContent = ExportToJson(selectedGrid);
                            Utilities.SaveExportedFile(fileName, fileContent);
                        }
                        else
                        {
                            if (lstView != null)
                                fileContent = ExportToJson(lstView);
                            else
                                fileContent = ExportToJson(selectedGrid);

                            DataTable dt = JsonToDataTable(fileContent);
                            Utilities.SaveExportedFileExcel(dt, fileName);
                        }


                    }
                }
            }
            catch (Exception ex)
            {
                DBTool.Controls.ThemedDialog.Show(ex.Message);
            }
        }



        public static void CheckAccess(RegionTenant tabRegionTenant)
        {
            if (tabRegionTenant != null && tabRegionTenant.Region != null)
            {
                if (tabRegionTenant.Region.RegionEndPoint.ToLower().Contains("prd-euc1") ||
                    tabRegionTenant.Region.RegionEndPoint.ToLower().Contains("prd-use1"))
                {
                    if (!StaticFunctions.AppConnection.settingsObject.IsFullAccess)
                    {
                        StaticFunctions.AppConnection.settingsObject.CheckAccess = false;
                    }
                }
                else
                {
                    StaticFunctions.AppConnection.settingsObject.CheckAccess = true; //StaticFunctions.forTestingEnvironmentsFullAccess;
                                                                                     // lblStatusIndicator.Text = "Admin";
                }
            }
        }

        public static string ExportToJson(ListView listView)
        {
            if (!(listView.View is GridView gridView)) return "[]";

            var rows = new List<Dictionary<string, object>>();

            foreach (var item in listView.Items)
            {
                var row = new Dictionary<string, object>();

                foreach (var column in gridView.Columns)
                {
                    if (column.Header is ContentControl && ((ContentControl)column.Header).Content is CheckBox)
                        continue;

                    string header = column.Header is ContentControl ? ((ContentControl)column.Header).Content.ToString() : "";
                    //string header = column.Header?.ToString() ?? "";
                    if (header == "#")
                        continue;

                    object? value = null;



                    if (column.DisplayMemberBinding is Binding binding)
                    {
                        if ((item is Dictionary<string, object> dict))
                        {
                            string key = binding.Path.Path.Replace("[", "").Replace("]", ""); // key from dictionary
                            if (dict.TryGetValue(key, out var v))
                                value = v ?? "";
                        }
                        else
                        {
                            // Get property by path (e.g. "Name" or nested "Address.City")
                            var prop = item.GetType().GetProperty(binding.Path.Path);
                            if (prop != null)
                            {
                                value = prop.GetValue(item);

                                // if null, set empty string
                                if (value == null)
                                    value = "";
                            }
                        }
                    }
                    else
                    {
                        // fallback: get ToString of item if no binding
                        value = item.ToString() ?? "";
                    }

                    row[header] = value;
                }

                rows.Add(row);
            }

            return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
        }

        public static string ExportToJson(DataGrid listView)
        {
            if (listView.Items.Count == 0) return "[]";

            var rows = new List<Dictionary<string, object>>();

            int rowIndex = 0;

            foreach (var item in listView.Items)
            {
                var rowItem = listView.Items[rowIndex];

                var row = new Dictionary<string, object>();

                foreach (var column in listView.Columns)
                {
                    if (column.Header is ContentControl && ((ContentControl)column.Header).Content is CheckBox)
                        continue;

                    if (column.Header is CheckBox)
                        continue;

                    string header = column.Header is ContentControl ? ((ContentControl)column.Header).Content.ToString() : "";
                    //string header = column.Header?.ToString() ?? "";
                    if (header == "" || header == "#")
                        continue;

                    object? value = null;

                    if (column is DataGridTextColumn textColumn)
                    {

                        var binding = textColumn.Binding; // <-- same as DisplayMemberBinding

                        if ((item is IDictionary<string, object> dict))
                        {
                            string key = (binding as Binding).Path.Path.Replace("[", "").Replace("]", ""); // key from dictionary
                            if (dict.TryGetValue(key, out var v))
                                value = v ?? "";
                        }

                        else
                        {
                            if (binding != null)
                            {
                                // Example: get the bound property path
                                var path = (binding as Binding).Path.Path;
                                var prop = listView.Items[rowIndex].GetType().GetProperty(path);

                                value = prop?.GetValue(rowItem);
                            }
                        }
                    }

                    row[header] = value;
                }

                rows.Add(row);
                rowIndex++;
            }

            return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
        }
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    yield return typed;

                foreach (var sub in FindVisualChildren<T>(child))
                    yield return sub;
            }
        }

        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        public static DataTable JsonToDataTable(string json)
        {
            var doc = JsonDocument.Parse(json);
            var table = new DataTable();

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("JSON must be an array of objects");

            var array = doc.RootElement.EnumerateArray();

            // build columns from first object
            if (array.Any())
            {
                foreach (var prop in array.First().EnumerateObject())
                {
                    table.Columns.Add(prop.Name, typeof(string)); // store as string (safe)
                }
            }

            // fill rows
            foreach (var element in array)
            {
                var row = table.NewRow();
                foreach (var prop in element.EnumerateObject())
                {
                    row[prop.Name] = prop.Value.ToString();
                }
                table.Rows.Add(row);
            }

            return table;
        }

        public static void WriteFile(DataSet ds, string tableName, int fileCounter, string filePath)
        {
            string TABLE_FILE_EXTENSION = ".tab";
            string numberPrefix = fileCounter.ToString("0000000000");
            string fileName = tableName + "." + numberPrefix + TABLE_FILE_EXTENSION;
            if (!Directory.Exists($"{filePath}/DbTables"))
            {
                Directory.CreateDirectory($"{filePath}/DbTables");
            }


            ds.WriteXml($"{filePath}/DbTables/{fileName}", XmlWriteMode.WriteSchema);
        }

        public static void CompressFile(string sourcePath, string filename, string extenstionFile)
        {
            string zipFilePath = $"{filename}.{extenstionFile}";

            if (System.IO.File.Exists(zipFilePath))
            {
                System.IO.File.Delete(zipFilePath);
            }

            ZipFile.CreateFromDirectory(sourcePath, zipFilePath, CompressionLevel.Optimal, includeBaseDirectory: true);


        }

        public static void ReProcessTranslation(CustObj custObj)
        {
            if (custObj != null)
            {
                bool isRemoveMnemonic = false;
                foreach (CustomObject customObject in custObj.Objects)
                {

                    for (int i = 0; i < customObject.Object.Count; i++)
                    {
                        var item = customObject.Object.ElementAt(i);

                        if (item.Key.ToLower().Contains("key"))
                            continue;

                        if (item.Key.ToLower().Contains("len"))
                        {
                            var currentKey = customObject.Object.ElementAt(i).Key;
                            var prevItem = customObject.Object.ElementAt(i - 1);
                            customObject.Object[currentKey] = prevItem.Value == null ? 0 : prevItem.Value.ToString().Length;
                            // continue;
                        }

                        else if (item.Key.ToLower().Contains("_nomne"))
                        {
                            isRemoveMnemonic = true;
                            var currentKey = customObject.Object.ElementAt(i).Key;
                            var prevItem = customObject.Object.ElementAt(i - 2);
                            customObject.Object[currentKey] = prevItem.Value == null ? "" : prevItem.Value.ToString().Split("(Alt")[0].ToString();
                            // continue;
                        }
                        else
                        {
                            if (i != 1 && isRemoveMnemonic)
                            {
                                isRemoveMnemonic = true;
                                var currentKey = customObject.Object.ElementAt(i);
                                customObject.Object[currentKey.Key] = currentKey.Value == null ? "" : currentKey.Value.ToString().Split("(Alt")[0].ToString();
                            }
                        }
                    }
                }
            }
        }

        public static void CopySelectedRow(List< IDictionary<string, object>> customObject)
        {
            try
            {
                if (customObject !=null && customObject.Count > 0)
                {

                    string jsonString = JsonConvert.SerializeObject(customObject, Newtonsoft.Json.Formatting.Indented);

                    jsonString = Utilities.BeautifyJson(jsonString);

                    Clipboard.SetText(jsonString);
                }
            }
            catch (Exception ex) { DBTool.Controls.ThemedDialog.Show(ex.Message); }
        }

        public static CustObj ProcessSCAHLocale(List<CustomObject> localeObj, List<CustomObject> ExcelObj, List<Language> selectedLanguages)
        {

            List<string> excelKeys = ExcelObj.Select(x => x.Object["KEY"].ToString()).ToList();

            List<CustomObject> localeData = localeObj.Where(x => excelKeys.Contains(x.Object["KEY"].ToString())).ToList();
            List<CustomObject> excelData = ExcelObj.Where(x => excelKeys.Contains(x.Object["KEY"].ToString())).ToList();

            CustObj entityObj = new CustObj();
            entityObj.Objects = new List<CustomObject>();
            entityObj.Fields = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;


            foreach (string key in excelKeys)
            {
              

                CustomObject localeCustomObj = localeData.FirstOrDefault(x => x.Object["KEY"].ToString() == key);
                CustomObject excelCustomObj = excelData.FirstOrDefault(x => x.Object["KEY"].ToString() == key);

                /// fields
                if (!entityObj.Fields.ContainsKey("KEY"))
                {
                    entityObj.Fields.Add("KEY", key.GetType());
                }

                CustomObject customObject = new CustomObject();

                customObject.Object = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                customObject.Object.Add("KEY", key);
                bool hasInconsistency = false;

                if (localeCustomObj != null && excelCustomObj != null)
                {
                    foreach (Language culture in selectedLanguages)
                    {
                        if (!entityObj.Fields.ContainsKey($"{culture.Culture}_DATA") && !entityObj.Fields.ContainsKey($"{culture.Culture}_FILE"))
                        {
                            entityObj.Fields.Add($"{culture.Culture}_DATA", culture.Culture.GetType());
                            entityObj.Fields.Add($"{culture.Culture}_FILE", culture.Culture.GetType());
                        }

                        var dataValue = localeCustomObj.Object[culture.Culture] != null ? localeCustomObj.Object[culture.Culture].ToString().Replace(Convert.ToChar(160).ToString(), " ") : "";
                        var fileValue = excelCustomObj.Object[culture.Culture] != null ? excelCustomObj.Object[culture.Culture].ToString().Replace(Convert.ToChar(160).ToString(), " ") : "";

                        var splittedDataValue = dataValue.Split("(Alt");
                        var splittedfileValue = fileValue.Split("(Alt");

                        if (splittedDataValue.Length > 1 && splittedfileValue.Length == 1)
                        {
                            fileValue = $"{fileValue.TrimStart().TrimEnd()} (Alt{splittedDataValue[1]}";
                        }

                        if (dataValue.ToLower().TrimStart().TrimEnd() != fileValue.ToLower().TrimStart().TrimEnd())
                        {
                            hasInconsistency = true;
                            if (!customObject.Object.ContainsKey("KEY"))
                            {
                                customObject.Object.Add("KEY", key);
                            }

                            if (!customObject.Object.ContainsKey($"{culture.Culture}_DATA"))
                            {
                                customObject.Object.Add($"{culture.Culture}_DATA", dataValue);
                            }

                            if (!customObject.Object.ContainsKey($"{culture.Culture}_FILE"))
                            {
                                customObject.Object.Add($"{culture.Culture}_FILE", fileValue);
                            }
                        }
                        else
                        {
                            if (!customObject.Object.ContainsKey($"{culture.Culture}_DATA"))
                            {
                                customObject.Object.Add($"{culture.Culture}_DATA", "");
                            }

                            if (!customObject.Object.ContainsKey($"{culture.Culture}_FILE"))
                            {
                                customObject.Object.Add($"{culture.Culture}_FILE", "");
                            }
                        }

                    }

                    if (hasInconsistency)
                    {
                        entityObj.Objects.Add(customObject);
                    }
                }

            }

            return entityObj;
        }

        public static CustObj ProcessFSHCultureInfo(List<CustomObject> localeObj, List<CustomObject> ExcelObj, List<Language> selectedLanguages)
        {

            List<string> excelKeys = ExcelObj.Select(x => $"{x.Object["TABREF"].ToString()}_{x.Object["REFID"]}").ToList();


            List<CustomObject> localeData = localeObj.Where(x => excelKeys.Contains($"{x.Object["TABREF"].ToString()}_{x.Object["REFID"]}")).ToList();
            List<CustomObject> excelData = ExcelObj.Where(x => excelKeys.Contains($"{x.Object["TABREF"].ToString()}_{x.Object["REFID"]}")).ToList();

            CustObj entityObj = new CustObj();
            entityObj.Objects = new List<CustomObject>();
            entityObj.Fields = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;


            foreach (string key in excelKeys)
            {
                CustomObject localeCustomObj = localeData.FirstOrDefault(x => $"{x.Object["TABREF"].ToString()}_{x.Object["REFID"]}" == key);
                CustomObject excelCustomObj = excelData.FirstOrDefault(x => $"{x.Object["TABREF"].ToString()}_{x.Object["REFID"]}" == key);

                /// fields
                if (!entityObj.Fields.ContainsKey("KEY"))
                {
                    entityObj.Fields.Add("KEY", key.GetType());
                }

                CustomObject customObject = new CustomObject();

                customObject.Object = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                customObject.Object.Add("KEY", key);
                bool hasInconsistency = false;

                if (localeCustomObj != null && excelCustomObj != null)
                {
                    foreach (Language culture in selectedLanguages)
                    {
                        if (!entityObj.Fields.ContainsKey($"{culture.Culture}_DATA") && !entityObj.Fields.ContainsKey($"{culture.Culture}_FILE"))
                        {
                            entityObj.Fields.Add($"{culture.Culture}_DATA", culture.Culture.GetType());
                            entityObj.Fields.Add($"{culture.Culture}_FILE", culture.Culture.GetType());
                        }

                        var dataValue = localeCustomObj.Object[culture.Culture] != null ? localeCustomObj.Object[culture.Culture].ToString().Replace(Convert.ToChar(160).ToString(), " ") : "";
                        var fileValue = excelCustomObj.Object[culture.Culture] != null ? excelCustomObj.Object[culture.Culture].ToString().Replace(Convert.ToChar(160).ToString(), " ") : "";

                        var splittedDataValue = dataValue.Split("(Alt");
                        var splittedfileValue = fileValue.Split("(Alt");

                        if (splittedDataValue.Length > 1 && splittedfileValue.Length == 1)
                        {
                            fileValue = $"{fileValue} (Alt{splittedDataValue[1]}";
                        }

                        if (dataValue.ToLower().TrimStart().TrimEnd() != fileValue.ToLower().TrimStart().TrimEnd())
                        {
                            hasInconsistency = true;
                            if (!customObject.Object.ContainsKey("KEY"))
                            {
                                customObject.Object.Add("KEY", key);
                            }

                            if (!customObject.Object.ContainsKey($"{culture.Culture}_DATA"))
                            {
                                customObject.Object.Add($"{culture.Culture}_DATA", dataValue);
                            }

                            if (!customObject.Object.ContainsKey($"{culture.Culture}_FILE"))
                            {
                                customObject.Object.Add($"{culture.Culture}_FILE", fileValue);
                            }
                        }
                        else
                        {
                            if (!customObject.Object.ContainsKey($"{culture.Culture}_DATA"))
                            {
                                customObject.Object.Add($"{culture.Culture}_DATA", "");
                            }

                            if (!customObject.Object.ContainsKey($"{culture.Culture}_FILE"))
                            {
                                customObject.Object.Add($"{culture.Culture}_FILE", "");
                            }
                        }

                    }

                    if (hasInconsistency)
                    {
                        entityObj.Objects.Add(customObject);
                    }
                }

            }

            return entityObj;
        }

    }

}
