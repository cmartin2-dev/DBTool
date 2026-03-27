using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
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
        }

        private void AddTextboxQueryContextMenu()
        {
            var cmInsertQuery = new MenuItem { Header = "Insert Query" };
            var cmSaveQuery = new MenuItem { Header = "Save Query" };
            var cmLogQuery = new MenuItem { Header = "Log Query" };
            cmSaveQuery.Click += CmSaveQuery_Click;
            cmLogQuery.Click += CmLogQuery_Click;


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


            if (StaticFunctions.CurrentUser.ToLower() == "cmartin2")
                txtQuery.ContextMenu.Items.Add(cmLogQuery);
            
        }

        private void CmLogQuery_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtQuery.Text))
            {
                var queryLog = new LogQueryControl(txtQuery.Text);
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
                // Execute the command
               RaiseEvent(new RoutedEventArgs(ExecuteEvent));
                e.Handled = true; // prevent further bubbling
            }
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
