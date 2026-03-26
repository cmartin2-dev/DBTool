using DBTool.Commons;
using Entities;
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
    /// Interaction logic for LanguageSettingsControl.xaml
    /// </summary>
    public partial class LanguageSettingsControl : UserControl
    {

        Entities.Language newLanguage = new Entities.Language();
        Entities.Language originalLanguage = new Entities.Language();
        public LanguageSettingsControl()
        {
            InitializeComponent();
            GenerateColumns();
            SetDataContext();
            EnableAddBtn(true);
            PopulateExcludedKeys(); 
        }

        private void PopulateExcludedKeys()
        {
           
            Binding binding = new Binding("ExcludedKeys")
            {
                Source = this.DataContext,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.TwoWay
            };

            txtExcludedKeys.SetBinding(TextBox.TextProperty, binding);
        }

        private void EnableAddBtn(bool enable)
        {
            btnAdd.IsEnabled = enable;
            btnDelete.IsEnabled = !enable;
            // btnUpdate.IsEnabled = !enable;
            btnCancel.IsEnabled = true;
            btnCopy.IsEnabled = !enable;
        }

        public void SetDataContext()
        {
            SetNewStackPanelContext();
            this.DataContext = StaticFunctions.AppConnection.settingsObject;

           var languageCollectionView = CollectionViewSource.GetDefaultView(StaticFunctions.AppConnection.settingsObject.Languages);
            lstviewLanguage.ItemsSource = null;
            lstviewLanguage.ItemsSource = languageCollectionView;


            PopulateExcludedKeys();
        }

        private void SetNewStackPanelContext(bool isCopy = false)
        {
            if (!isCopy)
            {
                newLanguage = new Entities.Language();
            }
            else
            {
                newLanguage = (stackPanelDetails.DataContext as Entities.Language).Clone() as Entities.Language;
            }

            stackPanelDetails.DataContext = newLanguage;

        }

        private void GenerateColumns()
        {

            string[] ColumnNames = { "Name","Culture" };
            string[] bindingNames = { "Name","Culture" };
            int[] widths = { 250 };
            lstviewLanguage.GenerateListView(ColumnNames, bindingNames, widths);
        }

        private bool Validate()
        {
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                hasError = true;
            }


            if (string.IsNullOrWhiteSpace(txtCulture.Text))
            {
                hasError = true;
            }

            return hasError;
        }

        private void lstviewLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstviewLanguage.SelectedItems.Count > 0)
            {
                var item = lstviewLanguage.SelectedItem;
                originalLanguage = (item as Entities.Language).Clone() as Entities.Language;

                EnableAddBtn(false);
                stackPanelDetails.DataContext = item;
            }
        }

        private void btnAdd_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!Validate())
                {
                    EnableAddBtn(true);
                    StaticFunctions.AppConnection.settingsObject.Languages.Add(newLanguage);

                    SetNewStackPanelContext();
                }
            }
        }

        private void btnCopy_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddBtn(true);
                SetNewStackPanelContext(true);
            }
        }

        private void btnDelete_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (MessageBox.Show("Do you want to delete this langauge?", "Delete Language", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    {
                        EnableAddBtn(true);
                        StaticFunctions.AppConnection.settingsObject.Languages.Remove(stackPanelDetails.DataContext as Entities.Language);
                        SetNewStackPanelContext();
                    }

                }
            }
        }

        private void btnCancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                EnableAddBtn(true);
                stackPanelDetails.DataContext = originalLanguage;
                SetNewStackPanelContext();
                lstviewLanguage.SelectedValue = null;
            }
        }
    }
}
