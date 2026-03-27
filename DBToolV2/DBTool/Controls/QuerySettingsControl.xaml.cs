using DBTool.Commons;
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

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for QuerySettingsControl.xaml
    /// </summary>
    public partial class QuerySettingsControl : UserControl
    {
        Entities.Query newQuery = new Entities.Query();
        Entities.Query OriginalQuery = new Entities.Query();
        public QuerySettingsControl()
        {
            InitializeComponent();
            GenerateColumns();
            EnableAddQuery(true);
            SetDataContext();
        }

        public void SetDataContext()
        {
            SetNewStackPanelContext();
            this.DataContext = StaticFunctions.AppConnection.settingsObject;
        }
        private void GenerateColumns()
        {

            string[] ColumnNames = { "Name" };
            string[] bindingNames = { "Name" };
            int[] widths = {  250 };
            lstViewQuery.GenerateListView(ColumnNames, bindingNames, widths);
        }

        private void EnableAddQuery(bool enable)
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
                newQuery = new Entities.Query();
            }
            else
            {
                newQuery = (stackPanelDetails.DataContext as Entities.Query).Clone() as Entities.Query;
            }

            stackPanelDetails.DataContext = newQuery;

        }

        private bool Validate()
        {
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                hasError = true;
            }

            return hasError;
        }

        private void btnAdd_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!Validate())
                {
                    EnableAddQuery(true);
                    StaticFunctions.AppConnection.settingsObject.Queries.Add(newQuery);

                    SetNewStackPanelContext();
                }
            }
        }

        private void btnCopy_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddQuery(true);
                SetNewStackPanelContext(true);
            }
        }

        private void btnDelete_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (ThemedDialog.Confirm("Do you want to delete this query?", "Delete Query"))
                {
                    {
                        EnableAddQuery(true);
                        StaticFunctions.AppConnection.settingsObject.Queries.Remove(stackPanelDetails.DataContext as Entities.Query);
                        SetNewStackPanelContext();
                    }

                }
            }
        }

        private void btnCancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddQuery(true);
                stackPanelDetails.DataContext = OriginalQuery;
                SetNewStackPanelContext();
                lstViewQuery.SelectedValue = null;
            }
        }

        private void lstViewQuery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstViewQuery.SelectedItems.Count > 0)
            {
                var item = lstViewQuery.SelectedItem;
                OriginalQuery = (item as Entities.Query).Clone() as Entities.Query;

                EnableAddQuery(false);
                stackPanelDetails.DataContext = item;
            }
        }
    }
}
