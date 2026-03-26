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
using System.Windows.Shapes;

namespace DBTool.Controls
{
    /// <summary>
    /// Interaction logic for SaveQueyControl.xaml
    /// </summary>
    public partial class SaveQueyControl : Window
    {
        public string ItemName { get; private set; }
        public string ItemValue { get; private set; }

        public Query userQuery { get; set; }

        public SaveQueyControl()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Get values
            ItemName = txtName.Text.Trim();

            if (string.IsNullOrEmpty(ItemName))
            {
                MessageBox.Show("Name is required.");
                return;
            }

            userQuery.Name = ItemName;
            StaticFunctions.AppConnection.settingsObject.Queries.Add(userQuery);

            StaticFunctions.AppConnection.SaveSettings2();

            this.DialogResult = true; // closes the dialog
        }
    }
}
