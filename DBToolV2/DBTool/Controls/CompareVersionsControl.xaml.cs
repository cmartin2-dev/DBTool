using DBTool.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DBTool.Controls
{
    public partial class CompareVersionsControl : UserControl
    {
        private SqlAnalyzer _analyzer;

        public CompareVersionsControl()
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
                cboVersionA.ItemsSource = versions;
                cboVersionB.ItemsSource = versions;
                if (versions.Count > 0)
                {
                    cboVersionA.SelectedIndex = 0;
                    cboVersionB.SelectedIndex = versions.Count > 1 ? 1 : 0;
                }
                txtSummary.Text = $"Found {versions.Count} versions.";
            }
        }

        private void CboVersionA_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateFileCombo();
        }

        private void CboSchema_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateFileCombo();
        }

        private void PopulateFileCombo()
        {
            if (_analyzer == null || cboVersionA.SelectedItem == null || cboSchema.SelectedItem == null)
                return;

            var version = cboVersionA.SelectedItem.ToString();
            var schema = ((ComboBoxItem)cboSchema.SelectedItem).Content.ToString();

            var files = _analyzer.GetCreateFiles(version, schema);
            cboFile.ItemsSource = files;
            if (files.Count > 0) cboFile.SelectedIndex = 0;
        }

        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            if (_analyzer == null)
            {
                ThemedDialog.Show("Please select a root folder first.", "No Folder");
                return;
            }
            if (cboVersionA.SelectedItem == null || cboVersionB.SelectedItem == null)
            {
                ThemedDialog.Show("Please select both versions.", "Missing Version");
                return;
            }
            if (cboFile.SelectedItem == null)
            {
                ThemedDialog.Show("Please select a file to compare.", "No File");
                return;
            }

            var versionA = cboVersionA.SelectedItem.ToString();
            var versionB = cboVersionB.SelectedItem.ToString();
            var schema = ((ComboBoxItem)cboSchema.SelectedItem).Content.ToString();
            var fileName = cboFile.SelectedItem.ToString();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var contentA = _analyzer.GetCreateFileContent(versionA, schema, fileName);
                var contentB = _analyzer.GetCreateFileContent(versionB, schema, fileName);

                var diffs = DiffEngine.ComputeScriptDiff(contentA, contentB);

                var present = diffs.Count(d => d.Type == "Present");
                var modified = diffs.Count(d => d.Type == "Modified");
                var missing = diffs.Count(d => d.Type == "Missing");

                txtSummary.Text = $"Version A: {versionA} | Version B: {versionB} | Schema: {schema} | File: {fileName} — " +
                    $"Total: {diffs.Count} — Present: {present}, Modified: {modified}, Missing: {missing}";

                txtHeaderA.Text = $"Version A — {versionA} / {schema} / {fileName}";
                txtHeaderB.Text = $"Version B — {versionB} / {schema} / {fileName}";

                txtContentA.Text = DiffEngine.AddLineNumbers(contentA);
                txtContentB.Text = DiffEngine.AddLineNumbers(contentB);
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
    }
}
