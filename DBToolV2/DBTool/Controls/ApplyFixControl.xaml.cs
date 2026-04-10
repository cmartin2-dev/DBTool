using DBTool.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DBTool.Controls
{
    public partial class ApplyFixControl : UserControl
    {
        private FixService _fixService;
        private List<FixItem> _fixItems = new();

        public ApplyFixControl()
        {
            InitializeComponent();
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the SQLSERVER root folder"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtRootFolder.Text = dialog.SelectedPath;
                _fixService = new FixService(dialog.SelectedPath);
                var versions = _fixService.GetVersions();
                cboVersion.ItemsSource = versions;
                if (versions.Count > 0) cboVersion.SelectedIndex = 0;
                txtSummary.Text = $"Found {versions.Count} versions.";
            }
        }

        private void FixScan_Click(object sender, RoutedEventArgs e)
        {
            if (_fixService == null)
            {
                ThemedDialog.Show("Please select a root folder first.", "No Folder");
                return;
            }
            if (cboVersion.SelectedItem == null)
            {
                ThemedDialog.Show("Please select a version.", "No Version");
                return;
            }

            var version = cboVersion.SelectedItem.ToString();
            var schema = ((ComboBoxItem)cboSchema.SelectedItem).Content.ToString();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var versions = _fixService.GetVersions();
                var prevVersion = _fixService.GetPreviousVersion(version, versions);

                _fixItems = new List<FixItem>();

                if (prevVersion != null)
                {
                    _fixItems.AddRange(_fixService.FindMissingFromPrevious(version, prevVersion, schema));
                }

                _fixItems.AddRange(_fixService.FindMissingFromUpgrade(version, schema));

                dgFixItems.ItemsSource = _fixItems;

                var fromPrev = _fixItems.Count(i => i.Source == "Previous CREATE");
                var fromUpg = _fixItems.Count(i => i.Source == "UPGRADE");

                txtSummary.Text = $"Version: {version} | Schema: {schema} | " +
                    $"Total missing: {_fixItems.Count} — From previous CREATE: {fromPrev}, From UPGRADE: {fromUpg}";
            }
            catch (Exception ex)
            {
                ThemedDialog.Show($"Error: {ex.Message}", "Error");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void DgFixItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgFixItems.SelectedItem is not FixItem selected)
            {
                txtScriptPreview.Text = "";
                return;
            }

            txtScriptPreview.Text = selected.SqlContent;
        }

        private void FixSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _fixItems)
                item.IsSelected = true;
            dgFixItems.Items.Refresh();
        }

        private void FixDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _fixItems)
                item.IsSelected = false;
            dgFixItems.Items.Refresh();
        }

        private void FixApply_Click(object sender, RoutedEventArgs e)
        {
            if (_fixService == null || cboVersion.SelectedItem == null) return;

            var selected = _fixItems.Count(i => i.IsSelected);
            if (selected == 0)
            {
                ThemedDialog.Show("No items selected.", "Apply Fix");
                return;
            }

            if (!ThemedDialog.Confirm($"Apply {selected} fix(es) to CREATE scripts?", "Confirm Apply"))
                return;

            var version = cboVersion.SelectedItem.ToString();
            var schema = ((ComboBoxItem)cboSchema.SelectedItem).Content.ToString();

            try
            {
                var applied = _fixService.ApplyFixes(version, schema, _fixItems);
                ThemedDialog.Show($"Applied {applied} fix(es) successfully.", "Apply Fix");
            }
            catch (Exception ex)
            {
                ThemedDialog.Show($"Error: {ex.Message}", "Error");
            }
        }
    }
}
