using Entities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DBTool.Connect;

namespace DBTool.Controls
{
    public class VersionItem
    {
        public string Version { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ChangesetDisplayItem
    {
        public string Version { get; set; }
        public string Schema { get; set; }
        public string FileName { get; set; }
        public string Id { get; set; }
        public string Author { get; set; }
        public string Comment { get; set; }
        public string Script { get; set; }
        public bool IsSelected { get; set; } = true;
        public string Status { get; set; } = "Pending";
    }

    public class SchemaItem
    {
        public string SchemaName { get; set; }
        public string Name { get; set; }
        public string ModuleCode { get; set; }
        public string DbVersion { get; set; }
        public bool IsSelected { get; set; }
    }

    public class ChangelogPreviewItem
    {
        public string Id { get; set; }
        public string Author { get; set; }
        public string FileName { get; set; }
        public string ExecType { get; set; }
        public string Description { get; set; }
        public string Comments { get; set; }
    }

    public partial class UpgradeControl : UserControl
    {
        private ObservableCollection<VersionItem> _versions = new ObservableCollection<VersionItem>();
        private List<VersionChangeset> _loadedChangesets = new List<VersionChangeset>();
        private string _upgradeFolderPath;
        private bool _isRunning = false;
        private CancellationTokenSource _sourceToken;

        public UpgradeControl()
        {
            InitializeComponent();
            lstVersions.ItemsSource = _versions;
            this.DataContextChanged += UpgradeControl_DataContextChanged;
        }

        private async void UpgradeControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var regionTenant = e.NewValue as RegionTenant;
            if (regionTenant != null && regionTenant.Region != null)
            {
                await FetchSchemas(regionTenant);
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if (openFolderDialog.ShowDialog() == true)
            {
                _upgradeFolderPath = openFolderDialog.FolderName;
                txtUpgradePath.Text = _upgradeFolderPath;
                LoadVersions();
            }
        }

        private void LoadVersions()
        {
            _versions.Clear();
            var versions = StaticFunctions.GetVersionFolders(_upgradeFolderPath);
            foreach (var v in versions)
            {
                _versions.Add(new VersionItem { Version = v, IsSelected = false });
            }
            AppendLog($"Found {versions.Count} version(s).");
        }

        private async void btnFetchSchemas_Click(object sender, RoutedEventArgs e)
        {
            var regionTenant = this.DataContext as RegionTenant;
            if (regionTenant == null || regionTenant.Region == null)
            {
                ThemedDialog.Show("Please select an environment first.", "No Environment");
                return;
            }

            await FetchSchemas(regionTenant);
        }

        private async Task FetchSchemas(RegionTenant regionTenant)
        {
            AppendLog("Fetching schemas...");

            try
            {
                string query = @"SELECT 0 AS 'DATASCHEMASID','SCAH' as 'MODULECODE','SCAH' AS 'NAME', 'SCAH' AS 'SCHEMANAME', '' AS 'DBVERSION' UNION SELECT DATASCHEMASID,MODULECODE,NAME,SCHEMANAME,DBVERSION FROM SCAH.DATASCHEMAS WHERE ISREADY = 1";

                RequestQuery requestQuery = new RequestQuery();
                requestQuery.Query = query;
                requestQuery.SetDetails(regionTenant);
                requestQuery.sourceToken = new CancellationTokenSource();
                requestQuery.token = requestQuery.sourceToken.Token;

                var task = requestQuery.GetRequestQuery();
                await task;

                if (task.Result != null && task.Result.isSuccess)
                {
                    var schemas = new List<SchemaItem>();
                    foreach (var obj in task.Result.CustObj.Objects)
                    {
                        var dict = obj.Object as IDictionary<string, object>;
                        if (dict != null && dict.ContainsKey("SCHEMANAME"))
                        {
                            schemas.Add(new SchemaItem
                            {
                                SchemaName = dict.ContainsKey("SCHEMANAME") ? dict["SCHEMANAME"]?.ToString() : "",
                                Name = dict.ContainsKey("NAME") ? dict["NAME"]?.ToString() : "",
                                ModuleCode = dict.ContainsKey("MODULECODE") ? dict["MODULECODE"]?.ToString() : "",
                                DbVersion = dict.ContainsKey("DBVERSION") ? dict["DBVERSION"]?.ToString() : ""
                            });
                        }
                    }

                    dgSchemas.ItemsSource = schemas;
                    if (schemas.Count > 0)
                        dgSchemas.SelectedIndex = 0;

                    AppendLog($"Loaded {schemas.Count} schema(s).");
                }
                else
                {
                    AppendLog($"Failed to fetch schemas: {task.Result?.ErrorMessage ?? "No response"}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error fetching schemas: {ex.Message}");
            }
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var v in _versions) v.IsSelected = true;
            lstVersions.Items.Refresh();
        }

        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var v in _versions) v.IsSelected = false;
            lstVersions.Items.Refresh();
        }

        private void VersionCheckBox_Checked(object sender, RoutedEventArgs e) { }
        private void VersionCheckBox_Unchecked(object sender, RoutedEventArgs e) { }

        private void dgChangesets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = dgChangesets.SelectedItem as ChangesetDisplayItem;
            if (selected != null)
            {
                // Replace parameters in the displayed script
                string script = selected.Script ?? "(no script content)";
                var parameters = BuildParameters();
                foreach (var param in parameters)
                {
                    script = script.Replace($"${{{param.Key}}}", param.Value);
                }
                txtScriptContent.Text = script;
                tabDetail.SelectedIndex = 0;
            }
        }

        private void txtChangesetSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(dgChangesets.ItemsSource);
            if (view == null) return;

            string searchText = txtChangesetSearch.Text?.Trim() ?? "";

            view.Filter = item =>
            {
                if (string.IsNullOrEmpty(searchText)) return true;

                if (item is ChangesetDisplayItem cs)
                {
                    return (cs.Version?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (cs.Schema?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (cs.FileName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (cs.Id?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (cs.Author?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (cs.Comment?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (cs.Status?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true);
                }

                return false;
            };

            view.Refresh();
        }

        private void chkSelectAllSchemas_Checked(object sender, RoutedEventArgs e)
        {
            var items = dgSchemas.ItemsSource as List<SchemaItem>;
            if (items != null)
            {
                foreach (var item in items) item.IsSelected = true;
                dgSchemas.Items.Refresh();
            }
        }

        private void chkSelectAllSchemas_Unchecked(object sender, RoutedEventArgs e)
        {
            var items = dgSchemas.ItemsSource as List<SchemaItem>;
            if (items != null)
            {
                foreach (var item in items) item.IsSelected = false;
                dgSchemas.Items.Refresh();
            }
        }

        private bool _suppressHeaderCheckbox = false;

        private void chkSelectAllChangesets_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressHeaderCheckbox) return;
            var items = dgChangesets.ItemsSource as List<ChangesetDisplayItem>;
            if (items != null)
            {
                foreach (var item in items) item.IsSelected = true;
                dgChangesets.Items.Refresh();
            }
        }

        private void chkSelectAllChangesets_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressHeaderCheckbox) return;
            var items = dgChangesets.ItemsSource as List<ChangesetDisplayItem>;
            if (items != null)
            {
                foreach (var item in items) item.IsSelected = false;
                dgChangesets.Items.Refresh();
            }
        }

        private async void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            var items = dgChangesets.ItemsSource as List<ChangesetDisplayItem>;
            if (items == null || !items.Any())
            {
                ThemedDialog.Show("Please load changesets first.", "No Changesets");
                return;
            }

            var dataContext = this.DataContext as RegionTenant;
            if (dataContext == null || dataContext.Region == null)
            {
                ThemedDialog.Show("No environment selected.", "Error");
                return;
            }

            btnVerify.IsEnabled = false;
            btnVerify.Content = "Verifying...";
            tabDetail.SelectedIndex = 2; // switch to Execution Log tab
            AppendLog("Verifying changesets against DATABASECHANGELOG...");

            try
            {
                // Build a query to get all existing changeset IDs
                string query = "SELECT ID, AUTHOR FROM DATABASECHANGELOG";

                RequestQuery requestQuery = new RequestQuery();
                requestQuery.Query = query;
                requestQuery.SetDetails(dataContext);
                requestQuery.sourceToken = new CancellationTokenSource();
                requestQuery.token = requestQuery.sourceToken.Token;

                var task = requestQuery.GetRequestQuery();
                await task;

                var existingIds = new HashSet<string>();

                if (task.Result != null && task.Result.isSuccess)
                {
                    foreach (var obj in task.Result.CustObj.Objects)
                    {
                        var dict = obj.Object as IDictionary<string, object>;
                        if (dict != null && dict.ContainsKey("ID") && dict.ContainsKey("AUTHOR"))
                        {
                            string key = $"{dict["ID"]}|{dict["AUTHOR"]}";
                            existingIds.Add(key);
                        }
                    }

                    int alreadyExists = 0;
                    int pending = 0;

                    foreach (var item in items)
                    {
                        string key = $"{item.Id}|{item.Author}";
                        if (existingIds.Contains(key))
                        {
                            item.Status = "Exists";
                            item.IsSelected = false;
                            alreadyExists++;
                        }
                        else
                        {
                            item.Status = "New";
                            item.IsSelected = true;
                            pending++;
                        }
                    }

                    dgChangesets.Items.Refresh();
                    AppendLog($"Verification complete. {alreadyExists} already exist, {pending} new changeset(s) to run.");

                    // Update header checkbox without triggering events
                    _suppressHeaderCheckbox = true;
                    var headerCheckbox = FindName("chkSelectAllChangesets") as System.Windows.Controls.CheckBox;
                    if (headerCheckbox != null)
                        headerCheckbox.IsChecked = pending > 0 && alreadyExists == 0;
                    _suppressHeaderCheckbox = false;
                }
                else
                {
                    AppendLog($"Failed to verify: {task.Result?.ErrorMessage ?? "No response"}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error verifying: {ex.Message}");
            }
            finally
            {
                btnVerify.IsEnabled = true;
                btnVerify.Content = "Verify";
            }
        }

        private List<string> GetSelectedModules()
        {
            var modules = new List<string>();
            if (chkSCAH.IsChecked == true) modules.Add("SCAH");
            if (chkFSH.IsChecked == true) modules.Add("FSH");
            return modules;
        }

        private void btnLoadChangesets_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedVersions();
            if (!selected.Any())
            {
                ThemedDialog.Show("Please select at least one version.", "No Version Selected");
                return;
            }

            if (string.IsNullOrEmpty(_upgradeFolderPath))
            {
                ThemedDialog.Show("Please select an upgrade folder.", "No Folder Selected");
                return;
            }

            try
            {
                var parameters = BuildParameters();
                var modules = GetSelectedModules();
                if (!modules.Any())
                {
                    ThemedDialog.Show("Please check at least one module (SCAH or FSH).", "No Module");
                    return;
                }
                _loadedChangesets = StaticFunctions.ReadUpgradeFolder(_upgradeFolderPath, selected, parameters, modules);

                var displayItems = new List<ChangesetDisplayItem>();
                foreach (var vc in _loadedChangesets)
                {
                    foreach (var item in vc.ChangeSetItem)
                    {
                        displayItems.Add(new ChangesetDisplayItem
                        {
                            Version = vc.Version,
                            Schema = item.Schema,
                            FileName = item.FileName,
                            Id = item.Id,
                            Author = item.Author,
                            Comment = item.Comment,
                            Script = item.Script
                        });
                    }
                }

                dgChangesets.ItemsSource = displayItems;
                AppendLog($"Loaded {displayItems.Count} changeset(s) from {selected.Count} version(s).");

                // Populate DATABASECHANGELOG preview
                var previewItems = displayItems.Select(d => new ChangelogPreviewItem
                {
                    Id = d.Id,
                    Author = d.Author,
                    FileName = d.FileName,
                    ExecType = "EXECUTED",
                    Description = "sql",
                    Comments = d.Comment
                }).ToList();
                dgChangelogPreview.ItemsSource = previewItems;
            }
            catch (Exception ex)
            {
                AppendLog($"Error loading changesets: {ex.Message}");
            }
        }

        private List<string> GetSelectedVersions()
        {
            return _versions.Where(v => v.IsSelected).Select(v => v.Version).OrderBy(v => v).ToList();
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedVersions();
            if (!selected.Any())
            {
                ThemedDialog.Show("Please select at least one version.", "No Version Selected");
                return;
            }

            if (string.IsNullOrEmpty(_upgradeFolderPath))
            {
                ThemedDialog.Show("Please select an upgrade folder.", "No Folder Selected");
                return;
            }

            txtLog.Clear();
            AppendLog("Generating preview...");

            try
            {
                var parameters = BuildParameters();
                var modules = GetSelectedModules();
                var versionChangesets = StaticFunctions.ReadUpgradeFolder(_upgradeFolderPath, selected, parameters, modules);
                var script = StaticFunctions.BuildUpgradeScript(versionChangesets);

                int totalChangesets = versionChangesets.Sum(vc => vc.ChangeSetItem.Count);
                AppendLog($"Preview complete. {selected.Count} version(s), {totalChangesets} changeset(s).");
                AppendLog("--- Script Preview ---");
                AppendLog(script);
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
            }
        }

        private async void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedVersions();
            if (!selected.Any())
            {
                ThemedDialog.Show("Please select at least one version.", "No Version Selected");
                return;
            }

            if (string.IsNullOrEmpty(_upgradeFolderPath))
            {
                ThemedDialog.Show("Please select an upgrade folder.", "No Folder Selected");
                return;
            }

            var dataContext = this.DataContext as RegionTenant;
            if (dataContext == null || dataContext.Region == null)
            {
                ThemedDialog.Show("No environment selected.", "Error");
                return;
            }

            if (_isRunning)
            {
                _sourceToken?.Cancel();
                _isRunning = false;
                btnExecute.Content = "Execute Upgrade";
                lblStatus.Content = "Cancelled";
                AppendLog("Execution cancelled by user.");
                return;
            }

            if (!ThemedDialog.Confirm($"Execute upgrade for {selected.Count} version(s)?", "Confirm Execution"))
                return;

            _isRunning = true;
            _sourceToken = new CancellationTokenSource();
            btnExecute.Content = "Cancel";
            txtLog.Clear();

            var executionLog = new ExecutionLog();

            try
            {
                var parameters = BuildParameters();
                var versionChangesets = _loadedChangesets.Any() ? _loadedChangesets 
                    : StaticFunctions.ReadUpgradeFolder(_upgradeFolderPath, selected, parameters);

                // Get only checked changesets from the grid
                var allDisplayItems = dgChangesets.ItemsSource as List<ChangesetDisplayItem>;
                var selectedItems = allDisplayItems?.Where(x => x.IsSelected).ToList() ?? new List<ChangesetDisplayItem>();

                if (!selectedItems.Any())
                {
                    ThemedDialog.Show("No changesets selected to run.", "No Selection");
                    _isRunning = false;
                    btnExecute.Content = "Execute Upgrade";
                    return;
                }

                var checkedSchemas = GetCheckedSchemas();
                if (!checkedSchemas.Any())
                {
                    ThemedDialog.Show("No schemas checked. Please check at least one schema.", "No Schema");
                    _isRunning = false;
                    btnExecute.Content = "Execute Upgrade";
                    return;
                }

                int totalChangesets = selectedItems.Count * checkedSchemas.Count;
                int current = 0;

                foreach (var schemaName in checkedSchemas)
                {
                    if (_sourceToken.IsCancellationRequested) break;

                    AppendLog($"--- Schema: {schemaName} ---");

                    foreach (var item in selectedItems)
                    {
                        if (_sourceToken.IsCancellationRequested) break;

                        current++;
                        lblStatus.Content = $"Executing {current}/{totalChangesets}: [{schemaName}] {item.FileName} ({item.Id})";

                        // Replace ${schemaName} in the script
                        string script = item.Script?.Replace("${schemaName}", schemaName) ?? "";

                        // Remove GO batch separators
                        script = System.Text.RegularExpressions.Regex.Replace(script, @"^\s*GO\s*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline).Trim();

                        // Skip blank scripts
                        if (string.IsNullOrWhiteSpace(script))
                        {
                            AppendLog($"  SKIP: [{schemaName}] {item.Id} ({item.FileName}) - empty script");
                            continue;
                        }

                        var changeset = new Changeset
                        {
                            Id = item.Id,
                            Author = item.Author,
                            Comment = item.Comment,
                            Script = script
                        };

                        string executionScript = StaticFunctions.BuildChangesetExecutionScript(changeset, item.FileName);

                        var response = await ExecuteScript(executionScript, dataContext);

                        string status;
                        if (response.isSuccess)
                        {
                            status = "Success";
                            AppendLog($"  OK: [{schemaName}] {item.Id} ({item.FileName})");
                        }
                        else
                        {
                            status = "Failed";
                            AppendLog($"  FAIL: [{schemaName}] {item.Id} ({item.FileName}) - {response.ErrorMessage}");
                        }

                        executionLog.Add(new ExecutionLogEntry
                        {
                            TenantId = dataContext.tenantId,
                            SchemaName = schemaName,
                            ChangesetId = item.Id,
                            FileName = item.FileName,
                            Comment = item.Comment,
                            Version = item.Version,
                            Status = status,
                            DateExecuted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ExecutedBy = StaticFunctions.CurrentUser
                        });
                    }
                }

                lblStatus.Content = _sourceToken.IsCancellationRequested ? "Cancelled" : $"Done. {current}/{totalChangesets} executed.";
                AppendLog(_sourceToken.IsCancellationRequested ? "Execution cancelled." : "Upgrade execution complete.");
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
                lblStatus.Content = "Error";
            }
            finally
            {
                AppendLog($"Saving execution log ({executionLog.GetAll().Count} entries)...");
                executionLog.Save();
                AppendLog("Execution log saved.");
                _isRunning = false;
                btnExecute.Content = "Execute Upgrade";
            }
        }

        private async Task<RequestResponse> ExecuteScript(string script, RegionTenant regionTenant)
        {
            RequestQuery requestQuery = new RequestQuery();
            requestQuery.SetDetails(regionTenant);
            requestQuery.sourceToken = _sourceToken;
            requestQuery.token = _sourceToken.Token;
            requestQuery.Query = script;

            var task = requestQuery.GetRequestQuery();
            await task;

            return task.Result ?? new RequestResponse { isSuccess = false, ErrorMessage = "No response" };
        }

        private Dictionary<string, string> BuildParameters()
        {
            var parameters = new Dictionary<string, string>();

            // For preview/display, use the highlighted row
            var selectedSchema = dgSchemas.SelectedItem as SchemaItem;
            if (selectedSchema != null && !string.IsNullOrEmpty(selectedSchema.SchemaName))
            {
                parameters["schemaName"] = selectedSchema.SchemaName;
            }

            return parameters;
        }

        private List<string> GetCheckedSchemas()
        {
            var items = dgSchemas.ItemsSource as List<SchemaItem>;
            if (items == null) return new List<string>();
            return items.Where(x => x.IsSelected && !string.IsNullOrEmpty(x.SchemaName))
                        .Select(x => x.SchemaName)
                        .ToList();
        }

        private void AppendLog(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToEnd();
        }
    }
}
