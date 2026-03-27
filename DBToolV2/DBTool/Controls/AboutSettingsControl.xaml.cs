using DBTool.Connect;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DBTool.Controls
{
    public partial class AboutSettingsControl : UserControl
    {
        private VersionInfo _latestVersion;

        public AboutSettingsControl()
        {
            InitializeComponent();
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            lblVersion.Content = $"Version {version}";
        }

        private async void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            btnCheckUpdate.IsEnabled = false;
            lblUpdateStatus.Content = "Checking...";
            pnlUpdateAvailable.Visibility = Visibility.Collapsed;

            _latestVersion = await AppUpdater.CheckForUpdate();

            if (_latestVersion == null)
            {
                lblUpdateStatus.Content = "Could not check for updates.";
                btnCheckUpdate.IsEnabled = true;
                return;
            }

            string current = AppUpdater.GetCurrentVersion();

            if (AppUpdater.IsNewerVersion(_latestVersion.version, current))
            {
                txtNewVersion.Text = $"New version available: {_latestVersion.version}";
                pnlUpdateAvailable.Visibility = Visibility.Visible;
                lblUpdateStatus.Content = "";
            }
            else
            {
                lblUpdateStatus.Content = "You're up to date.";
            }

            btnCheckUpdate.IsEnabled = true;
        }

        private async void btnDownloadUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_latestVersion == null) return;

            btnDownloadUpdate.IsEnabled = false;
            btnDownloadUpdate.Content = "Downloading...";
            progressUpdate.Visibility = Visibility.Visible;

            string extractedPath = await AppUpdater.DownloadUpdate(
                _latestVersion.downloadUrl,
                progress => Dispatcher.Invoke(() => progressUpdate.Value = progress));

            if (extractedPath != null)
            {
                if (ThemedDialog.Confirm("Update downloaded. Restart now to apply?",
                    "Update Ready"))
                {
                    AppUpdater.ApplyUpdate(extractedPath);
                }
            }
            else
            {
                ThemedDialog.Show("Download failed.", "Error");
            }

            btnDownloadUpdate.IsEnabled = true;
            btnDownloadUpdate.Content = "Download & Install";
            progressUpdate.Visibility = Visibility.Collapsed;
        }

        private async void btnViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DBTool-Updater");

                string markdown = null;

                // Try changelogdDir from version.json first
                if (!string.IsNullOrEmpty(_latestVersion?.changelogdDir))
                {
                    try { markdown = await client.GetStringAsync(_latestVersion.changelogdDir); }
                    catch { }
                }

                // Fallback to raw repo CHANGELOG.md
                if (string.IsNullOrEmpty(markdown))
                {
                    try { markdown = await client.GetStringAsync("https://raw.githubusercontent.com/cmartin2-dev/DBTool/main/CHANGELOG.md"); }
                    catch { }
                }

                if (string.IsNullOrEmpty(markdown))
                {
                    ThemedDialog.Show("Could not load changelog.", "Error");
                    return;
                }

                var window = new Window
                {
                    Title = "Release Notes",
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = System.Windows.Application.Current.MainWindow,
                    Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush")
                };

                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = markdown,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    Margin = new Thickness(16),
                    BorderThickness = new Thickness(0),
                    Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush"),
                    Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush")
                };

                window.Content = textBox;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                ThemedDialog.Show($"Could not load changelog: {ex.Message}", "Error");
            }
        }
    }
}
