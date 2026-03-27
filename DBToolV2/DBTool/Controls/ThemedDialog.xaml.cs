using System.Windows;
using System.Windows.Controls;

namespace DBTool.Controls
{
    public partial class ThemedDialog : Window
    {
        public bool Result { get; private set; }

        private ThemedDialog(string title, string message, string[] buttons)
        {
            InitializeComponent();
            txtTitle.Text = title;
            txtMessage.Text = message;

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = new Button
                {
                    Content = buttons[i],
                    Style = (Style)FindResource("RoundButton"),
                    MinWidth = 80,
                    Margin = new Thickness(i > 0 ? 8 : 0, 0, 0, 0)
                };
                int index = i;
                btn.Click += (s, e) =>
                {
                    Result = index == 0;
                    DialogResult = index == 0;
                    Close();
                };
                pnlButtons.Children.Add(btn);
            }
        }

        public static void Show(string message, string title = "DBTool")
        {
            var dlg = new ThemedDialog(title, message, new[] { "OK" });
            dlg.Owner = Application.Current.MainWindow;
            dlg.ShowDialog();
        }

        public static bool Confirm(string message, string title = "Confirm")
        {
            var dlg = new ThemedDialog(title, message, new[] { "Yes", "No" });
            dlg.Owner = Application.Current.MainWindow;
            dlg.ShowDialog();
            return dlg.Result;
        }
    }
}
