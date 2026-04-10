using DBTool.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DBTool.Controls
{
    public partial class CompressFdbControl : UserControl
    {
        private List<FdbResult> _fdbResults = new();

        public CompressFdbControl()
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

                // Populate version combo from subdirectories of CREATE
                var createDir = Path.Combine(dialog.SelectedPath, "CREATE");
                if (Directory.Exists(createDir))
                {
                    var versions = Directory.GetDirectories(createDir)
                        .Select(d => Path.GetFileName(d))
                        .OrderByDescending(v => v)
                        .ToList();
                    cboVersion.ItemsSource = versions;
                    if (versions.Count > 0) cboVersion.SelectedIndex = 0;
                }

                // Load AWS profiles
                try
                {
                    var profiles = S3Service.GetAwsProfiles();
                    cboAwsProfile.ItemsSource = profiles;
                    if (profiles.Count > 0) cboAwsProfile.SelectedIndex = 0;
                }
                catch { /* AWS SDK not configured */ }
            }
        }

        private void CompressFdb_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtRootFolder.Text))
            {
                ThemedDialog.Show("Please select a root folder first.", "No Folder");
                return;
            }

            if (cboVersion.SelectedItem == null)
            {
                ThemedDialog.Show("Please select a version.", "No Version");
                return;
            }

            var version = cboVersion.SelectedItem.ToString()!;
            var rootFolder = txtRootFolder.Text;

            var folders = new List<string>();
            if (chkCreate.IsChecked == true) folders.Add("CREATE");
            if (chkUpgrade.IsChecked == true) folders.Add("UPGRADE");

            if (folders.Count == 0)
            {
                ThemedDialog.Show("Please select at least one folder (CREATE or UPGRADE).", "No Selection");
                return;
            }

            var schemas = new[] { "FSH", "SCAH" };
            var results = new List<FdbResult>();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                foreach (var folder in folders)
                {
                    foreach (var schema in schemas)
                    {
                        var sourceDir = Path.Combine(rootFolder, folder, version, schema);
                        var fdbPath = Path.Combine(rootFolder, folder, version, $"{schema}.FDB");
                        var s3Prefix = txtS3Prefix.Text.Trim();
                        var s3Bucket = txtS3Bucket.Text.Trim();
                        var outputRelative = $"{folder}/{version}/{schema}.FDB";
                        var result = new FdbResult
                        {
                            Folder = folder,
                            Schema = schema,
                            OutputPath = outputRelative,
                            S3Path = !string.IsNullOrEmpty(s3Bucket)
                                ? $"s3://{s3Bucket}/{s3Prefix}{outputRelative}"
                                : "(configure bucket)"
                        };

                        if (!Directory.Exists(sourceDir))
                        {
                            result.Status = "Skipped";
                            result.FileSize = "—";
                            LogConsole($"Skipped {folder}/{schema} — directory not found");
                            results.Add(result);
                            continue;
                        }

                        try
                        {
                            LogConsole($"Compressing {folder}/{version}/{schema}...");

                            if (File.Exists(fdbPath))
                                File.Delete(fdbPath);

                            var tempZip = fdbPath + ".tmp";
                            ZipFile.CreateFromDirectory(sourceDir, tempZip);
                            File.Move(tempZip, fdbPath);

                            var fileInfo = new FileInfo(fdbPath);
                            result.Status = "Created";
                            result.FileSize = FormatFileSize(fileInfo.Length);
                            result.LocalFullPath = fdbPath;
                            LogConsole($"  ✓ Created {schema}.FDB ({result.FileSize})");
                        }
                        catch (Exception ex)
                        {
                            result.Status = "Error";
                            result.FileSize = "—";
                            LogConsole($"  ✗ Error: {ex.Message}");
                        }

                        results.Add(result);
                    }
                }

                _fdbResults = results;
                dgFdbResults.ItemsSource = results;

                var created = results.Count(r => r.Status == "Created");
                txtSummary.Text = $"Version: {version} | Created: {created} FDB file(s) | " +
                                  $"Skipped: {results.Count(r => r.Status == "Skipped")} | " +
                                  $"Errors: {results.Count(r => r.Status.StartsWith("Error"))}";
            }
            catch (Exception ex)
            {
                ThemedDialog.Show($"Error compressing:\n{ex.Message}", "Error");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void UploadS3_Click(object sender, RoutedEventArgs e)
        {
            if (cboAwsProfile.SelectedItem == null)
            {
                ThemedDialog.Show("Please select an AWS profile.", "No Profile");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtS3Bucket.Text))
            {
                ThemedDialog.Show("Please enter an S3 bucket name.", "No Bucket");
                return;
            }

            var createdFiles = _fdbResults.Where(r => r.Status == "Created" && !string.IsNullOrEmpty(r.LocalFullPath)).ToList();
            if (createdFiles.Count == 0)
            {
                ThemedDialog.Show("No FDB files to upload. Compress first.", "No Files");
                return;
            }

            var profile = cboAwsProfile.SelectedItem.ToString()!;
            var region = txtAwsRegion.Text.Trim();
            var bucket = txtS3Bucket.Text.Trim();
            var prefix = txtS3Prefix.Text.Trim();

            if (!ThemedDialog.Confirm(
                $"Upload {createdFiles.Count} file(s) to s3://{bucket}/{prefix}...?\n\nProfile: {profile}\nRegion: {region}",
                "Confirm Upload"))
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                LogConsole($"Starting upload to s3://{bucket}/{prefix}...");
                LogConsole($"Profile: {profile} | Region: {region}");
                LogConsole($"Files to upload: {createdFiles.Count}");
                LogConsole("");

                foreach (var file in createdFiles)
                {
                    var s3Key = prefix + file.OutputPath.Replace("\\", "/");
                    LogConsole($"Uploading {file.OutputPath} ({file.FileSize})...");
                    LogConsole($"  → s3://{bucket}/{s3Key}");

                    try
                    {
                        await S3Service.UploadFileAsync(profile, region, bucket, s3Key, file.LocalFullPath);
                        file.S3Status = "Uploaded";
                        LogConsole($"  ✓ Upload complete");
                    }
                    catch (Exception ex)
                    {
                        file.S3Status = "Error";
                        LogConsole($"  ✗ Error: {ex.Message}");
                    }
                    LogConsole("");
                }

                // Refresh grid
                dgFdbResults.ItemsSource = null;
                dgFdbResults.ItemsSource = _fdbResults;

                var uploaded = createdFiles.Count(f => f.S3Status == "Uploaded");
                var errors = createdFiles.Count(f => f.S3Status == "Error");

                LogConsole($"Upload complete. {uploaded} succeeded, {errors} failed.");
                txtSummary.Text += $" | S3: {uploaded} uploaded, {errors} errors";
            }
            catch (Exception ex)
            {
                ThemedDialog.Show($"Upload error:\n{ex.Message}", "Error");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void LogConsole(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtConsoleLog.AppendText($"[{timestamp}] {message}\n");
            txtConsoleLog.ScrollToEnd();
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}
