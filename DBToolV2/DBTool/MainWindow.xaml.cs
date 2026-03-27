using DBTool    ;
using DBTool.Commons;
using DBTool.Connect;
using DBTool.Controls;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
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
            ThemeManager.ApplyTheme(ThemeManager.LoadSavedTheme());
            GetSettings();
            InitializeComponent();

            rdoDashboard.IsChecked = true;
            UpdateThemeButtonText();

            this.DataContext = StaticFunctions.AppConnection.settingsObject;

            StaticFunctions.AppConnection.settingsObject.CheckAccess = false;

            if (StaticFunctions.CurrentUser.ToLower() == "cmartin2")
            {
                rdoQueryLog.Visibility = Visibility.Visible;
                StaticFunctions.AppConnection.settingsObject.CheckAccess = true;
            }
            else
                rdoQueryLog.Visibility = Visibility.Collapsed;

            StateChanged += MainWindow_StateChanged;

            // Start maximized (not always on top)
            _isFullscreen = true;
            Loaded += (s, ev) =>
            {
                Topmost = false;
                WindowState = WindowState.Normal;
                Left = 0;
                Top = 0;
                Width = System.Windows.SystemParameters.PrimaryScreenWidth;
                Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                ResizeMode = ResizeMode.CanResize;
            };
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                Topmost = false;
                WindowState = WindowState.Normal;
                Left = 0;
                Top = 0;
                Width = System.Windows.SystemParameters.PrimaryScreenWidth;
                Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                ResizeMode = ResizeMode.CanResize;
            }
        }

        private void RestoreFromFullscreen()
        {
            Topmost = false;
            ResizeMode = ResizeMode.CanResize;
            Width = 1200;
            Height = 800;
            Left = (System.Windows.SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (System.Windows.SystemParameters.PrimaryScreenHeight - Height) / 2;
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
            translationControl.Visibility = rdoTranslation.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            databaseControl.Visibility = rdoDatabase.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            apiControl.Visibility = rdoApi.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            logDBExecuteControl.Visibility = rdoQueryLog.IsChecked == true ? Visibility.Visible : Visibility.Collapsed ;

            settingsControl.SetDataContext();
            environmentControl.SetDataContext();
            translationControl.SetDataContext();

            if (rdoSettings.IsChecked == true)
            {


                settingsControl.rdoDatabaseSettings.IsChecked = true;
            }

        }

        public void SwitchToEnvironment()
        {
            dashboardControl.Visibility = Visibility.Collapsed;
            settingsControl.Visibility = Visibility.Collapsed;
            environmentControl.Visibility = Visibility.Visible;
            translationControl.Visibility = Visibility.Collapsed;
            databaseControl.Visibility = Visibility.Collapsed;
            apiControl.Visibility = Visibility.Collapsed;
            logDBExecuteControl.Visibility = Visibility.Collapsed;
            rdoEnvironment.IsChecked = true;
            environmentControl.SetDataContext();
        }

        private void GetSettings()
        {
            StaticFunctions.AppConnection = new AppConnect();

        }

        private void checkSettingsObject_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!StaticFunctions.AppConnection.CheckDirty())
            {

                if (Controls.ThemedDialog.Confirm("Settings has not been saved. Do you want to discard any changes?", "Unsaved Settings"))
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

        private bool _isFullscreen = false;

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleFullscreen();
            }
            else
            {
                if (!_isFullscreen)
                    DragMove();
            }
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            Topmost = false;
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
            UpdateThemeButtonText();
        }

        private void UpdateThemeButtonText()
        {
            themeIcon.Text = ThemeManager.IsDarkMode ? "\uD83C\uDF19" : "\u2600";
            themeLabel.Text = ThemeManager.IsDarkMode ? "Dark Mode" : "Light Mode";
        }

        private void ToggleFullscreen()
        {
            if (_isFullscreen)
            {
                _isFullscreen = false;
                RestoreFromFullscreen();
            }
            else
            {
                _isFullscreen = true;
                Topmost = false;
                WindowState = WindowState.Normal;
                Left = 0;
                Top = 0;
                Width = System.Windows.SystemParameters.PrimaryScreenWidth;
                Height = System.Windows.SystemParameters.PrimaryScreenHeight;
                ResizeMode = ResizeMode.CanResize;
            }
        }
    }

   
}