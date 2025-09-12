using DBTool.Controls;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace DBTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DashboardControl dashboardControl = null;
        SettingsControl settingsControl = null;
        public MainWindow()
        {
            InitializeComponent();
            rdoDashboard.IsChecked = true;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mainRdo_Checked(object sender, RoutedEventArgs e)
        {
            mainDockPanel.Children.Clear();
            SetCurrentControl();
            GetSettings();

        }
        private void SetCurrentControl()
        {
            if (rdoDashboard.IsChecked == true)
            {
                if (dashboardControl != null)
                    dashboardControl = null;

                dashboardControl = new DashboardControl();
                mainDockPanel.Children.Add(dashboardControl);
            }
            if(rdoSettings.IsChecked == true)
            {
                if (settingsControl != null)
                    settingsControl = null;

                settingsControl = new SettingsControl();
                mainDockPanel.Children.Add(settingsControl);
            }

        }

        private void GetSettings()
        {

        }
    }
}