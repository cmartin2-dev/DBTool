using DBTool    ;
using DBTool.Connect;
using DBTool.Controls;
using System.Globalization;
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
        public MainWindow()
        {
            GetSettings();
            InitializeComponent();

            rdoDashboard.IsChecked = true;


            this.DataContext = StaticFunctions.AppConnection.settingsObject;

            StaticFunctions.AppConnection.settingsObject.CheckAccess = StaticFunctions.AppConnection.settingsObject.IsFullAccess;

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mainRdo_Checked(object sender, RoutedEventArgs e)
        {
          //  mainDockPanel.Children.Clear();
            SetCurrentControl();

        }
        private void SetCurrentControl()
        {
            dashboardControl.Visibility = rdoDashboard.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            settingsControl.Visibility = rdoSettings.IsChecked == true ? Visibility.Visible: Visibility.Collapsed;
            environmentControl.Visibility = rdoEnvironment.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            settingsControl.SetDataContext();
            environmentControl.SetDataContext();
            
            if (rdoSettings.IsChecked == true)
            {


                settingsControl.rdoDatabaseSettings.IsChecked = true;
            }

        }

        private void GetSettings()
        {
            StaticFunctions.AppConnection = new AppConnect();

        }

        private void checkSettingsObject_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!StaticFunctions.AppConnection.CheckDirty())
            {

                if (MessageBox.Show("Settings has not been saved. Do you want to discard any changes?", "Unsaved Settings", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    StaticFunctions.AppConnection.RevertSettings();
                    this.DataContext = StaticFunctions.AppConnection.settingsObject;

                    e.Handled = false;
                    (sender as RadioButton).IsChecked = true;
                }
                else
                    e.Handled = true;
            }
            else
                e.Handled = false;
        }
    }

   
}