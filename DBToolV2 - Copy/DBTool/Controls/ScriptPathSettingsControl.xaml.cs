using Entities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for ScriptPathControl.xaml
    /// </summary>
    public partial class ScriptPathSettingsControl : UserControl
    {
        SettingsObject SettingsObject;
        public ScriptPathSettingsControl()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            if(openFolderDialog.ShowDialog() == true)
            {
                string createPath = $"{openFolderDialog.FolderName}\\SQLSERVER\\CREATE";
                string upgradePath = $"{openFolderDialog.FolderName}\\SQLSERVER\\UPGRADE";
                //CHECK IF VALID PATH
                if (Directory.Exists(createPath) && Directory.Exists(upgradePath))
                {
                    txtUtilityPath.Text = openFolderDialog.FolderName;
                    txtCreateFolderPath.Text = createPath ;
                    txtUpgradeFolderPath.Text = upgradePath ;
                }
                else
                {
                    MessageBox.Show("Invalid forlder path","Error",MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
