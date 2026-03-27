using DBTool.Connect;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DBTool.Controls
{
    public partial class ExecutionLogControl : UserControl
    {
        public ExecutionLogControl()
        {
            InitializeComponent();
            this.IsVisibleChanged += (s, e) =>
            {
                if ((bool)e.NewValue) LoadLog();
            };
        }

        public void LoadLog()
        {
            var log = new ExecutionLog();
            var entries = log.GetAll().OrderByDescending(e => e.DateExecuted).ToList();
            dgLog.ItemsSource = entries;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLog();
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (ThemedDialog.Confirm("Clear all execution logs?", "Confirm"))
            {
                var log = new ExecutionLog();
                // Save empty list
                var emptyLog = new ExecutionLog();
                string fullFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExecutionLog", "ExecutionLog.json");
                System.IO.File.WriteAllText(fullFile, "[]");
                LoadLog();
            }
        }

        private void dgLog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = dgLog.SelectedItem as ExecutionLogEntry;
            if (selected != null)
            {
                txtQueryPreview.Text = selected.Query ?? selected.Comment ?? "(no query)";
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(dgLog.ItemsSource);
            if (view == null) return;

            string searchText = txtSearch.Text?.Trim() ?? "";

            view.Filter = item =>
            {
                if (string.IsNullOrEmpty(searchText)) return true;

                if (item is ExecutionLogEntry entry)
                {
                    return (entry.TenantId?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (entry.SchemaName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (entry.ChangesetId?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (entry.Comment?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (entry.Status?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (entry.ExecutedBy?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true) ||
                           (entry.DateExecuted?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true);
                }
                return false;
            };

            view.Refresh();
        }
    }
}
