using DBTool.Connect;
using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Threading;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for CustomControl.xaml
    /// </summary>
    public partial class CustomControl : UserControl
    {
        string selectedDatabase = string.Empty;
        bool isRunning = false;

        ICollectionView EnvironmentServersCollectionView;

        Task task = null;
        public Label lblProgress;

        CancellationTokenSource sourceToken = null;
        CancellationToken token;

        public DispatcherTimer _timer;
        public Stopwatch _stopwatch;
        public double elapsedSeconds;
        public CustomControl()
        {
            InitializeComponent();


            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(20);
            _timer.Tick += _timer_Tick;

            lblProgress = lstResult.lblProgress;

            lstResult.HideListView();
            SetEnvironmentServers();
            lstResult.AddContextMenu(menuItem:null);
        }

        private void SetEnvironmentServers()
        {
            //cmbBaseTenant.Items.Clear();


            cmbServer.DisplayMemberPath = "ServerName";
            cmbServer.SelectedValuePath = "Id";

            Binding binding = new Binding
            {
                Source = StaticFunctions.AppConnection.settingsObject.EnvironmentServers
            };

            cmbServer.SetBinding(ComboBox.ItemsSourceProperty, binding);
        }

        private void _timer_Tick(object? sender, EventArgs e)
        {
            elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            lblProgress.Content = $"Status: Running - {elapsedSeconds}";
        }

        private void rdoMSSQL_Checked(object sender, RoutedEventArgs e)
        {
            selectedDatabase = "MSSQL";
        }

        private void rdoPostgre_Checked(object sender, RoutedEventArgs e)
        {
            selectedDatabase = "PostgreSQL";
        }
        private void txtQueryControl_Execute(object sender, RoutedEventArgs e)
        {
            RegionTenant regionTenant = this.DataContext as RegionTenant;

            if (regionTenant == null)
            {
                ThemedDialog.Show("Select Tenant", "Error");
                return;
            }

            if (regionTenant.Region == null)
            {
                ThemedDialog.Show("Select Tenant", "Error");
                return;
            }

            if(cmbServer.SelectedItem == null)
            {
                ThemedDialog.Show("Select Server", "Error");
                return;
            }

            if (cmbDatabase.SelectedItem == null)
            {
                ThemedDialog.Show("Select Database", "Error");
                return;
            }


            if (string.IsNullOrEmpty(txtQueryControl.txtQuery.Text))
            {
                //  MessageBox.Show("Enter Query", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string connStr = string.Empty;

            EnvironmentServer server = cmbServer.SelectedItem as EnvironmentServer;
            EnvironmentDatabase database = cmbDatabase.SelectedItem as EnvironmentDatabase;

            if (selectedDatabase == "MSSQL")
            {
                connStr = $"Server={server.ServerName};Initial Catalog={database.DatabaseName};User ID={database.Username};Password={database.Password};MultiSubnetFailover=True;ConnectRetryCount=3;TrustServerCertificate=True;";


            }

            try
            {
                if (!isRunning)
                {
                    isRunning = true;
                    txtQueryControl.rdoExecute.Content = "Executing";
                    task = GetCustomQuery(connStr, txtQueryControl.txtQuery.Text);
                }
                else
                {
                    if (task != null)
                    {
                        sourceToken.Cancel();
                        isRunning = false;
                        lblProgress.Content = $"Status : Cancelled - Execution Time : {_stopwatch.Elapsed.TotalSeconds}";

                        txtQueryControl.rdoExecute.Content = "Execute";
                    }
                }
            }
            catch (Exception ex) { }


        }

        private async Task GetCustomQuery(string connectionString, string query)
        {
            RegionTenant regionTenant = this.DataContext as RegionTenant;

            sourceToken = new CancellationTokenSource();
            token = sourceToken.Token;

            RequestQuery requestQuery = new RequestQuery();
            requestQuery.Query = query;
            requestQuery.sourceToken = sourceToken;
            requestQuery.token = token;
            requestQuery.SetDetails(regionTenant);

            _timer.Start();
            _stopwatch.Restart();
            _stopwatch.Start();

            lstResult.ShowLoading();
            var result = await requestQuery.GetRequestQueryCustomCS(connectionString);

            _timer.Stop();
            _stopwatch.Stop();

            if (result != null)
            {
                if (result.isSuccess)
                {
                    lstResult.loadingControl.lblTotalCount = new Label();
                    lstResult.ShowListView();
                    lstResult.LoadData(result.CustObj);
                }
                else
                {
                    lstResult.ShowError();
                    lblProgress.Content = $"Status : Error - Execution Time : {_stopwatch.Elapsed.TotalSeconds}";
                }
            }
            else
            {
                lstResult.ShowError();
                lblProgress.Content = $"Status : Error - Execution Time : {_stopwatch.Elapsed.TotalSeconds}";
            }

            txtQueryControl.rdoExecute.Content = "Execute";

            isRunning = false;
        }

        private void cmbServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbServer.SelectedItem != null)
            {
                EnvironmentServer selectedItem = cmbServer.SelectedItem as EnvironmentServer;
                if (selectedItem != null)
                {
                    cmbDatabase.DisplayMemberPath = "DatabaseName";
                    cmbDatabase.SelectedValuePath = "DatabaseName";

                    Binding binding = new Binding
                    {
                        Source = selectedItem.Databases,
                    };

                    cmbDatabase.SetBinding(ComboBox.ItemsSourceProperty, binding);
                }
            }
            else
            {
                cmbDatabase.ItemsSource = null;
                lstResult.ClearItems();
            }
        }
    }
}
