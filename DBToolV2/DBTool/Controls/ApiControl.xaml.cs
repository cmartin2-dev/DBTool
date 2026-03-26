using DBTool.Commons;
using DBTool.Connect;
using Entities;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DBTool.Controls
{
    public class ApiSchemaItem
    {
        public string SchemaName { get; set; }
        public bool IsSelected { get; set; }
        public string Status { get; set; } = "";
    }

    public partial class ApiControl : UserControl
    {
        private bool _isRunning = false;
        private CancellationTokenSource _cts;
        private RegionTenant _selectedRegionTenant;
        private HeaderEnvironment _selectedHeader;

        public ApiControl()
        {
            InitializeComponent();
            LoadRegions();
        }

        private void LoadRegions()
        {
            cmbRegion.DisplayMemberPath = "RegionName";
            cmbRegion.SelectedValuePath = "Id";

            var binding = new Binding
            {
                Source = StaticFunctions.AppConnection.settingsObject.Regions
            };
            cmbRegion.SetBinding(ComboBox.ItemsSourceProperty, binding);
        }

        private async void cmbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var region = cmbRegion.SelectedItem as Region;
            if (region == null)
            {
                lstEnvironments.ClearItems();
                return;
            }

            var header = StaticFunctions.AppConnection.settingsObject.Headers
                .FirstOrDefault(x => x.Id == region.HeaderEnvironmentId);

            if (header == null) return;

            _selectedHeader = header;

            // Show headers immediately when region is selected
            if (header.Headers != null && header.Headers.Count > 0)
            {
                var headerLines = header.Headers.Select(h => $"{h.Key}: {h.Value}");
                txtHeaders.Text = string.Join(Environment.NewLine, headerLines);
            }
            else
            {
                txtHeaders.Text = "(no headers configured)";
            }

            lstEnvironments.ShowLoading();

            try
            {
                RequestQuery requestQuery = new RequestQuery();
                requestQuery.SetDetails(region, header);
                var task = requestQuery.GetRequestEnvironment();
                await task;

                if (task.Result != null && task.Result.isSuccess)
                {
                    var tenantList = task.Result.TenantList as List<RegionTenant>;
                    if (tenantList != null)
                    {
                        tenantList.ForEach(x =>
                        {
                            x.Region = region;
                            var tenant = StaticFunctions.GetTenants()?.FirstOrDefault(y => y.TenantId == x.tenantId)?.TenantName;
                            x.TenantName = tenant ?? "";
                        });

                        lstEnvironments.CustomColumns.Clear();
                        lstEnvironments.CustomColumns.Add("tenantId", "Tenant Id");
                        lstEnvironments.CustomColumns.Add("TenantName", "Tenant Name");
                        lstEnvironments.LoadData(tenantList, null, null);
                        lstEnvironments.loadingControl.lblTotalCount = new Label();
                        lstEnvironments.ShowListView();
                    }
                }
                else
                {
                    lstEnvironments.ShowError();
                }
            }
            catch
            {
                lstEnvironments.ShowError();
            }

            UpdateHeaders();
        }

        private void lstEnvironments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var rt = lstEnvironments.dataGrid1.SelectedItem as RegionTenant;
            if (rt != null)
            {
                rt.Region = cmbRegion.SelectedItem as Region;
                _selectedRegionTenant = rt;
                UpdateHeaders();
                FetchSchemas(rt);
            }
        }

        private async void FetchSchemas(RegionTenant regionTenant)
        {
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
                    var schemas = new List<ApiSchemaItem>();
                    foreach (var obj in task.Result.CustObj.Objects)
                    {
                        var dict = obj.Object as IDictionary<string, object>;
                        if (dict != null && dict.ContainsKey("SCHEMANAME"))
                        {
                            schemas.Add(new ApiSchemaItem
                            {
                                SchemaName = dict["SCHEMANAME"]?.ToString() ?? "",
                                IsSelected = false
                            });
                        }
                    }
                    dgSchemas.ItemsSource = schemas;
                }
            }
            catch { }
        }

        private void chkSelectAllSchemas_Checked(object sender, RoutedEventArgs e)
        {
            var items = dgSchemas.ItemsSource as List<ApiSchemaItem>;
            if (items != null)
            {
                foreach (var item in items) item.IsSelected = true;
                dgSchemas.Items.Refresh();
            }
        }

        private void chkSelectAllSchemas_Unchecked(object sender, RoutedEventArgs e)
        {
            var items = dgSchemas.ItemsSource as List<ApiSchemaItem>;
            if (items != null)
            {
                foreach (var item in items) item.IsSelected = false;
                dgSchemas.Items.Refresh();
            }
        }

        private List<string> GetCheckedSchemas()
        {
            var items = dgSchemas.ItemsSource as List<ApiSchemaItem>;
            if (items == null) return new List<string>();
            return items.Where(x => x.IsSelected).Select(x => x.SchemaName).ToList();
        }

        private void UpdateHeaders()
        {
            if (_selectedHeader?.Headers != null)
            {
                var headerLines = _selectedHeader.Headers.Select(h =>
                {
                    if (h.Key == "X-Infor-TenantId" && _selectedRegionTenant != null)
                        return $"{h.Key}: {_selectedRegionTenant.tenantId}";
                    return $"{h.Key}: {h.Value}";
                });
                txtHeaders.Text = string.Join(Environment.NewLine, headerLines);
            }
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _cts?.Cancel();
                _isRunning = false;
                btnSend.Content = "Send";
                lblStatus.Content = "Cancelled";
                return;
            }

            string url = txtUrl.Text?.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a URL.", "No URL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedHeader == null)
            {
                MessageBox.Show("Please select a region first.", "No Region", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isRunning = true;
            _cts = new CancellationTokenSource();
            btnSend.Content = "Cancel";
            txtResponse.Text = "";
            lblStatusCode.Content = "";
            lblElapsed.Content = "";
            lblStatus.Content = "Sending...";

            var stopwatch = Stopwatch.StartNew();

            try
            {
                string method = (cmbMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "GET";
                var checkedSchemas = GetCheckedSchemas();

                // If no schemas checked, run once without replacement
                if (!checkedSchemas.Any())
                    checkedSchemas = new List<string> { "" };

                var allResults = new System.Text.StringBuilder();
                var schemaItems = dgSchemas.ItemsSource as List<ApiSchemaItem>;

                // Mark all checked as Pending
                if (schemaItems != null)
                {
                    foreach (var s in schemaItems.Where(x => x.IsSelected))
                        s.Status = "Pending";
                    dgSchemas.Items.Refresh();
                }

                foreach (var schemaName in checkedSchemas)
                {
                    string currentUrl = url.Replace("{schemaName}", schemaName);
                    string currentBody = txtRequestBody.Text?.Trim()?.Replace("{schemaName}", schemaName) ?? "";

                    // Update status to Sending
                    var currentItem = schemaItems?.FirstOrDefault(x => x.SchemaName == schemaName);
                    if (currentItem != null)
                    {
                        currentItem.Status = "Sending...";
                        dgSchemas.Items.Refresh();
                    }

                    if (checkedSchemas.Count > 1 && !string.IsNullOrEmpty(schemaName))
                        allResults.AppendLine($"--- {schemaName} ---");

                    var options = new RestClientOptions();
                    if (_selectedHeader.isOAuth1)
                    {
                        options.Authenticator = OAuth1Authenticator.ForRequestToken(
                            _selectedHeader.ClientId, _selectedHeader.ClientSecret);
                    }
                    options.MaxTimeout = 300000;
                    var client = new RestClient(options);

                    var request = new RestRequest(currentUrl);
                    request.Method = method switch
                    {
                        "GET" => Method.Get,
                        "POST" => Method.Post,
                        "PUT" => Method.Put,
                        "PATCH" => Method.Patch,
                        "DELETE" => Method.Delete,
                        _ => Method.Get
                    };

                    // Parse headers from the textbox
                    var headerLines = txtHeaders.Text?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (headerLines != null)
                    {
                        foreach (var line in headerLines)
                        {
                            var colonIndex = line.IndexOf(':');
                            if (colonIndex > 0)
                            {
                                string key = line.Substring(0, colonIndex).Trim();
                                string val = line.Substring(colonIndex + 1).Trim()
                                    .Replace("{schemaName}", schemaName);
                                try { request.AddOrUpdateHeader(key, val); } catch { }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(currentBody) && method != "GET")
                    {
                        request.AddStringBody(currentBody, ContentType.Json);
                    }

                    lblStatus.Content = string.IsNullOrEmpty(schemaName) 
                        ? "Sending..." 
                        : $"Sending [{schemaName}]...";

                    var response = await client.ExecuteAsync(request, _cts.Token);

                    lblStatusCode.Content = $"{(int)response.StatusCode} {response.StatusCode}";
                    lblStatusCode.Foreground = response.IsSuccessful
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));

                    // Update schema status
                    if (currentItem != null)
                    {
                        currentItem.Status = $"{(int)response.StatusCode} {response.StatusCode}";
                        dgSchemas.Items.Refresh();
                    }

                    allResults.AppendLine(response.Content ?? response.ErrorMessage ?? "(no response)");
                    allResults.AppendLine();
                }

                stopwatch.Stop();
                lblElapsed.Content = $"{stopwatch.ElapsedMilliseconds}ms";
                lblStatus.Content = "Done";
                txtResponse.Text = allResults.ToString().Trim();
            }
            catch (OperationCanceledException)
            {
                lblStatus.Content = "Cancelled";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                lblStatus.Content = "Error";
                txtResponse.Text = ex.Message;
            }
            finally
            {
                _isRunning = false;
                btnSend.Content = "Send";
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtResponse.Text))
                Clipboard.SetText(txtResponse.Text);
        }

        private void btnBeautify_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(txtResponse.Text);
                txtResponse.Text = JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch { }
        }
    }
}
