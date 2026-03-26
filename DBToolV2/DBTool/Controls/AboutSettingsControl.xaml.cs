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
                if (MessageBox.Show("Update downloaded. Restart now to apply?",
                    "Update Ready", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    AppUpdater.ApplyUpdate(extractedPath);
                }
            }
            else
            {
                MessageBox.Show("Download failed.", "Error");
            }

            btnDownloadUpdate.IsEnabled = true;
            btnDownloadUpdate.Content = "Download & Install";
            progressUpdate.Visibility = Visibility.Collapsed;
        }

        private void btnViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            if (_latestVersion?.changelog != null)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _latestVersion.changelog,
                    UseShellExecute = true
                });
            }
        }
    }
}
