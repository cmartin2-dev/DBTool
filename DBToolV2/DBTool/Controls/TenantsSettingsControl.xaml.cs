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
    /// Interaction logic for TenantsSettingsControl.xaml
    /// </summary>
    public partial class TenantsSettingsControl : UserControl
    {
        private HeaderEnvironment newHeaderEnvironment = new HeaderEnvironment();
        private HeaderEnvironment OriginalHeaderEnvironment = new HeaderEnvironment();


        private ICollectionView environmentCollectionView;
        public TenantsSettingsControl()
        {
            InitializeComponent();
            GenerateColumns();
            EnableAddTenant(true);
            SetDataContext();
            AddContextMenu();
        }

        private void AddContextMenu()
        {
            lstViewTenants.Tag = "Tenants";

            var globalMenu = (ContextMenu)System.Windows.Application.Current.Resources["SharedListViewContextMenu"];
            var newMenu = Utilities.CloneContextMenu(globalMenu, lstViewTenants);

            lstViewTenants.ContextMenu = newMenu;
        }

        public void SetDataContext()
        {

            SetNewStackPanelContext();
            this.DataContext = StaticFunctions.AppConnection.settingsObject;

            environmentCollectionView = CollectionViewSource.GetDefaultView(StaticFunctions.AppConnection.settingsObject.Headers);
            lstViewTenants.ItemsSource = null;
            lstViewTenants.ItemsSource = environmentCollectionView;
        }

        private void GenerateColumns()
        {

            string[] ColumnNames = { "ID", "Tenant Name" };
            string[] bindingNames = { "Id", "TenantName" };
            int[] widths = { 25, 150 };
            lstViewTenants.GenerateListView(ColumnNames, bindingNames, widths);



        }

        private void EnableAddTenant(bool enable)
        {
            btnAdd.IsEnabled = enable;
            btnDelete.IsEnabled = !enable;
            // btnUpdate.IsEnabled = !enable;
            btnCancel.IsEnabled = true;
            btnCopy.IsEnabled = !enable;
        }

        private void SetNewStackPanelContext(bool isCopy = false)
        {
            if (!isCopy)
            {
                newHeaderEnvironment = new Entities.HeaderEnvironment();
            }
            else
            {
                newHeaderEnvironment = (stackPanelDetails.DataContext as Entities.HeaderEnvironment).Clone() as Entities.HeaderEnvironment;
            }
            var items = StaticFunctions.AppConnection.settingsObject.Headers;
            if (items != null && items.Count > 0)
            {
                newHeaderEnvironment.Id = items.Max(x => x.Id) + 1;
            }
            else
                newHeaderEnvironment.Id = 1;


            stackPanelDetails.DataContext = newHeaderEnvironment;

        }


        private void lstViewTenants_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstViewTenants.SelectedItems.Count > 0)
            {
                var item = lstViewTenants.SelectedItem;
                OriginalHeaderEnvironment = (item as Entities.HeaderEnvironment).Clone() as Entities.HeaderEnvironment;

                EnableAddTenant(false);
                stackPanelDetails.DataContext = item;
            }
        }

        private void btnAdd_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!Validate())
                {
                    EnableAddTenant(true);
                    StaticFunctions.AppConnection.settingsObject.Headers.Add(newHeaderEnvironment);
                    SetNewStackPanelContext();
                }
            }
        }

        private void btnCopy_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddTenant(true);
                SetNewStackPanelContext(true);
            }
        }

        private void btnDelete_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (MessageBox.Show("Do you want to delete this tenant?", "Delete Tenant", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    {
                        EnableAddTenant(true);
                        StaticFunctions.AppConnection.settingsObject.Headers.Remove(stackPanelDetails.DataContext as Entities.HeaderEnvironment);
                        SetNewStackPanelContext();
                    }

                }
            }
        }

        private void btnCancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddTenant(true);
                stackPanelDetails.DataContext = OriginalHeaderEnvironment;
                SetNewStackPanelContext();
                lstViewTenants.SelectedValue = null;
            }
        }

        private bool Validate()
        {
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(txtTenantName.Text))
            {
                hasError = true;
            }

            return hasError;
        }

        private void ScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt) && e.Key == Key.A)
            {
                if (lblEndpoint.Visibility == Visibility.Collapsed)
                {
                    lblEndpoint.Visibility = Visibility.Visible;
                    txtEndpoint.Visibility = Visibility.Visible;
                }
                else
                {
                    lblEndpoint.Visibility = Visibility.Collapsed;
                    txtEndpoint.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
