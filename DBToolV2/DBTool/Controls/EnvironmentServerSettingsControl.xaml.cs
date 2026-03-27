using DBTool.Commons;
using Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for EnvironmentServerSettingsControl.xaml
    /// </summary>
    public partial class EnvironmentServerSettingsControl : UserControl
    {
        ICollectionView EnvironmentServerView;
        EnvironmentServer newEnvironmentServer = null;

        Entities.EnvironmentServer OriginalEnvironmentServer = new Entities.EnvironmentServer();

        public EnvironmentServerSettingsControl()
        {
            InitializeComponent();
            GenerateColumns();
            SetDataContext();
            SetNewStackPanelContext();
        }

        public void SetDataContext()
        {

            SetNewStackPanelContext();
            this.DataContext = StaticFunctions.AppConnection.settingsObject;

            EnvironmentServerView = CollectionViewSource.GetDefaultView(StaticFunctions.AppConnection.settingsObject.EnvironmentServers);
            lstViewServer.ItemsSource = null;
            lstViewServer.ItemsSource = EnvironmentServerView;
        }

        private void GenerateColumns()
        {

            string[] ColumnNames = { "ID", "Server Name" };
            string[] bindingNames = { "Id", "ServerName" };
            int[] widths = { 25, 150 };



            lstViewServer.GenerateListView(ColumnNames, bindingNames, widths);



        }

        private void SetNewStackPanelContext(bool isCopy = false)
        {
            if (!isCopy)
            {
                newEnvironmentServer = new Entities.EnvironmentServer();
            }
            else
            {
                newEnvironmentServer = (stackPanelDetails.DataContext as Entities.EnvironmentServer).Clone() as Entities.EnvironmentServer;
            }
            var items = StaticFunctions.AppConnection.settingsObject.EnvironmentServers;
            if (items != null && items.Count > 0)
            {
                newEnvironmentServer.Id = items.Max(x => x.Id) + 1;
            }
            else
                newEnvironmentServer.Id = 1;


            stackPanelDetails.DataContext = newEnvironmentServer;

        }

        private bool Validate()
        {
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(txtServer.Text))
            {
                hasError = true;
            }
           


            return hasError;
        }

        private void EnableAddEnvironmentServer(bool enable)
        {
            btnAdd.IsEnabled = enable;
            btnDelete.IsEnabled = !enable;
            // btnUpdate.IsEnabled = !enable;
            btnCancel.IsEnabled = true;
            btnCopy.IsEnabled = !enable;
        }



        private void btnAdd_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!Validate())
                {
                    EnableAddEnvironmentServer(true);
                    StaticFunctions.AppConnection.settingsObject.EnvironmentServers.Add(newEnvironmentServer);

                    SetNewStackPanelContext();
                }
            }
        }

        private void btnCopy_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddEnvironmentServer(true);
                SetNewStackPanelContext(true);
            }
        }

        private void btnDelete_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (ThemedDialog.Confirm("Do you want to delete this server?", "Delete Server"))
                {
                    {
                        EnableAddEnvironmentServer(true);
                        StaticFunctions.AppConnection.settingsObject.EnvironmentServers.Remove(stackPanelDetails.DataContext as Entities.EnvironmentServer);
                        SetNewStackPanelContext();
                    }

                }
            }
        }

        private void btnCancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddEnvironmentServer(true);
                stackPanelDetails.DataContext = OriginalEnvironmentServer;
                SetNewStackPanelContext();
                lstViewServer.SelectedValue = null;
            }
        }

 

         private void lstViewServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstViewServer.SelectedItems.Count > 0)
            {
                var item = lstViewServer.SelectedItem;
                OriginalEnvironmentServer = (item as Entities.EnvironmentServer).Clone() as Entities.EnvironmentServer;

                EnableAddEnvironmentServer(false);
                stackPanelDetails.DataContext = item;
            }
        }
    }
}
