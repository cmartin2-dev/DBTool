using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using SqlCreateUpgradeChecker.Models;
using SqlCreateUpgradeChecker.Services;

namespace SqlCreateUpgradeChecker;

public partial class MainWindow : Window
{
    private SqlAnalyzer? _analyzer;
    private List<AnalysisResult> _results = [];
    private string _selectedVersion = string.Empty;
    private string _selectedSchema = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        cboSchema.SelectedIndex = 0;
        cboCompareSchema.SelectedIndex = 0;
        cboFixSchema.SelectedIndex = 0;
        cboCompareSchema.SelectionChanged += CboCompareInputs_Changed;
        cboCompareVersionA.SelectionChanged += CboCompareInputs_Changed;
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the SQLSERVER root folder (containing CREATE and UPGRADE folders)",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            txtRootFolder.Text = dialog.SelectedPath;
            _analyzer = new SqlAnalyzer(dialog.SelectedPath);

            var versions = _analyzer.GetVersions();
            cboVersion.ItemsSource = versions;
            if (versions.Count > 0)
                cboVersion.SelectedIndex = 0;

            // Also populate compare tab dropdowns
            cboCompareVersionA.ItemsSource = versions;
            cboCompareVersionB.ItemsSource = versions;
            if (versions.Count > 1)
            {
                cboCompareVersionA.SelectedIndex = versions.Count - 2;
                cboCompareVersionB.SelectedIndex = versions.Count - 1;
            }
            else if (versions.Count > 0)
            {
                cboCompareVersionA.SelectedIndex = 0;
                cboCompareVersionB.SelectedIndex = 0;
            }

            // Populate fix tab
            cboFixVersion.ItemsSource = versions;
            if (versions.Count > 0)
                cboFixVersion.SelectedIndex = versions.Count - 1;

            // Populate FDB tab
            cboFdbVersion.ItemsSource = versions;
            if (versions.Count > 0)
                cboFdbVersion.SelectedIndex = versions.Count - 1;

            // Load AWS profiles
            var profiles = S3Service.GetAwsProfiles();
            cboAwsProfile.ItemsSource = profiles;
            if (profiles.Count > 0)
                cboAwsProfile.SelectedIndex = 0;

            // Populate scaffold tab
            cboBaseVersion.ItemsSource = versions;
            if (versions.Count > 0)
                cboBaseVersion.SelectedIndex = versions.Count - 1;

            txtSummary.Text = $"Found {versions.Count} versions. Select a version and schema, then click Analyze.";
        }
    }

    private void Analyze_Click(object sender, RoutedEventArgs e)
    {
        if (_analyzer == null)
        {
            System.Windows.MessageBox.Show("Please select a root folder first.", "No Folder",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboVersion.SelectedItem == null)
        {
            System.Windows.MessageBox.Show("Please select a version.", "No Version",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selectedVersion = cboVersion.SelectedItem.ToString()!;
        _selectedSchema = ((ComboBoxItem)cboSchema.SelectedItem).Content.ToString()!;

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            _results = _analyzer.Analyze(_selectedVersion, _selectedSchema);
            dgResults.ItemsSource = _results;

            var missing = _results.Count(r => r.Status == "Missing");
            var appended = _results.Count(r => r.Status == "Appended as ALTER");
            var wrongType = _results.Count(r => r.Status == "Wrong Type in CREATE");
            var stillPresent = _results.Count(r => r.Status == "Column Still in CREATE");
            var present = _results.Count(r => r.Status == "Present");
            var other = _results.Count - missing - appended - wrongType - stillPresent - present;

            txtSummary.Text = $"Version: {_selectedVersion} | Schema: {_selectedSchema} | " +
                              $"Total: {_results.Count} changes found — " +
                              $"Missing: {missing}, Appended as ALTER: {appended}, Wrong Type: {wrongType}, " +
                              $"Column Still Present: {stillPresent}, Present: {present}, Other: {other}";

            txtResultCount.Text = $"{_results.Count} items";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error during analysis:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
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
            txtUpgradeScript.Text = string.Empty;
            txtCreateScript.Text = string.Empty;
            txtUpgradeHeader.Text = "UPGRADE Script";
            txtCreateHeader.Text = "CREATE Script";
            return;
        }

        txtUpgradeHeader.Text = $"UPGRADE Script — {selected.UpgradeFileName}";
        txtUpgradeScript.Text = selected.UpgradeScript.TrimEnd();

        if (!string.IsNullOrEmpty(selected.CreateSnippet))
        {
            txtCreateHeader.Text = $"CREATE Script — {selected.CreateFileName}";
            txtCreateScript.Text = selected.CreateSnippet.TrimEnd();
        }
        else
        {
            txtCreateHeader.Text = "CREATE Script — (not found)";
            txtCreateScript.Text = selected.Status == "Missing"
                ? $"No reference to [{selected.ObjectName}] found in CREATE scripts."
                : string.Empty;
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
        {
            System.Windows.MessageBox.Show("No results to export. Run an analysis first.", "No Data",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"CreateUpgradeCheck_{_selectedVersion}_{_selectedSchema}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                ExportService.ExportToCsv(_results, dialog.FileName);
                System.Windows.MessageBox.Show($"Exported to {dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
        {
            System.Windows.MessageBox.Show("No results to export. Run an analysis first.", "No Data",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"CreateUpgradeCheck_{_selectedVersion}_{_selectedSchema}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                ExportService.ExportToExcel(_results, dialog.FileName, _selectedVersion, _selectedSchema);
                System.Windows.MessageBox.Show($"Exported to {dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CboCompareInputs_Changed(object sender, SelectionChangedEventArgs e)
    {
        PopulateCompareFileList();
    }

    private void PopulateCompareFileList()
    {
        if (_analyzer == null || cboCompareVersionA.SelectedItem == null || cboCompareSchema.SelectedItem == null)
            return;

        var version = cboCompareVersionA.SelectedItem.ToString()!;
        var schema = ((ComboBoxItem)cboCompareSchema.SelectedItem).Content.ToString()!;

        var files = _analyzer.GetCreateFiles(version, schema);
        cboCompareFile.ItemsSource = files;
        if (files.Count > 0)
            cboCompareFile.SelectedIndex = 0;
    }

    private List<DiffResult> _diffs = [];
    private string _rawContentA = string.Empty;
    private string _rawContentB = string.Empty;
    private List<DiffResult> _allResults = [];

    private void Compare_Click(object sender, RoutedEventArgs e)
    {
        if (_analyzer == null)
        {
            System.Windows.MessageBox.Show("Please select a root folder first.", "No Folder",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboCompareVersionA.SelectedItem == null || cboCompareVersionB.SelectedItem == null)
        {
            System.Windows.MessageBox.Show("Please select both versions.", "Missing Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboCompareFile.SelectedItem == null)
        {
            System.Windows.MessageBox.Show("Please select a file to compare.", "Missing Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var versionA = cboCompareVersionA.SelectedItem.ToString()!;
        var versionB = cboCompareVersionB.SelectedItem.ToString()!;
        var schema = ((ComboBoxItem)cboCompareSchema.SelectedItem).Content.ToString()!;
        var fileName = cboCompareFile.SelectedItem.ToString()!;

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            _rawContentA = _analyzer.GetCreateFileContent(versionA, schema, fileName);
            _rawContentB = _analyzer.GetCreateFileContent(versionB, schema, fileName);

            txtCompareHeaderA.Text = $"Version {versionA} / {schema} / {fileName}";
            txtCompareHeaderB.Text = $"Version {versionB} / {schema} / {fileName}";

            // Load content into RichTextBoxes with line numbers
            LoadRichTextContent(rtbCompareScriptA, _rawContentA);
            LoadRichTextContent(rtbCompareScriptB, _rawContentB);

            var identical = _rawContentA == _rawContentB;

            _allResults = identical ? [] : DiffEngine.ComputeScriptDiff(_rawContentA, _rawContentB);

            var blocksA = DiffEngine.ParseScriptBlocks(_rawContentA);
            var blocksB = DiffEngine.ParseScriptBlocks(_rawContentB);

            var missing = _allResults.Count(d => d.Type == "Missing");
            var modified = _allResults.Count(d => d.Type == "Modified");
            var present = _allResults.Count(d => d.Type == "Present");

            _diffs = _allResults.Where(d => d.Type != "Present").ToList();
            for (int i = 0; i < _diffs.Count; i++)
                _diffs[i].Index = i + 1;

            cboDiffList.ItemsSource = _diffs;
            if (_diffs.Count > 0)
                cboDiffList.SelectedIndex = 0;

            txtCompareSummary.Text = identical
                ? $"Files are identical. A: {blocksA.Count} statements | B: {blocksB.Count} statements"
                : $"A: {blocksA.Count} statements | B: {blocksB.Count} statements | " +
                  $"Present: {present} | Modified: {modified} | Missing in B: {missing}";

            txtDiffPosition.Text = _diffs.Count > 0 ? $"1 / {_diffs.Count}" : "No differences";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error during comparison:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private static void LoadRichTextContent(System.Windows.Controls.RichTextBox rtb, string content)
    {
        var doc = new FlowDocument { PageWidth = 10000 };
        var lines = content.Replace("\r", "").Split('\n');
        var width = lines.Length.ToString().Length;

        var para = new Paragraph { Margin = new Thickness(0), LineHeight = 1 };
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) para.Inlines.Add(new LineBreak());
            var lineNum = (i + 1).ToString().PadLeft(width);
            var run = new Run($"{lineNum}  {lines[i].TrimEnd()}");
            para.Inlines.Add(run);
        }
        doc.Blocks.Add(para);
        rtb.Document = doc;
    }

    private void CboDiffList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboDiffList.SelectedItem is not DiffResult diff) return;
        NavigateToDiff(diff);
    }

    private void PrevDiff_Click(object sender, RoutedEventArgs e)
    {
        if (_diffs.Count == 0) return;
        var idx = cboDiffList.SelectedIndex;
        if (idx > 0)
            cboDiffList.SelectedIndex = idx - 1;
    }

    private void NextDiff_Click(object sender, RoutedEventArgs e)
    {
        if (_diffs.Count == 0) return;
        var idx = cboDiffList.SelectedIndex;
        if (idx < _diffs.Count - 1)
            cboDiffList.SelectedIndex = idx + 1;
    }

    private void NavigateToDiff(DiffResult diff)
    {
        txtDiffPosition.Text = $"{diff.Index} / {_diffs.Count}";

        // Clear previous highlights
        ClearHighlights(rtbCompareScriptA);
        ClearHighlights(rtbCompareScriptB);

        // Highlight and scroll left panel
        if (diff.LineA > 0)
        {
            var endLineA = FindBlockEndLine(_rawContentA, diff.LineA);
            HighlightLines(rtbCompareScriptA, diff.LineA, endLineA,
                diff.Type == "Missing" ? Colors.MistyRose : Colors.LightYellow);
            ScrollToLineRtb(rtbCompareScriptA, diff.LineA);
        }

        // Highlight and scroll right panel
        if (diff.LineB > 0)
        {
            var endLineB = FindBlockEndLine(_rawContentB, diff.LineB);
            HighlightLines(rtbCompareScriptB, diff.LineB, endLineB, Colors.LightYellow);
            ScrollToLineRtb(rtbCompareScriptB, diff.LineB);
        }
    }

    private static int FindBlockEndLine(string content, int startLine)
    {
        var lines = content.Replace("\r", "").Split('\n');
        // Find the next GO after startLine
        for (int i = startLine; i < lines.Length; i++)
        {
            if (lines[i].Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }
        return Math.Min(startLine + 50, lines.Length);
    }

    private static void ClearHighlights(System.Windows.Controls.RichTextBox rtb)
    {
        var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
        range.ApplyPropertyValue(TextElement.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
    }

    private static void HighlightLines(System.Windows.Controls.RichTextBox rtb, int startLine, int endLine, System.Windows.Media.Color color)
    {
        var doc = rtb.Document;
        var para = doc.Blocks.FirstBlock as Paragraph;
        if (para == null) return;

        int currentLine = 1;
        foreach (var inline in para.Inlines)
        {
            if (inline is Run run && currentLine >= startLine && currentLine <= endLine)
            {
                var range = new TextRange(run.ContentStart, run.ContentEnd);
                range.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(color));
            }
            if (inline is LineBreak)
                currentLine++;
        }
    }

    private static void ScrollToLineRtb(System.Windows.Controls.RichTextBox rtb, int lineNumber)
    {
        var doc = rtb.Document;
        var para = doc.Blocks.FirstBlock as Paragraph;
        if (para == null) return;

        int currentLine = 1;
        // Scroll a few lines before target
        var targetLine = Math.Max(1, lineNumber - 3);

        foreach (var inline in para.Inlines)
        {
            if (inline is Run run && currentLine == targetLine)
            {
                var pos = run.ContentStart;
                var rect = pos.GetCharacterRect(LogicalDirection.Forward);
                rtb.ScrollToVerticalOffset(rtb.VerticalOffset + rect.Top - 20);
                return;
            }
            if (inline is LineBreak)
                currentLine++;
        }
    }

    // ===== TAB 3: Apply Fix =====

    private List<FixItem> _fixItems = [];

    private void FixScan_Click(object sender, RoutedEventArgs e)
    {
        if (_analyzer == null)
        {
            System.Windows.MessageBox.Show("Please select a root folder first.", "No Folder",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboFixVersion.SelectedItem == null)
        {
            System.Windows.MessageBox.Show("Please select a version.", "No Version",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var version = cboFixVersion.SelectedItem.ToString()!;
        var schema = ((ComboBoxItem)cboFixSchema.SelectedItem).Content.ToString()!;

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            var fixService = new FixService(txtRootFolder.Text);
            var versions = fixService.GetVersions();
            var prevVersion = fixService.GetPreviousVersion(version, versions);

            _fixItems = [];

            // 1. Find missing from previous CREATE
            if (prevVersion != null)
            {
                var fromPrev = fixService.FindMissingFromPrevious(version, prevVersion, schema);
                _fixItems.AddRange(fromPrev);
            }

            // 2. Find missing from UPGRADE
            var fromUpgrade = fixService.FindMissingFromUpgrade(version, schema);
            _fixItems.AddRange(fromUpgrade);

            dgFixItems.ItemsSource = null;
            dgFixItems.ItemsSource = _fixItems;

            var fromPrevCount = _fixItems.Count(i => i.Source == "Previous CREATE");
            var fromUpgCount = _fixItems.Count(i => i.Source == "UPGRADE");

            txtFixSummary.Text = prevVersion != null
                ? $"Version: {version} | Schema: {schema} | Previous: {prevVersion} | " +
                  $"Missing from previous CREATE: {fromPrevCount} | Missing from UPGRADE: {fromUpgCount} | Total: {_fixItems.Count}"
                : $"Version: {version} | Schema: {schema} | No previous version | " +
                  $"Missing from UPGRADE: {fromUpgCount} | Total: {_fixItems.Count}";

            txtFixPreview.Text = string.Empty;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error during scan:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void DgFixItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgFixItems.SelectedItem is not FixItem item) return;

        txtFixPreviewHeader.Text = $"Preview — {item.Source} → {item.TargetFile}";
        txtFixPreview.Text = item.SqlContent.TrimEnd();
    }

    private void FixSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _fixItems) item.IsSelected = true;
        dgFixItems.ItemsSource = null;
        dgFixItems.ItemsSource = _fixItems;
    }

    private void FixDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _fixItems) item.IsSelected = false;
        dgFixItems.ItemsSource = null;
        dgFixItems.ItemsSource = _fixItems;
    }

    private void FixApply_Click(object sender, RoutedEventArgs e)
    {
        var selected = _fixItems.Count(i => i.IsSelected);
        if (selected == 0)
        {
            System.Windows.MessageBox.Show("No items selected.", "Nothing to Apply",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var version = cboFixVersion.SelectedItem?.ToString() ?? "";
        var schema = ((ComboBoxItem)cboFixSchema.SelectedItem).Content.ToString()!;

        var result = System.Windows.MessageBox.Show(
            $"Apply {selected} fix(es) to CREATE/{version}/{schema}?\n\nThis will append SQL to the CREATE files. This cannot be undone.",
            "Confirm Apply",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var fixService = new FixService(txtRootFolder.Text);
            var applied = fixService.ApplyFixes(version, schema, _fixItems);

            System.Windows.MessageBox.Show($"Applied {applied} fix(es) successfully.\n\nUse Tab 1 or Tab 2 to verify.",
                "Done", MessageBoxButton.OK, MessageBoxImage.Information);

            // Re-scan to refresh
            FixScan_Click(sender, e);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error applying fixes:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CompressFdb_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(txtRootFolder.Text))
        {
            System.Windows.MessageBox.Show("Please select a root folder first.", "No Folder",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboFdbVersion.SelectedItem == null)
        {
            System.Windows.MessageBox.Show("Please select a version.", "No Version",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var version = cboFdbVersion.SelectedItem.ToString()!;
        var rootFolder = txtRootFolder.Text;

        var folders = new List<string>();
        if (chkFdbCreate.IsChecked == true) folders.Add("CREATE");
        if (chkFdbUpgrade.IsChecked == true) folders.Add("UPGRADE");

        if (folders.Count == 0)
        {
            System.Windows.MessageBox.Show("Please select at least one folder (CREATE or UPGRADE).", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var schemas = new[] { "FSH", "SCAH" };
        var results = new List<FdbResult>();

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            foreach (var folder in folders)
            {
                foreach (var schema in schemas)
                {
                    var sourceDir = System.IO.Path.Combine(rootFolder, folder, version, schema);
                    var fdbPath = System.IO.Path.Combine(rootFolder, folder, version, $"{schema}.FDB");
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

                    if (!System.IO.Directory.Exists(sourceDir))
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

                        if (System.IO.File.Exists(fdbPath))
                            System.IO.File.Delete(fdbPath);

                        var tempZip = fdbPath + ".tmp";
                        System.IO.Compression.ZipFile.CreateFromDirectory(sourceDir, tempZip);
                        System.IO.File.Move(tempZip, fdbPath);

                        var fileInfo = new System.IO.FileInfo(fdbPath);
                        result.Status = "Created";
                        result.FileSize = FormatFileSize(fileInfo.Length);
                        result.LocalFullPath = fdbPath;
                        LogConsole($"  ✓ Created {schema}.FDB ({result.FileSize})");
                    }
                    catch (Exception ex)
                    {
                        result.Status = $"Error";
                        result.FileSize = "—";
                        LogConsole($"  ✗ Error: {ex.Message}");
                    }

                    results.Add(result);
                }
            }

            _fdbResults = results;
            dgFdbResults.ItemsSource = results;

            var created = results.Count(r => r.Status == "Created");
            txtFdbSummary.Text = $"Version: {version} | Created: {created} FDB file(s) | " +
                                 $"Skipped: {results.Count(r => r.Status == "Skipped")} | " +
                                 $"Errors: {results.Count(r => r.Status.StartsWith("Error"))}";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error compressing:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private List<FdbResult> _fdbResults = [];

    private void LogConsole(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        txtConsoleLog.AppendText($"[{timestamp}] {message}\n");
        txtConsoleLog.ScrollToEnd();
    }

    private async void UploadS3_Click(object sender, RoutedEventArgs e)
    {
        if (cboAwsProfile.SelectedItem == null)
        {
            System.Windows.MessageBox.Show("Please select an AWS profile.", "No Profile",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtS3Bucket.Text))
        {
            System.Windows.MessageBox.Show("Please enter an S3 bucket name.", "No Bucket",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var createdFiles = _fdbResults.Where(r => r.Status == "Created" && !string.IsNullOrEmpty(r.LocalFullPath)).ToList();
        if (createdFiles.Count == 0)
        {
            System.Windows.MessageBox.Show("No FDB files to upload. Compress first.", "No Files",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var profile = cboAwsProfile.SelectedItem.ToString()!;
        var region = txtAwsRegion.Text.Trim();
        var bucket = txtS3Bucket.Text.Trim();
        var prefix = txtS3Prefix.Text.Trim();

        var confirm = System.Windows.MessageBox.Show(
            $"Upload {createdFiles.Count} file(s) to s3://{bucket}/{prefix}...?\n\nProfile: {profile}\nRegion: {region}",
            "Confirm Upload", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

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
                    file.S3Status = $"Error";
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
            txtFdbSummary.Text += $" | S3: {uploaded} uploaded, {errors} errors";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Upload error:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    // ===== TAB 5: New Version Scaffold =====

    private List<ScaffoldItem> _scaffoldItems = [];

    private void BrowseTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the UPGRADE template folder (containing FSH and/or SCAH subfolders)",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            txtTemplateFolder.Text = dialog.SelectedPath;
    }

    private void ScaffoldPreview_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(txtRootFolder.Text))
        {
            System.Windows.MessageBox.Show("Please select a root folder first.", "No Folder",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var newVersion = txtNewVersion.Text.Trim();
        if (string.IsNullOrEmpty(newVersion))
        {
            System.Windows.MessageBox.Show("Please enter a new version name.", "No Version",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboBaseVersion.SelectedItem == null)
        {
            System.Windows.MessageBox.Show("Please select a base version.", "No Base Version",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var rootFolder = txtRootFolder.Text;
        var baseVersion = cboBaseVersion.SelectedItem.ToString()!;
        var templateFolder = txtTemplateFolder.Text.Trim();

        // Check if version already exists
        var createNewDir = System.IO.Path.Combine(rootFolder, "CREATE", newVersion);
        var upgradeNewDir = System.IO.Path.Combine(rootFolder, "UPGRADE", newVersion);

        _scaffoldItems = [];

        // CREATE: copy from base version
        var createBaseDir = System.IO.Path.Combine(rootFolder, "CREATE", baseVersion);
        if (System.IO.Directory.Exists(createBaseDir))
        {
            foreach (var file in System.IO.Directory.GetFiles(createBaseDir, "*", System.IO.SearchOption.AllDirectories))
            {
                var relativePath = file[(createBaseDir.Length + 1)..];
                _scaffoldItems.Add(new ScaffoldItem
                {
                    Action = "Copy",
                    Source = $"CREATE/{baseVersion}/{relativePath}",
                    Destination = $"CREATE/{newVersion}/{relativePath}",
                    Status = System.IO.Directory.Exists(createNewDir) ? "Exists" : "Pending"
                });
            }
        }

        // UPGRADE: copy from template folder
        if (!string.IsNullOrEmpty(templateFolder) && System.IO.Directory.Exists(templateFolder))
        {
            foreach (var file in System.IO.Directory.GetFiles(templateFolder, "*", System.IO.SearchOption.AllDirectories))
            {
                var relativePath = file[(templateFolder.Length + 1)..];
                _scaffoldItems.Add(new ScaffoldItem
                {
                    Action = "Template",
                    Source = $"Template/{relativePath}",
                    Destination = $"UPGRADE/{newVersion}/{relativePath}",
                    Status = System.IO.Directory.Exists(upgradeNewDir) ? "Exists" : "Pending"
                });
            }
        }

        dgScaffoldItems.ItemsSource = _scaffoldItems;

        var createCount = _scaffoldItems.Count(i => i.Action == "Copy");
        var templateCount = _scaffoldItems.Count(i => i.Action == "Template");

        txtScaffoldSummary.Text = $"New version: {newVersion} | Base: {baseVersion} | " +
                                  $"CREATE files: {createCount} | UPGRADE template files: {templateCount} | " +
                                  $"Total: {_scaffoldItems.Count}";
    }

    private void ScaffoldCreate_Click(object sender, RoutedEventArgs e)
    {
        if (_scaffoldItems.Count == 0)
        {
            System.Windows.MessageBox.Show("Click Preview first to see what will be created.", "No Preview",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var newVersion = txtNewVersion.Text.Trim();
        var rootFolder = txtRootFolder.Text;
        var baseVersion = cboBaseVersion.SelectedItem?.ToString() ?? "";
        var templateFolder = txtTemplateFolder.Text.Trim();

        var createNewDir = System.IO.Path.Combine(rootFolder, "CREATE", newVersion);
        var upgradeNewDir = System.IO.Path.Combine(rootFolder, "UPGRADE", newVersion);

        if (System.IO.Directory.Exists(createNewDir) || System.IO.Directory.Exists(upgradeNewDir))
        {
            var overwrite = System.Windows.MessageBox.Show(
                $"Version {newVersion} already exists. Overwrite?",
                "Version Exists", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (overwrite != MessageBoxResult.Yes) return;
        }

        var confirm = System.Windows.MessageBox.Show(
            $"Create version {newVersion}?\n\n" +
            $"CREATE: copy from {baseVersion}\n" +
            $"UPGRADE: copy from template\n\n" +
            $"Total files: {_scaffoldItems.Count}",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            int created = 0;

            // Copy CREATE from base — replace old version references with new
            var createBaseDir = System.IO.Path.Combine(rootFolder, "CREATE", baseVersion);
            if (System.IO.Directory.Exists(createBaseDir))
            {
                var createReplacements = new Dictionary<string, string>
                {
                    { baseVersion, newVersion }
                };
                CopyDirectory(createBaseDir, createNewDir, createReplacements);
                created += _scaffoldItems.Count(i => i.Action == "Copy");
            }

            // Copy UPGRADE from template — replace {new_version} placeholder
            if (!string.IsNullOrEmpty(templateFolder) && System.IO.Directory.Exists(templateFolder))
            {
                var templateReplacements = new Dictionary<string, string>
                {
                    { "{new_version}", newVersion }
                };
                CopyDirectory(templateFolder, upgradeNewDir, templateReplacements);
                created += _scaffoldItems.Count(i => i.Action == "Template");
            }

            // Update statuses
            foreach (var item in _scaffoldItems)
                item.Status = "Created";

            dgScaffoldItems.ItemsSource = null;
            dgScaffoldItems.ItemsSource = _scaffoldItems;

            System.Windows.MessageBox.Show($"Version {newVersion} created with {created} files.",
                "Done", MessageBoxButton.OK, MessageBoxImage.Information);

            // Refresh version dropdowns
            if (_analyzer != null)
            {
                var versions = _analyzer.GetVersions();
                cboVersion.ItemsSource = versions;
                cboCompareVersionA.ItemsSource = versions;
                cboCompareVersionB.ItemsSource = versions;
                cboFixVersion.ItemsSource = versions;
                cboFdbVersion.ItemsSource = versions;
                cboBaseVersion.ItemsSource = versions;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error creating version:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir, Dictionary<string, string>? replacements = null)
    {
        System.IO.Directory.CreateDirectory(destDir);

        foreach (var file in System.IO.Directory.GetFiles(sourceDir))
        {
            var destFile = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(file));
            var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();

            // Only do text replacement on text files
            if (replacements != null && replacements.Count > 0 &&
                (ext is ".sql" or ".xml" or ".txt" or ".ddl"))
            {
                var content = System.IO.File.ReadAllText(file);
                foreach (var (oldVal, newVal) in replacements)
                    content = content.Replace(oldVal, newVal);
                System.IO.File.WriteAllText(destFile, content);
            }
            else
            {
                System.IO.File.Copy(file, destFile, true);
            }
        }

        foreach (var dir in System.IO.Directory.GetDirectories(sourceDir))
        {
            var destSubDir = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir, replacements);
        }
    }
}
