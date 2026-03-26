using DBTool.Connect;
using Entities;
using Newtonsoft.Json;
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
    /// Interaction logic for LogQueryControl.xaml
    /// </summary>
    public partial class LogQueryControl : Window
    {
        QueryLogProcess queryLogProcess = null;
        public LogQueryControl(string query)
        {
            InitializeComponent();
            QueryLog queryLog = new QueryLog();
            queryLog.Script = query;

            this.DataContext = queryLog;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            queryLogProcess = new QueryLogProcess();
            List<QueryLog> lstQueryLog = queryLogProcess.GetLog();

            lstQueryLog.Add(this.DataContext as QueryLog);

            queryLogProcess.SaveLog();

            this.Close();

        }
    }
}
