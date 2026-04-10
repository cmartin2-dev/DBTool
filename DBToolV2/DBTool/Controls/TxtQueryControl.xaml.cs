using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using Entities;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;
using DBTool.Commons;
using DBTool.Connect;
using Task = System.Threading.Tasks.Task;
using System.Windows.Forms;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;
using Binding = System.Windows.Data.Binding;
using MessageBox = System.Windows.MessageBox;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for TxtQueryControl.xaml
    /// </summary>
    public partial class TxtQueryControl : UserControl
    {
        private double _fontSize = 14;   // starting font size
        private const double MinFontSize = 8;
        private const double MaxFontSize = 72;

        private bool isRunning = false;


        private ICollectionView FilteredQueries { get; set; }

        public static readonly RoutedEvent ExecuteEvent =
        EventManager.RegisterRoutedEvent(
            "Execute",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(TxtQueryControl));

        public event RoutedEventHandler Execute
        {
            add { AddHandler(ExecuteEvent, value); }
            remove { RemoveHandler(ExecuteEvent, value); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Raise the event
            RaiseEvent(new RoutedEventArgs(ExecuteEvent));
        }

        public TxtQueryControl()
        {
            InitializeComponent();
            AddTextboxQueryContextMenu();

            txtQuery.MouseLeftButtonDown += txtQuery_MouseLeftButtonDown;

            var resourceName = "DBTool.CUSTOMTSQL.xshd";

            using (var s = typeof(MainWindow).Assembly.GetManifestResourceStream(resourceName))
            using (var reader = new XmlTextReader(s))
            {
                txtQuery.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }

            // Add SQL column highlighter
            var columnColorizer = new DBTool.Commons.SqlColumnColorizer();
            txtQuery.TextArea.TextView.LineTransformers.Add(columnColorizer);

            // Setup autocomplete
            txtQuery.TextArea.TextEntered += TextArea_TextEntered;
        }

        private CompletionWindow _completionWindow;

        private void TextArea_TextEntered(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Close completion on space, comma, semicolon, etc.
            if (e.Text == " " || e.Text == "," || e.Text == ";" || e.Text == "(" || e.Text == ")")
            {
                _completionWindow?.Close();
                return;
            }

            if (e.Text == ".")
            {
                _completionWindow?.Close();
                _completionWindow = null;

                int offset = txtQuery.CaretOffset - 2;
                if (offset >= 0)
                {
                    string text = txtQuery.Text;
                    int start = offset;
                    while (start > 0 && char.IsLetterOrDigit(text[start - 1]))
                        start--;
                    string prefix = text.Substring(start, offset - start + 1);

                    var tables = SqlTableParser.ExtractTables(text);

                    if (tables.ContainsKey(prefix))
                    {
                        // Known alias — show columns for that alias's table
                        var completions = SqlCompletionProvider.GetAliasCompletions(prefix, text);
                        if (completions.Count > 0)
                            ShowCompletionWindow(completions, txtQuery.CaretOffset);
                    }
                    else
                    {
                        // Not a known alias — likely a schema prefix, show tables for that schema
                        var schemaTables = SchemaStore.GetTablesForSchema(prefix);
                        var tableCompletions = new System.Collections.Generic.List<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData>();

                        if (schemaTables != null)
                        {
                            foreach (var table in schemaTables)
                                tableCompletions.Add(new SqlCompletionData(table, "Table"));
                        }
                        else
                        {
                            // Fallback: show all tables
                            foreach (var table in SchemaStore.TableColumns.Keys)
                                tableCompletions.Add(new SqlCompletionData(table, "Table"));
                        }

                        if (tableCompletions.Count > 0)
                            ShowCompletionWindow(tableCompletions, txtQuery.CaretOffset);
                    }
                }
            }
            else if (char.IsLetter(e.Text[0]))
            {
                int offset = txtQuery.CaretOffset;
                string text = txtQuery.Text;
                int start = offset - 1;
                while (start > 0 && char.IsLetterOrDigit(text[start - 1]))
                    start--;
                string currentWord = text.Substring(start, offset - start);

                // Check if we're typing after a dot (schema.table context)
                bool afterDot = start > 0 && text[start - 1] == '.';

                if (afterDot)
                {
                    // Get the prefix before the dot
                    int prefixEnd = start - 2;
                    int prefixStart = prefixEnd;
                    while (prefixStart > 0 && char.IsLetterOrDigit(text[prefixStart - 1]))
                        prefixStart--;
                    string prefix = text.Substring(prefixStart, prefixEnd - prefixStart + 1);

                    var tables = SqlTableParser.ExtractTables(text);
                    if (tables.ContainsKey(prefix))
                    {
                        // Known alias — show columns
                        var completions = SqlCompletionProvider.GetAliasCompletions(prefix, text);
                        var filtered = completions.Where(c => c.Text.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase)).ToList();
                        if (filtered.Count > 0)
                            ShowCompletionWindow(filtered, start);
                    }
                    else
                    {
                        // Schema prefix — show only tables for that schema
                        var schemaTables = SchemaStore.GetTablesForSchema(prefix);
                        var tableCompletions = new System.Collections.Generic.List<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData>();

                        if (schemaTables != null)
                        {
                            foreach (var table in schemaTables)
                            {
                                if (table.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                                    tableCompletions.Add(new SqlCompletionData(table, "Table"));
                            }
                        }
                        else
                        {
                            // Fallback: show all tables
                            foreach (var table in SchemaStore.TableColumns.Keys)
                            {
                                if (table.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                                    tableCompletions.Add(new SqlCompletionData(table, "Table"));
                            }
                        }

                        if (tableCompletions.Count > 0)
                            ShowCompletionWindow(tableCompletions, start);
                    }
                }
                else if (currentWord.Length >= 2)
                {
                    var completions = SqlCompletionProvider.GetCompletions(currentWord, text, offset);
                    if (completions.Count > 0)
                        ShowCompletionWindow(completions, start);
                }
            }
        }

        private void ShowCompletionWindow(System.Collections.Generic.List<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData> completions, int startOffset)
        {
            if (completions == null || completions.Count == 0) return;
            if (_completionWindow != null) return;

            _completionWindow = new CompletionWindow(txtQuery.TextArea);
            _completionWindow.StartOffset = startOffset;
            var data = _completionWindow.CompletionList.CompletionData;
            foreach (var item in completions)
                data.Add(item);
            _completionWindow.Show();
            _completionWindow.Closed += (s, args) => _completionWindow = null;
        }

        private void AddTextboxQueryContextMenu()
        {
            var cmInsertQuery = new MenuItem { Header = "Insert Query" };
            var cmSaveQuery = new MenuItem { Header = "Save Query" };
            var cmLogQuery = new MenuItem { Header = "Log Query" };
            var cmHistory = new MenuItem { Header = "Query History" };
            cmSaveQuery.Click += CmSaveQuery_Click;
            cmLogQuery.Click += CmLogQuery_Click;
            cmHistory.Click += CmHistory_Click;


            var cvs = new CollectionViewSource { Source = StaticFunctions.AppConnection.settingsObject.Queries };
            FilteredQueries = cvs.View;

            Binding menuItems = new Binding
            {
                Source = FilteredQueries
            };

            FilteredQueries.Filter = item =>
            {
                var query = item as Entities.Query;
                return query != null && query.IsUser;
            };

            cmInsertQuery.ItemContainerStyle = new Style(typeof(MenuItem))
            {
                Setters = {
                    new Setter(MenuItem.HeaderProperty, new Binding("Name")) ,


                }

            };

            cmInsertQuery.SetBinding(MenuItem.ItemsSourceProperty, menuItems);

            cmInsertQuery.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(SubmenuItem_Click));

            txtQuery.ContextMenu.Items.Add(cmInsertQuery);
            txtQuery.ContextMenu.Items.Add(cmSaveQuery);
            txtQuery.ContextMenu.Items.Add(cmHistory);


            if (StaticFunctions.CurrentUser.ToLower() == "cmartin2")
                txtQuery.ContextMenu.Items.Add(cmLogQuery);
            
        }

        private void CmHistory_Click(object sender, RoutedEventArgs e)
        {
            var rt = this.DataContext as Entities.RegionTenant;
            var history = QueryHistory.GetForTenant(rt?.tenantId);

            if (history.Count == 0)
            {
                ThemedDialog.Show("No query history found.", "Query History");
                return;
            }

            var window = new System.Windows.Window
            {
                Title = "Query History",
                Width = 700,
                Height = 450,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStyle = System.Windows.WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ResizeMode = System.Windows.ResizeMode.NoResize
            };

            var outerBorder = new System.Windows.Controls.Border
            {
                CornerRadius = new System.Windows.CornerRadius(12),
                Margin = new System.Windows.Thickness(10),
                Background = (System.Windows.Media.Brush)FindResource("CardBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
                BorderThickness = new System.Windows.Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 20, ShadowDepth = 4, Opacity = 0.15,
                    Color = System.Windows.Media.Colors.Black
                }
            };

            var mainGrid = new System.Windows.Controls.Grid { Margin = new System.Windows.Thickness(16) };
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            // Title bar
            var titleBar = new System.Windows.Controls.Grid();
            var titleText = new System.Windows.Controls.TextBlock
            {
                Text = "Query History",
                FontSize = 15, FontWeight = System.Windows.FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new System.Windows.Thickness(0, 0, 0, 12)
            };
            var closeBtn = new System.Windows.Controls.Button
            {
                Content = "✕", Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new System.Windows.Thickness(0),
                Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new System.Windows.Thickness(0, 0, 0, 12)
            };
            closeBtn.Click += (s, args) => window.Close();
            titleBar.Children.Add(titleText);
            titleBar.Children.Add(closeBtn);
            System.Windows.Controls.Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            var listBox = new System.Windows.Controls.ListBox
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush"),
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
                BorderThickness = new System.Windows.Thickness(1)
            };

            foreach (var entry in history)
            {
                var display = $"[{entry.DateExecuted}] {entry.TenantId}\n{(entry.Query.Length > 100 ? entry.Query.Substring(0, 100) + "..." : entry.Query)}";
                listBox.Items.Add(new System.Windows.Controls.ListBoxItem { Content = display, Tag = entry.Query });
            }

            System.Windows.Controls.Grid.SetRow(listBox, 1);
            mainGrid.Children.Add(listBox);

            var btnInsert = new System.Windows.Controls.Button
            {
                Content = "Insert Selected",
                Style = (System.Windows.Style)FindResource("RoundButton"),
                Margin = new System.Windows.Thickness(0, 12, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            btnInsert.Click += (s, args) =>
            {
                var selected = listBox.SelectedItem as System.Windows.Controls.ListBoxItem;
                if (selected != null)
                {
                    txtQuery.Text = selected.Tag.ToString();
                    window.Close();
                }
            };

            System.Windows.Controls.Grid.SetRow(btnInsert, 2);
            mainGrid.Children.Add(btnInsert);

            outerBorder.Child = mainGrid;
            window.Content = outerBorder;
            window.MouseLeftButtonDown += (s, args) => { try { window.DragMove(); } catch { } };
            window.ShowDialog();
        }

        private void CmLogQuery_Click(object sender, RoutedEventArgs e)
        {
            RegionTenant selectedRegionTenant = this.DataContext as RegionTenant;
            if (selectedRegionTenant == null) return;

            if (!string.IsNullOrWhiteSpace(txtQuery.Text))
            {
                
                var queryLog = new LogQueryControl(selectedRegionTenant.tenantId, txtQuery.Text);
              //  Entities.Query newQuery = new Entities.Query();

             //   newQuery.QueryString = txtQuery.Text;
             //   newQuery.IsUser = true;

            //    saveQueryDialog.userQuery = newQuery;

                if (queryLog.ShowDialog() == true)
                {

                   // MessageBox.Show("Item saved into list.");
                }
            }
        }

        private void txtQuery_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                RaiseEvent(new RoutedEventArgs(ExecuteEvent));
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                FormatSql();
                e.Handled = true;
            }
        }

        private void btnFormat_Click(object sender, RoutedEventArgs e)
        {
            FormatSql();
        }

        private void FormatSql()
        {
            string formatted = SqlFormatter.Format(txtQuery.Text);
            txtQuery.Text = formatted;
        }

        private void MyRichTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0 && _fontSize < MaxFontSize)
                    _fontSize += 1;
                else if (e.Delta < 0 && _fontSize > MinFontSize)
                    _fontSize -= 1;

                // Apply to entire document
                txtQuery.FontSize = _fontSize;

                e.Handled = true; // prevent scrolling
            }
        }


        private void CmSaveQuery_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtQuery.Text))
            {
                var saveQueryDialog = new SaveQueyControl();
                Entities.Query newQuery = new Entities.Query();

                newQuery.QueryString = txtQuery.Text;
                newQuery.IsUser = true;

                saveQueryDialog.userQuery = newQuery;

                if (saveQueryDialog.ShowDialog() == true)
                {

                    ThemedDialog.Show("Item saved into list.");
                }
            }
        }

        private void SubmenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menuItem)
            {
                var clickedItem = menuItem.DataContext;

                // If you know it has "Name" property
                var queryString = (clickedItem as Entities.Query).QueryString;

                txtQuery.Text = queryString;
                RaiseEvent(new RoutedEventArgs(ExecuteEvent));
            }
        }

        //private async Task MainExecute(string query, QueryListViewControl queryListViewControl)
        //{
        //    QueryListViewControl currentList = queryListViewControl;



        //    var lstViewResult = currentList.lstViewResult;
        //    var lblTotalCount = currentList.lblTotalCount;
        //    var lblProgress = currentList.lblProgress;
        //    var elapsedSeconds = currentList.elapsedSeconds;

        //    RequestQuery requestQuery = new RequestQuery();
        //    requestQuery.SetDetails(this.DataContext as RegionTenant);
        //    requestQuery.sourceToken = sourceToken;
        //    requestQuery.token = token;

        //    requestQuery.Query = query;

        //    currentList._timer.IsEnabled = true;
        //    currentList._timer.Start();
        //    currentList._stopwatch.Restart();
        //    currentList._stopwatch.Start();

        //    task = requestQuery.GetRequestQuery();

        //    currentList.ShowLoading();

        //    await task;

        //    currentList._timer.IsEnabled = false;
        //    currentList._timer.Stop();
        //    currentList._stopwatch.Stop();



        //    if (task != null)
        //    {
        //        RequestResponse _response = task.Result;
        //        if (_response.isSuccess)
        //        {
        //            CustObj result = _response.CustObj as CustObj;

        //            int itemCount = result.Objects.Count;
        //            if (itemCount > 0)
        //            {
        //                lstViewResult.GenerateListView(result);
        //                currentList.ShowListView();
        //                lblTotalCount.Content = $"Total Count : {itemCount}";
        //            }
        //            else
        //            {
        //                int count = result.Objects.Count > result.RowsAffected ? result.Objects.Count : result.RowsAffected;
        //                lblTotalCount.Content = $"Total Count : {count}";
        //                currentList.ShowDone();
        //                currentList.txtRowsAffected.Text = $"{count} rows affected.";

        //            }

        //            lblProgress.Content = $"Status: Done - {currentList.elapsedSeconds}";
        //        }
        //        else
        //        {
        //            if (_response.ErrorMessage.ToLower().Contains("cancel"))
        //            {
        //                lblProgress.Content = $"Status: Cancelled - {currentList.elapsedSeconds}";
        //                currentList.txtErrorName.Text = "Cancelled";

        //            }
        //            else
        //            {
        //                lblProgress.Content = $"Status: Error - {currentList.elapsedSeconds}";
        //                currentList.txtErrorName.Text = "Error";
        //                currentList.txtErrorDetail.Text = _response.ErrorMessage;

        //            }
        //            currentList.ShowError();
        //        }
        //    }
        //    else
        //    {
        //        lblProgress.Content = $"Status: Error - {currentList.elapsedSeconds}";
        //        currentList.ShowError();
        //        currentList.txtErrorName.Text = "Error";
        //    }

        //}

        private void txtQuery_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ExecuteEvent));
        }

        public string[] CleanQuery()
        {
          return  Regex.Split(txtQuery.Text, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }
    }
}
