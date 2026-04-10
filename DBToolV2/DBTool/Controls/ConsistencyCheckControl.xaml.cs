using DBTool.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DBTool.Controls
{
    public partial class ConsistencyCheckControl : UserControl
    {
        private SqlAnalyzer _analyzer;
        private List<AnalysisResult> _results = new();
        private string _selectedVersion = "";
        private string _selectedSchema = "";

        public ConsistencyCheckControl()
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
                _analyzer = new SqlAnalyzer(dialog.SelectedPath);
                var versions = _analyzer.GetVersions();
                cboVersion.ItemsSource = versions;
                if (versions.Count > 0) cboVersion.SelectedIndex = 0;
                txtSummary.Text = $"Found {versions.Count} versions.";
            }
        }

        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            if (_analyzer == null)
            {
                ThemedDialog.Show("Please select a root folder first.", "No Folder");
                return;
            }
            if (cboVersion.SelectedItem == null) return;

            _selectedVersion = cboVersion.SelectedItem.ToString();
            _selectedSchema = ((ComboBoxItem)cboSchema.SelectedItem).Content.ToString();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _results = _analyzer.Analyze(_selectedVersion, _selectedSchema);
                dgResults.ItemsSource = _results;

                var missing = _results.Count(r => r.Status == "Missing");
                var present = _results.Count(r => r.Status == "Present");
                var appended = _results.Count(r => r.Status == "Appended as ALTER");

                txtSummary.Text = $"Version: {_selectedVersion} | Schema: {_selectedSchema} | " +
                    $"Total: {_results.Count} — Missing: {missing}, Present: {present}, Appended: {appended}";
                txtResultCount.Text = $"{_results.Count} items";
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

        private void DgResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgResults.SelectedItem is not AnalysisResult selected)
            {
                txtUpgradeScript.Text = "";
                txtCreateScript.Text = "";
                return;
            }

            txtUpgradeHeader.Text = $"UPGRADE — {selected.UpgradeFileName}";
            txtUpgradeScript.Text = selected.UpgradeScript.TrimEnd();
            txtCreateHeader.Text = !string.IsNullOrEmpty(selected.CreateSnippet)
                ? $"CREATE — {selected.CreateFileName}" : "CREATE — (not found)";
            txtCreateScript.Text = !string.IsNullOrEmpty(selected.CreateSnippet)
                ? selected.CreateSnippet.TrimEnd()
                : selected.Status == "Missing" ? $"No reference to [{selected.ObjectName}] found." : "";
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0) { ThemedDialog.Show("No results.", "Export"); return; }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                FileName = $"Check_{_selectedVersion}_{_selectedSchema}.csv"
            };
            if (dialog.ShowDialog() == true)
            {
                ExportService.ExportToCsv(_results, dialog.FileName);
                ThemedDialog.Show("Exported.", "Export");
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0) { ThemedDialog.Show("No results.", "Export"); return; }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"Check_{_selectedVersion}_{_selectedSchema}.xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                ExportService.ExportToExcel(_results, dialog.FileName, _selectedVersion, _selectedSchema);
                ThemedDialog.Show("Exported.", "Export");
            }
        }
    }
}
