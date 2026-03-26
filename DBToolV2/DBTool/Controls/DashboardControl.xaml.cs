using DBTool.Connect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DBTool.Controls
{
    public class TopTenantItem
    {
        public int Rank { get; set; }
        public string TenantId { get; set; }
        public int Count { get; set; }
        public double BarWidth { get; set; }
    }

    public partial class DashboardControl : UserControl
    {
        public DashboardControl()
        {
            InitializeComponent();
            this.IsVisibleChanged += DashboardControl_IsVisibleChanged;
        }

        private void DashboardControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                RefreshDashboard();
            }
        }

        public void RefreshDashboard()
        {
            var settings = StaticFunctions.AppConnection?.settingsObject;
            if (settings == null) return;

            // User info
            txtCurrentUser.Text = StaticFunctions.CurrentUser;
            if (settings.CheckAccess)
            {
                txtAccessLevel.Text = "FULL ACCESS";
                badgeAccess.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                txtAccessLevel.Foreground = Brushes.White;
            }
            else
            {
                txtAccessLevel.Text = "READ ONLY";
                badgeAccess.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17"));
                txtAccessLevel.Foreground = Brushes.White;
            }

            // Stats
            txtRegionCount.Text = (settings.Regions?.Count ?? 0).ToString();
            txtEnvironmentCount.Text = (settings.Headers?.Count ?? 0).ToString();
            txtQueryCount.Text = (settings.Queries?.Count ?? 0).ToString();
            txtLanguageCount.Text = (settings.Languages?.Count ?? 0).ToString();
            txtServerCount.Text = (settings.EnvironmentServers?.Count ?? 0).ToString();

            // Recent script executions
            LoadRecentScripts();

            // Top tenants by executions
            LoadTopTenants();
        }

        private void LoadRecentScripts()
        {
            try
            {
                var log = new ExecutionLog();
                var recent = log.GetRecent(5);
                lstRecentScripts.ItemsSource = recent;
                txtQueryPreview.Text = "";
            }
            catch
            {
                lstRecentScripts.ItemsSource = null;
            }
        }

        private void lstRecentScripts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = lstRecentScripts.SelectedItem as ExecutionLogEntry;
            if (selected != null)
            {
                txtQueryPreview.Text = selected.Query ?? selected.Comment ?? "(no query)";
            }
        }

        private void lstRecentScripts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenSelectedInQueryTab();
        }

        private void OpenSelectedInQueryTab()
        {
            var selected = lstRecentScripts.SelectedItem as ExecutionLogEntry;
            if (selected == null)
                return;

            string query = selected.Query ?? selected.Comment ?? "";
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("No query content in this entry.");
                return;
            }

            try
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                if (mainWindow == null) return;

                mainWindow.SwitchToEnvironment();
                mainWindow.environmentControl.CreateTabWithQuery(query);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void LoadTopTenants()
        {
            try
            {
                var log = new ExecutionLog();
                var topTenants = log.GetTopTenants(10);

                if (topTenants.Any())
                {
                    int maxCount = topTenants.Max(x => x.Count);
                    double maxBarWidth = 200;

                    var items = topTenants.Select((t, i) => new TopTenantItem
                    {
                        Rank = i + 1,
                        TenantId = t.TenantId,
                        Count = t.Count,
                        BarWidth = maxCount > 0 ? (double)t.Count / maxCount * maxBarWidth : 0
                    }).ToList();

                    lstTopTenants.ItemsSource = items;
                }
                else
                {
                    lstTopTenants.ItemsSource = null;
                }
            }
            catch
            {
                lstTopTenants.ItemsSource = null;
            }
        }
    }
}
