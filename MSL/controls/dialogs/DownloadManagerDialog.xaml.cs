using HandyControl.Controls;
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

namespace MSL.controls.dialogs
{
    /// <summary>
    /// DownloadManagerDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadManagerDialog : UserControl
    {
        #region 单例模式
        private static readonly Lazy<DownloadManagerDialog> _instance = new Lazy<DownloadManagerDialog>(() => new DownloadManagerDialog());
        public static DownloadManagerDialog Instance => _instance.Value;
        #endregion

        public Dialog fatherDialog = null;
        public DownloadManagerDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            fatherDialog.Close();
        }
    }
}
