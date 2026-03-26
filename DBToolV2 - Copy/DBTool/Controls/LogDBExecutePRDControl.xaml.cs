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
    /// Interaction logic for LogDBExecutePRDControl.xaml
    /// </summary>
    public partial class LogDBExecutePRDControl : UserControl
    {
        List<QueryLog> lstQueryLog = null;
        public LogDBExecutePRDControl()
        {
            InitializeComponent();
        }

        private void btnGetLog_Click(object sender, RoutedEventArgs e)
        {
            
            QueryLogProcess queryLogProcess = new QueryLogProcess();
            lstQueryLog = queryLogProcess.GetLog();

            lstDBExecResult.CustomColumns.Clear();
            lstDBExecResult.CustomColumns.Add("JIRATicket", "JIRA Ticket");
            lstDBExecResult.CustomColumns.Add("TenantId", "Tenant Id");
            lstDBExecResult.CustomColumns.Add("SchemaVersion", "Schema");
            lstDBExecResult.CustomColumns.Add("ReleaseVersion", "Release Version");
            lstDBExecResult.CustomColumns.Add("DateExecuted", "Date Executed"); 

            lstDBExecResult.LoadData(lstQueryLog);

        }

        private void btnGenerateLogFile_Click(object sender, RoutedEventArgs e)
        {
            QueryLogProcess queryLogProcess = new QueryLogProcess();
            queryLogProcess.GetLog();
            queryLogProcess.SaveLog();
        }

        private void lstDBExecResult_SelectionChangedExecute(object sender, SelectionChangedEventArgs e)
        {
            if(lstDBExecResult.dataGrid1.SelectedItem != null)
            {
                var item = lstDBExecResult.dataGrid1.SelectedItem as QueryLog;
                txtScript.Text = item.Script;

            }
        }
    }
}
