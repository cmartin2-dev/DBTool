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
    /// Interaction logic for AuthenticationControl.xaml
    /// </summary>
    public partial class AuthenticationSettingsControl : UserControl
    {
        public AuthenticationSettingsControl()
        {
            InitializeComponent();
        }

        public void SetDataContext()
        {

            this.DataContext = StaticFunctions.AppConnection.settingsObject;
        }

        private void ScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt) && e.Key == Key.A)
            {
                if (lblFullAccess.Visibility == Visibility.Hidden)
                {
                    lblFullAccess.Visibility = Visibility.Visible;
                    chkIsFullAccess.Visibility = Visibility.Visible;
                }
                else
                {
                    lblFullAccess.Visibility = Visibility.Hidden;
                    chkIsFullAccess.Visibility = Visibility.Hidden;
                }
            }
        }
    }

}
