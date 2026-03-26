using DBTool;
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
    /// Interaction logic for DatabaseSettingsControl.xaml
    /// </summary>
    public partial class DatabaseSettingsControl : UserControl
    {
        public DatabaseSettingsControl()
        {
            InitializeComponent();
            rdoAuthentication.IsChecked = true;
            SetDataContext();
        }

        private void rdoDBSettings_Checked(object sender, RoutedEventArgs e)
        {
            authenticationSettingsControl.Visibility = rdoAuthentication.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            ScriptPathSettingsControl.Visibility = rdoScript.IsChecked == true ? Visibility.Visible: Visibility.Collapsed;
        }

        public void SetDataContext()
        {
            authenticationSettingsControl.DataContext = StaticFunctions.AppConnection.settingsObject;
            ScriptPathSettingsControl.DataContext = StaticFunctions.AppConnection.settingsObject;
        }

     
    }
}
