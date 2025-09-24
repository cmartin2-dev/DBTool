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

            //if (rdoDashboard.IsChecked == true)
            //{
            //    if (dashboardControl != null)
            //        dashboardControl = null;

            //    dashboardControl = new DashboardControl();
            //    mainDockPanel.Children.Add(dashboardControl);
            //}
            if (rdoSettings.IsChecked == true)
            {
                settingsControl.rdoDatabaseSettings.IsChecked = true;
                //if (settingsControl != null)
                //    settingsControl = null;

                //settingsControl = new SettingsControl();
                //mainDockPanel.Children.Add(settingsControl);
            }

            //if (rdoEnvironment.IsChecked == true)
            //{
            //    if (environmentControl != null)
            //        environmentControl = null;

            //    environmentControl = new EnvironmentControl();
            //    mainDockPanel.Children.Add(environmentControl);
            //}

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

    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return 0.0;

            if (!double.TryParse(values[0]?.ToString(), out double value)) return 0.0;
            if (!double.TryParse(values[1]?.ToString(), out double maximum)) return 0.0;
            if (!double.TryParse(values[2]?.ToString(), out double actualWidth)) return 0.0;

            if (maximum <= 0) return 0.0;
            double ratio = Math.Max(0.0, Math.Min(1.0, value / maximum));
            // You may want to subtract padding/margins if your template has them
            return ratio * actualWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SecondsToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double seconds)
            {
                return TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
            }
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}