using DBTool;
using DBTool.Connect;
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
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public SettingsControl()
        {
            InitializeComponent();
            rdoDatabaseSettings.IsChecked = true;
            //  environmentSettingsControl.Visibility = Visibility.Collapsed;
        }

        private void rdoSettings_Checked(object sender, RoutedEventArgs e)
        {
            databaseSettingsControl.Visibility = rdoDatabaseSettings.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            environmentSettingsControl.Visibility = rdoEnvironmentSettings.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            querySettingsControl.Visibility=rdoQuerySettings.IsChecked == true? Visibility.Visible : Visibility.Collapsed;
            aboutSettingsControl.Visibility=rdoAboutSettings.IsChecked == true? Visibility.Visible : Visibility.Collapsed;
            languageSettingsControl.Visibility=rdoLanguageSettings.IsChecked==true? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StaticFunctions.AppConnection.SaveSettings();
            }
            catch (Exception ex)
            {
            }

        }

        public void SetDataContext()
        {
            databaseSettingsControl.SetDataContext();
            environmentSettingsControl.SetDataContext();
            querySettingsControl.SetDataContext();
            languageSettingsControl.SetDataContext();
        }
    }
}
