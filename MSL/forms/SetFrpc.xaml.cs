using MSL.controls;
using System.Diagnostics;

namespace MSL
{
    /// <summary>
    /// SetFrpc.xaml 的交互逻辑
    /// </summary>
    public partial class SetFrpc : HandyControl.Controls.Window
    {
        public SetFrpc()
        {
            InitializeComponent();
        }

        private async void JoinUs_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await Shows.ShowMsgDialogAsync(this, "即将跳转到Github填写接入申请···\n记得填写接入服务商信息和联系方式哦！", "即将跳转···");
            Process.Start("https://github.com/MSLTeam/MSL/issues/new?assignees=octocat&labels=%E5%8A%9F%E8%83%BD%E8%AF%B7%E6%B1%82enhancement&projects=&template=feature.yml&title=%5BFeat%5D%3A+%E7%94%B3%E8%AF%B7%E6%8E%A5%E5%85%A5%E7%AC%AC%E4%B8%89%E6%96%B9FRP:");
        }
    }
}
