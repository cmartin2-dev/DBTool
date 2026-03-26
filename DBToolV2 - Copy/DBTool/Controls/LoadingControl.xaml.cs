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
    /// Interaction logic for LoadingControl.xaml
    /// </summary>
    public partial class LoadingControl : UserControl
    {

        public ListView lstViewResult { get; set; }
        public Button btnExportToExcel { get; set; }

        public Button btnExportToJson { get; set; }

        public Label lblTotalCount { get; set; }

        public LoadingControl()
        {
            InitializeComponent();
        }

        //public void ShowListView()
        //{
        //    //  lstViewResult.Visibility = Visibility.Visible;
        //    if (btnExportToExcel != null)
        //        btnExportToExcel.Visibility = Visibility.Visible;
        //    if (btnExportToJson != null)
        //        btnExportToJson.Visibility = Visibility.Visible;
        //    if (lblTotalCount != null)
        //        lblTotalCount.Visibility = Visibility.Visible;
        //    Errorborder.Visibility = Visibility.Collapsed;
        //    Doneborder.Visibility = Visibility.Collapsed;

        //    loadingborder.Visibility = Visibility.Collapsed;
        //}

        //public void ShowLoading()
        //{

        //    loadingborder.Visibility = Visibility.Visible;

        //    //  lstViewResult.Visibility = Visibility.Collapsed;
        //    if (btnExportToExcel != null)
        //        btnExportToExcel.Visibility = Visibility.Collapsed;
        //    if (btnExportToJson != null)
        //        btnExportToJson.Visibility = Visibility.Collapsed;
        //    if (lblTotalCount != null)
        //        lblTotalCount.Visibility = Visibility.Collapsed;
        //    Errorborder.Visibility = Visibility.Collapsed;
        //    Doneborder.Visibility = Visibility.Collapsed;
        //}

        //public void ShowError()
        //{
        //    loadingborder.Visibility = Visibility.Collapsed;

        //    //    lstViewResult.Visibility = Visibility.Collapsed;
        //    if (btnExportToExcel != null)
        //        btnExportToExcel.Visibility = Visibility.Collapsed;
        //    if (btnExportToJson != null)
        //        btnExportToJson.Visibility = Visibility.Collapsed;
        //    if (lblTotalCount != null)
        //        lblTotalCount.Visibility = Visibility.Collapsed;
        //    Errorborder.Visibility = Visibility.Visible;
        //    Doneborder.Visibility = Visibility.Collapsed;
        //}

        //public void ShowDone()
        //{
        //    loadingborder.Visibility = Visibility.Collapsed;

        //    //      lstViewResult.Visibility = Visibility.Collapsed;
        //    if (btnExportToExcel != null)
        //        btnExportToExcel.Visibility = Visibility.Collapsed;
        //    if (btnExportToJson != null)
        //        btnExportToJson.Visibility = Visibility.Collapsed;
        //    if (lblTotalCount != null)
        //        lblTotalCount.Visibility = Visibility.Collapsed;
        //    Errorborder.Visibility = Visibility.Collapsed;
        //    Doneborder.Visibility = Visibility.Visible;
        //}

        //public void HideListView(bool hide = true)
        //{
        //   // lstViewResult.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
        //    if (btnExportToExcel != null)
        //        btnExportToExcel.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
        //    if (btnExportToJson != null)
        //        btnExportToJson.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
        //    if (lblTotalCount != null)
        //        lblTotalCount.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
        //}
    }
}
