using MSL.pages.frpProviders;
using MSL.pages.frpProviders.MSLFrp;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace MSL
{
    /// <summary>
    /// AddFrpc.xaml 的交互逻辑
    /// </summary>
    public partial class FrpProvider : UserControl
    {
        private readonly List<Page> Pages;
        public Action _onClose;
        public FrpProvider(Action onClose)
        {
            InitializeComponent();
            _onClose = onClose;
            Pages = new List<Page> { new MSLFrp(ReturnFun), new OpenFrp(ReturnFun), new SakuraFrp(ReturnFun), new ChmlFrp(ReturnFun), new MEFrp(ReturnFun), new Custom() };
            TabCtrl.SelectedIndex = 1;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabCtrl.SelectedIndex != -1 && TabCtrl.SelectedIndex != TabCtrl.Items.Count - 1)
            {
                if (TabCtrl.SelectedIndex == 0)
                {
                    _onClose.Invoke();
                    return;
                }
                frame.Content = Pages[TabCtrl.SelectedIndex - 1];
            }
            else
            {
                frame.Content = null;
            }
        }

        private void ReturnFun() => _onClose.Invoke();

        public void Dispose()
        {
            Pages.Clear();
            GC.Collect(); // find finalizable objects
            GC.WaitForPendingFinalizers(); // wait until finalizers executed
            GC.Collect(); // collect finalized objects
        }

        /*
        private async void JoinUs_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await MagicShow.ShowMsgDialogAsync(this, "即将跳转到Github填写接入申请···\n记得填写接入服务商信息和联系方式哦！", "即将跳转···");
            Process.Start("https://github.com/MSLTeam/MSL/issues/new?assignees=octocat&labels=%E5%8A%9F%E8%83%BD%E8%AF%B7%E6%B1%82enhancement&projects=&template=feature.yml&title=%5BFeat%5D%3A+%E7%94%B3%E8%AF%B7%E6%8E%A5%E5%85%A5%E7%AC%AC%E4%B8%89%E6%96%B9FRP:");
        }
        */
    }
}
