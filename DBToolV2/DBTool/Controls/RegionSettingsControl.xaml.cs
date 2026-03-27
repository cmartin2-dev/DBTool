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
using DBTool.Commons;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for RegionSettingsControl.xaml
    /// </summary>
    public partial class RegionSettingsControl : UserControl
    {
        Entities.Region newRegion = new Entities.Region();
        Entities.Region OriginalRegion = new Entities.Region();


        private ICollectionView environmentCollectionView;
        public RegionSettingsControl()
        {
            InitializeComponent();
            GenerateColumns();
            EnableAddRegion(true);
            SetBaseTenants();
            SetDataContext();
            SetNewStackPanelContext();
            AddContextMenu();
        }


        private void AddContextMenu()
        {
            lstViewRegion.Tag = "Regions";

            var globalMenu = (ContextMenu)System.Windows.Application.Current.Resources["SharedListViewContextMenu"];
            var newMenu = Utilities.CloneContextMenu(globalMenu, lstViewRegion);

            lstViewRegion.ContextMenu = newMenu;
        }

        public void SetDataContext()
        {
            SetNewStackPanelContext();
            this.DataContext = StaticFunctions.AppConnection.settingsObject;

            var regionCollectionView = CollectionViewSource.GetDefaultView(StaticFunctions.AppConnection.settingsObject.Regions);
            lstViewRegion.ItemsSource = null;
            lstViewRegion.ItemsSource = regionCollectionView;

        }

        private void SetBaseTenants()
        {
            //cmbBaseTenant.Items.Clear();


            cmbBaseTenant.DisplayMemberPath = "EnvironmentName";
            cmbBaseTenant.SelectedValuePath = "Id";

            Binding binding = new Binding
            {
                Source = StaticFunctions.AppConnection.settingsObject.Headers
            };

            environmentCollectionView = CollectionViewSource.GetDefaultView(StaticFunctions.AppConnection.settingsObject.Regions);
            lstViewRegion.ItemsSource = null;
            lstViewRegion.ItemsSource = environmentCollectionView;

            cmbBaseTenant.SetBinding(ComboBox.ItemsSourceProperty, binding);
        }

        private void SetNewStackPanelContext(bool isCopy = false)
        {
            if (!isCopy)
            { 
                newRegion = new Entities.Region();
            }
            else
            {
                newRegion = (stackPanelDetails.DataContext as Entities.Region).Clone() as Entities.Region;
            }
            var items = StaticFunctions.AppConnection.settingsObject.Regions;
            if (items != null && items.Count > 0)
            {
                newRegion.Id = items.Max(x => x.Id) + 1;
            }
            else
                newRegion.Id = 1;

            
                stackPanelDetails.DataContext = newRegion;
   
        }
        private void GenerateColumns()
        {

            string[] ColumnNames = { "ID", "Region Name" };
            string[] bindingNames = { "Id", "RegionName" };
            int[] widths = { 25, 150 };



            lstViewRegion.GenerateListView(ColumnNames, bindingNames, widths);



        }

        private void EnableAddRegion(bool enable)
        {
            btnAdd.IsEnabled = enable;
            btnDelete.IsEnabled = !enable;
            // btnUpdate.IsEnabled = !enable;
            btnCancel.IsEnabled = true;
            btnCopy.IsEnabled = !enable;
        }

        private void lstViewRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstViewRegion.SelectedItems.Count > 0)
            {
                var item = lstViewRegion.SelectedItem;
                OriginalRegion = (item as Entities.Region).Clone() as Entities.Region;

                EnableAddRegion(false);
                stackPanelDetails.DataContext = item;
            }
        }

        private void btnCancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddRegion(true);
                stackPanelDetails.DataContext = OriginalRegion;
                SetNewStackPanelContext();
                lstViewRegion.SelectedValue = null;
            }
        }

        private void btnAdd_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!Validate())
                {
                    EnableAddRegion(true);
                    StaticFunctions.AppConnection.settingsObject.Regions.Add(newRegion);

                    SetNewStackPanelContext();
                }
            }
        }

        private bool Validate()
        {
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(txtRegionEnvironment.Text))
            {
                hasError = true;
            }
            if (string.IsNullOrWhiteSpace(txtEndpoint.Text))
            {
                hasError = true;
            }
            if (string.IsNullOrWhiteSpace(txtClientId.Text))
            {
                hasError = true;
            }
            if (string.IsNullOrWhiteSpace(txtClientSecret.Text))
            {
                hasError = true;
            }


            return hasError;
        }

        private void btnCopy_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddRegion(true);
                SetNewStackPanelContext(true);
            }
        }

        private void btnDelete_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (ThemedDialog.Confirm("Do you want to delete this region?", "Delete Region"))
                {
                    {
                        EnableAddRegion(true);
                        StaticFunctions.AppConnection.settingsObject.Regions.Remove(stackPanelDetails.DataContext as Entities.Region);
                        SetNewStackPanelContext();
                    }

                }
            }
        }
    }
}
