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
    /// Interaction logic for EnvironmentSettingsControl.xaml
    /// </summary>
    public partial class EnvironmentSettingsControl : UserControl
    {
        public EnvironmentSettingsControl()
        {
            InitializeComponent();
            SetDataContext();
            rdoRegion.IsChecked = true;
        }

        public void SetDataContext()
        {
            regionSettingsControl.SetDataContext();
            tenantsSettingsControl.SetDataContext();
            environmentServerSettingsControl.SetDataContext();
        }

        private void rdoMain_Checked(object sender, RoutedEventArgs e)
        {
            regionSettingsControl.Visibility = rdoRegion.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            tenantsSettingsControl.Visibility = rdoTenant.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            environmentServerSettingsControl.Visibility = rdoServer.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
