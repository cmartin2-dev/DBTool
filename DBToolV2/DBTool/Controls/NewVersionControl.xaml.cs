using DBTool.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DBTool.Controls
{
    public partial class NewVersionControl : UserControl
    {
        private List<ScaffoldItem> _scaffoldItems = new();

        public NewVersionControl()
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

                var createDir = Path.Combine(dialog.SelectedPath, "CREATE");
                if (Directory.Exists(createDir))
                {
                    var versions = Directory.GetDirectories(createDir)
                        .Select(d => Path.GetFileName(d))
                        .OrderByDescending(v => v)
                        .ToList();
                    cboBaseVersion.ItemsSource = versions;
                    if (versions.Count > 0) cboBaseVersion.SelectedIndex = 0;
                }
            }
        }

        private void BrowseTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the UPGRADE template folder"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtTemplateFolder.Text = dialog.SelectedPath;
            }
        }

        private void ScaffoldPreview_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRootFolder.Text))
            {
                ThemedDialog.Show("Please select a root folder first.", "No Folder");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtNewVersion.Text))
            {
                ThemedDialog.Show("Please enter a new version.", "No Version");
                return;
            }
            if (cboBaseVersion.SelectedItem == null)
            {
                ThemedDialog.Show("Please select a base version.", "No Base Version");
                return;
            }

            var newVersion = txtNewVersion.Text.Trim();
            var baseVersion = cboBaseVersion.SelectedItem.ToString()!;
            var rootFolder = txtRootFolder.Text.Trim();

            _scaffoldItems = new List<ScaffoldItem>();

            // Copy items from CREATE/baseVersion
            var createSourceDir = Path.Combine(rootFolder, "CREATE", baseVersion);
            if (Directory.Exists(createSourceDir))
            {
                var createDestDir = Path.Combine(rootFolder, "CREATE", newVersion);
                foreach (var file in Directory.GetFiles(createSourceDir, "*.*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(createSourceDir, file);
                    _scaffoldItems.Add(new ScaffoldItem
                    {
                        Action = "Copy",
                        Source = file,
                        Destination = Path.Combine(createDestDir, relativePath),
                        Status = "Pending"
                    });
                }
            }

            // Template items from UPGRADE template folder
            if (!string.IsNullOrWhiteSpace(txtTemplateFolder.Text) && Directory.Exists(txtTemplateFolder.Text))
            {
                var templateDir = txtTemplateFolder.Text.Trim();
                var upgradeDestDir = Path.Combine(rootFolder, "UPGRADE", newVersion);
                foreach (var file in Directory.GetFiles(templateDir, "*.*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(templateDir, file);
                    _scaffoldItems.Add(new ScaffoldItem
                    {
                        Action = "Template",
                        Source = file,
                        Destination = Path.Combine(upgradeDestDir, relativePath),
                        Status = "Pending"
                    });
                }
            }

            dgScaffoldItems.ItemsSource = null;
            dgScaffoldItems.ItemsSource = _scaffoldItems;

            var copyCount = _scaffoldItems.Count(i => i.Action == "Copy");
            var templateCount = _scaffoldItems.Count(i => i.Action == "Template");
            txtSummary.Text = $"Preview: {_scaffoldItems.Count} item(s) — Copy: {copyCount}, Template: {templateCount}. " +
                $"Base: {baseVersion} → New: {newVersion}";
        }

        private void ScaffoldCreate_Click(object sender, RoutedEventArgs e)
        {
            if (_scaffoldItems.Count == 0)
            {
                ThemedDialog.Show("Please preview first.", "No Items");
                return;
            }

            var newVersion = txtNewVersion.Text.Trim();
            var baseVersion = cboBaseVersion.SelectedItem.ToString()!;

            if (!ThemedDialog.Confirm($"Create version '{newVersion}' from base '{baseVersion}'?\n\n{_scaffoldItems.Count} file(s) will be created.", "Confirm Create"))
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                foreach (var item in _scaffoldItems)
                {
                    try
                    {
                        var destDir = Path.GetDirectoryName(item.Destination)!;
                        if (!Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        if (item.Action == "Copy")
                        {
                            CopyWithReplacements(item.Source, item.Destination, baseVersion, newVersion);
                        }
                        else if (item.Action == "Template")
                        {
                            CopyWithReplacements(item.Source, item.Destination, "{new_version}", newVersion);
                        }

                        item.Status = "Created";
                    }
                    catch (Exception ex)
                    {
                        item.Status = $"Error: {ex.Message}";
                    }
                }

                dgScaffoldItems.ItemsSource = null;
                dgScaffoldItems.ItemsSource = _scaffoldItems;

                var created = _scaffoldItems.Count(i => i.Status == "Created");
                var errors = _scaffoldItems.Count(i => i.Status.StartsWith("Error"));
                txtSummary.Text = $"Done — Created: {created}, Errors: {errors}";

                ThemedDialog.Show($"Version '{newVersion}' created.\nCreated: {created}, Errors: {errors}", "Complete");
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

        private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".sql", ".xml", ".txt", ".ddl"
        };

        private static void CopyWithReplacements(string sourcePath, string destPath, string oldValue, string newValue)
        {
            var ext = Path.GetExtension(sourcePath);
            if (TextExtensions.Contains(ext))
            {
                var content = File.ReadAllText(sourcePath);
                content = content.Replace(oldValue, newValue);
                File.WriteAllText(destPath, content);
            }
            else
            {
                File.Copy(sourcePath, destPath, overwrite: true);
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir, string oldValue, string newValue)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                CopyWithReplacements(file, destFile, oldValue, newValue);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir, oldValue, newValue);
            }
        }
    }
}
