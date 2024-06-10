using MSL.controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;


namespace MSL.pages.frpProviders
{
    /// <summary>
    /// Custom.xaml 的交互逻辑
    /// </summary>
    public partial class Custom : Page
    {
        private bool init=false; //初始化确认，防止未初始化就更改ui导致boom
        public Custom()
        {
            InitializeComponent();
        }
        private void Page_Initialized(object sender, EventArgs e)
        {
            init = true;//初始化完成了
        }

        /*
        private void WebBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        */

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EasyMode.IsChecked==true)
            {
                //可视化模式
                //检测必备数据写了没，防止小傻瓜
                if (ServerIP.Text == "" || ServerPort.Text=="" || ClientRemotePort.Text==""||ClientIP.Text==""||ClientPort.Text=="")
                {
                    Shows.ShowMsg(Window.GetWindow(this), "存在未填写的数据！\n请检查！", "错误！");
                }
                else
                {
                    //既然都写了，那就继续
                    string FrpcConfig;
                    FrpcConfig = $"serverAddr = {ServerIP.Text}\r\n" +
                        $"serverPort = {ServerPort.Text}\r\n" +
                        $"transport.tcpMux = {ServerTcpMux.IsChecked.ToString().ToLower()}\r\n" +
                        $"transport.protocol = {ServerProtocol.Text}\r\nuser = {ServerUser.Text}\r\n" +
                        $"auth.token = {ServerToken.Text}\r\ndnsServer = {ServerDNS.Text}\r\n" +
                        $"transport.tls.enable = {ServerTls.IsChecked.ToString().ToLower()}\r\n" +
                        //$"[[proxies]]\r\nprivilege_mode = {ServerPrivilege.IsChecked.ToString().ToLower()}\r\n" +
                        $"[[proxies]]\r\n" +
                        $"type = {ClientProtocol.Text}\r\nlocalIp = {ClientIP.Text}\r\n" +
                        $"localPort = {ClientPort.Text}\r\nremotePort = {ClientRemotePort.Text}\r\n" +
                        $"transport.useEncryption = {ClientEnc.IsChecked.ToString().ToLower()}\r\n" +
                        $"transport.useCompression = {ClientComp.IsChecked.ToString().ToLower()}\r\n \r\n";
                    File.WriteAllText(@"MSL\frpc.toml", FrpcConfig);
                    SetFrpcPath();
                }
            }
            else
            {
                //直接丢配置文件模式
                File.WriteAllText(@"MSL\frpc.toml", ConfigBox.Text);
                SetFrpcPath();
            }
        }

        private async void SetFrpcPath()
        {
            if (CustomFrpcClient.IsChecked == true)//自定义的话要导入进MSL文件夹
            {
                //选择文件对还款
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "exe应用程序 (*.exe)|*.exe";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "请选择您的Frpc客户端";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //文件路径
                    string filePath = openFileDialog.FileName;
                    File.Copy(filePath, @"MSL/frpc_custom.exe");
                    Config.Write("frpcServer", "-2");//自定义模式-自定义frpc客户端
                }
                else
                {
                    Config.Write("frpcServer", "-1");//自定义模式-Gh上的客户端
                }

            }
            //最后结束
            await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "隧道配置成功，请您点击“启动内网映射”以启动映射！", "信息");
            Window.GetWindow(this).Close();
        }

        private void EasyMode_Checked(object sender, RoutedEventArgs e)
        {
            if (init)
            {
                LowLevelGrid.Visibility = Visibility.Visible;
                CustomGrid.Visibility = Visibility.Collapsed;
            }
           
        }

        private void CustomMode_Checked(object sender, RoutedEventArgs e)
        {
            if (init)
            {
                LowLevelGrid.Visibility = Visibility.Collapsed;
                CustomGrid.Visibility = Visibility.Visible;
            }
            
        }
    }
}
// 最后是creeper镇楼⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡀⠤⠚⠉⢉⡑⠤⢀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⠤⠊⠉⠠⣄⡀⠈⠓⢤⣀⠤⠈⠑⠢⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠤⠒⣉⠀⠀⠐⠀⠀⠀⠉⠐⠦⣉⠈⠁⠒⠤⣀⠀⠉⠐⠢⢄⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⣤⠒⠙⠢⣄⣀⠀⠉⠢⣄⣠⠔⠂⠀⣀⠤⠚⠁⠀⠀⠀⠀⠑⠢⣄⡠⠔⠊⠒⣤⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⣀⠤⠲⢍⡀⠀⠈⠐⠧⣀⣀⠤⠒⢍⡀⠀⠈⠑⠫⣀⠀⠀⠀⠀⠀⣀⠥⠒⠯⣁⣀⠤⠒⠁⠀⢀⡨⠔⠤⣀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠙⠲⢤⣀⠈⠑⢢⡤⠔⠊⠑⣢⠤⠔⠊⠑⣢⠤⣔⠊⠁⣀⠤⠐⠉⠀⢀⡠⠐⠊⠑⠢⠤⣔⠊⠑⢢⡤⠔⠚⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⣄⡀⢈⠈⢱⠺⢥⣀⠀⠀⠫⢄⢀⠤⠒⠉⠀⠀⡠⠝⠫⢄⡀⡀⠔⠊⠁⠀⠀⠀⠀⠀⢀⣠⠽⡏⠁⠀⢀⡰⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠈⡏⠒⢼⠀⠀⠈⠉⠒⣖⡉⠀⢀⡠⠔⠊⠁⠀⠀⢖⡉⠀⢉⡲⠀⠀⠀⢀⣠⠔⠊⠁⠀⣠⡇⠀⠋⠁⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠐⠢⢇⡀⠀⠀⠀⠰⢄⡀⠇⠈⢳⠋⢄⣠⠔⠒⠂⢄⡀⠈⠛⠉⢀⡀⠄⢺⠉⠀⢀⡀⡔⠊⠁⡇⢀⡀⠀⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠀⢸⣿⣶⢤⣀⠀⠀⠈⡗⠤⣼⠀⠀⠈⠑⢲⣎⠁⠀⣀⠴⠊⢹⠀⢀⣸⠀⠀⠁⠀⢀⣀⡤⠗⠉⡇⠀⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠀⢸⣿⣿⡀⠈⡏⠐⠲⡇⠀⣀⠀⠀⠠⣀⢸⠀⠉⠉⠀⠀⣀⢼⠊⠁⢸⢠⡠⠔⠀⢸⠁⢀⣦⠔⠃⠀⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠒⠤⣼⣿⣿⣿⣶⣧⣀⠀⡇⠀⢳⣤⣀⠀⠀⡏⠑⢢⡄⠀⢽⠀⢸⣀⠠⠾⠁⠀⠀⣀⠴⠒⠉⠀⠀⣀⡠⠴⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⢀⠀⠈⠉⢻⠿⣿⡇⠀⠉⠃⠀⢸⣿⣿⡏⠲⡇⠀⠀⡇⣀⡼⠚⠉⠃⠀⣠⠤⢲⠉⠀⠀⢀⡠⠔⠊⠁⠀⣀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠉⢲⢄⣸⠀⠈⢹⣦⣄⡀⠀⢸⣿⣿⣧⣀⡇⠀⠀⡇⠁⡇⠀⠀⠖⠊⠁⠀⢸⠀⠀⠘⠁⠀⠀⠀⡔⠈⠁⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠲⢄⣠⠀⢸⠓⠦⣼⣿⣿⣿⣷⡿⢿⣿⣿⣿⡗⠢⢄⡧⠚⡇⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⣀⠀⠀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠀⠀⠈⠉⢺⣏⠀⢸⣿⣿⣿⣿⡷⠀⠈⡟⠻⡇⠀⠀⡇⢀⡇⠀⠈⠃⠀⢀⠀⢸⠀⠀⠀⠀⢀⡔⠋⠁⠀⢀⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠉⠒⠴⢄⣸⣿⣿⣿⣿⣿⣿⣿⣿⣶⣤⣧⣀⡇⠀⠀⡗⠉⡇⠀⠀⡦⠒⡇⠀⢸⣀⠤⠆⠀⢸⡇⠀⢤⠖⠫⡇⠀⠀⠀⠀
//⠀⠀⠀⠀⠸⢄⡀⠀⠀⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠅⠈⡟⠢⢄⡇⠔⡇⠀⠀⡇⣠⢧⠖⢹⠀⢀⡆⠀⢸⡇⠀⢸⣀⡼⠇⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠈⠉⠒⢼⣿⣿⡏⠈⠙⠿⣿⣿⣿⣿⡄⠠⣇⠀⠀⡇⠀⡇⠀⠈⠃⠀⢸⣠⢼⠋⠁⠁⠀⢰⡧⠖⠋⠁⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡊⠙⡷⢄⡀⠀⠀⢹⣿⣿⣇⠀⠀⠉⠢⡗⠋⡇⠀⢀⠄⠀⠉⠀⢸⣀⡠⠖⠊⠑⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡇⠀⡏⠓⢮⣗⡢⣼⣿⣿⡏⠙⡆⠤⣀⣇⠀⣓⠊⠈⠀⢀⡴⠀⢸⠁⠀⣀⡠⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠉⠑⢧⣀⠀⡇⠀⠙⠦⣍⣦⣄⡇⠀⠀⡇⠀⣏⠤⠚⠆⠁⡇⢀⣸⠖⠈⠁⠀⢠⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⢠⡄⠉⡖⠤⡀⠀⠀⡏⠑⠏⠓⢤⠗⠊⢱⠦⣤⡤⠔⠻⠉⢸⠀⣀⠴⠖⢻⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠈⠓⠦⡇⠀⠁⠀⠀⠣⢀⠀⠀⠈⡗⠤⣸⡀⢸⠀⠀⠀⠀⠈⠉⠀⡀⠀⣼⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⢠⣀⠀⡇⠀⠰⢤⣀⠀⠀⠁⠀⠀⠇⠀⡇⠈⠙⠄⠀⠀⠀⢀⠔⠚⠏⠀⢨⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⢄⡀⡀⠀⠉⡗⢤⣀⠀⢨⠑⠢⠄⠀⠀⠀⠈⢳⢄⣀⡄⠀⠀⠀⢸⠀⢀⣤⣴⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠑⠦⣄⡇⠀⠈⠉⠒⡄⠀⠀⠀⠀⠀⠠⣸⠀⢸⠀⠀⢀⣠⢾⣿⣿⣿⣿⡿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠒⠠⣤⣀⠀⠉⠒⠄⠀⠀⡇⠀⠀⠀⠀⠀⠀⢈⠙⠺⣴⠚⢹⡀⣸⠿⠛⠋⠁⠘⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣀⠀⡃⠈⠑⠢⢄⣄⠀⠀⠑⠢⣄⡀⠀⠀⠀⡼⢄⣀⡇⠀⠞⠋⠁⠀⢀⡄⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡆⠉⠳⠤⣀⠀⠀⠈⠁⠀⠀⠀⢸⠉⢹⠢⢄⡇⠀⠈⡇⠀⠀⠀⢠⠊⠁⠀⠀⣀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠓⠤⡄⠀⢈⡗⠢⡄⠀⠀⡀⠀⠘⠦⢼⡀⠈⡇⠀⠀⡇⠀⠀⠀⢸⣀⠠⠖⠋⠁⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⡀⠀⠀⠀⠀⢇⡀⠀⠀⠀⠑⠠⠀⠀⠀⡏⠒⢧⣀⠀⡇⢀⡶⠚⢹⠁⠀⣀⣠⣴⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠉⠑⠲⠤⣀⠀⠉⡇⠀⠀⠀⠀⠀⠀⢠⠣⣀⢸⠈⠉⠉⠀⡇⢀⣸⣶⣾⡏⠀⣸⡧⢄⡀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠦⢄⡀⠀⠀⠙⠢⢇⡀⠀⠀⠀⢰⢄⣸⠀⠀⢹⠀⠀⣠⣴⡟⠁⢸⣿⠿⠟⠋⠀⣗⠊⠉⠑⠢⢄⡀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠆⠀⠁⠀⠀⣇⡀⠀⠈⠳⠲⢄⣸⠀⢸⠐⠢⣼⣀⠀⣿⣿⡧⠖⠉⠀⠀⢀⣠⢴⣧⠝⠒⠤⣀⠀⠈⠑⠢⢄
//⠀⠀⠀⠀⠀⢀⡠⠤⢖⡉⢀⡠⠴⢄⡀⡇⠈⢳⡤⣀⠀⠀⢸⠑⠺⢄⠀⢸⠈⠙⡏⠁⡇⠀⠀⠀⠀⠈⠀⢰⢋⡲⠤⢔⡁⠀⣀⠤⠒⠋
//⠀⠀⣀⡤⠚⠻⢄⣀⡠⠜⠣⢄⡀⠀⠈⠑⠤⣼⡇⢸⠉⠲⠼⠀⠀⠀⠉⠊⠀⠀⣧⠔⠃⠀⠀⠀⢀⡴⠚⠉⠃⠀⢀⣤⢾⡏⠁⠀⠀⠠
//⢾⡉⠀⢈⠵⠊⠉⠀⢈⡱⢮⡉⠀⣉⠖⠀⠀⠀⢉⡽⠂⠀⠀⠀⠀⠀⠀⣀⡀⠀⠀⠀⣤⠀⢀⠈⠀⢀⣠⠼⡖⠋⢹⠀⣸⡧⠒⣽⠀⠀
//⠀⠈⠙⠣⢤⣀⠀⠀⠁⣀⠤⠚⠉⠒⣠⣤⠒⠉⠓⣠⣤⠒⠉⠒⠤⢇⠀⡇⠉⠲⠖⠉⠀⢀⣸⡤⠒⠋⠀⠀⣇⡠⣼⠋⠀⠀⢀⣿⠀⠀
//⠀⠀⠀⠀⠀⠈⠑⡶⢍⡀⢀⠤⠒⠮⣁⣈⠵⠒⠮⣁⢈⠵⠢⢄⡀⠀⠉⠓⠤⣀⣄⣠⡞⠉⢹⠀⠤⣄⠀⠀⡟⠀⣟⡠⢔⡞⠁⠁⠀⠀
//⠀⠀⠀⠀⢰⠀⠀⡇⠀⢸⠷⠢⢤⡔⠊⠀⠀⠀⠔⠊⠁⣢⠤⣐⠉⠀⢀⡠⠔⢺⠁⠈⠱⠢⣼⠀⠀⠈⠑⠲⠗⠉⢁⠀⢀⡧⠀⢹⠀⠀
//⠀⠀⡟⠢⢼⢀⠀⡇⠀⢸⠀⠀⠀⠈⠑⡦⢀⣀⠠⠔⠊⠀⢀⣠⢽⠞⠉⠀⢀⣸⣶⣤⣄⠀⢸⠀⠀⠀⠀⠀⠀⠀⢸⠒⠉⠀⠀⠘⠀⠈
//⠀⠠⣇⠀⠀⠁⠙⠧⣀⢸⠀⠀⠀⠀⠀⡇⠀⠋⠑⠦⡴⠚⢹⠁⢸⡠⠔⣿⠉⠀⣿⣿⣿⣿⣶⣤⣀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀
//⣀⠀⠈⠉⢲⠀⠀⠀⠀⢹⠓⠢⣄⡀⠀⡁⠀⠀⠀⠀⡇⣠⡜⠊⢹⠀⠀⣿⠀⠀⣿⣿⣿⣿⣿⣿⣿⣿⣶⣤⠀⠀⢼⠀⢀⣀⠀⠀⠀⠀
//⣿⣷⣶⣄⣸⠀⠀⠀⠀⠘⣄⠀⠀⠈⠑⡆⢀⡆⠀⠀⡏⠀⣇⣠⠼⡖⠋⢻⠀⠀⠻⢿⣿⣿⣿⣿⣿⣿⣿⣿⡇⣀⣸⠒⠉⡇⠀⢀⡀⠐
//⣿⣿⣿⣿⣿⣿⣦⣄⡀⠀⠀⠈⠳⢤⣀⡇⠀⠈⠒⠤⠷⠊⢁⠀⢀⣇⠔⢻⠀⠀⠀⠀⠈⠙⠻⢿⣿⣿⣿⣿⣿⣿⡿⢀⣠⠗⠊⠁⠀⠀
//⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣦⣄⡀⠀⠈⡇⠀⠀⠀⠀⠀⠀⢸⠔⠋⠀⠀⢸⠤⠚⠀⠀⠀⠀⠀⠀⠈⠙⠻⢿⡿⠟⠛⠉⠀⠀⠀⠀⠀⠀
//⠈⠙⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣦⣅⡀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⡏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠉⠛⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣦⣄⡀⠀⢼⠀⠀⠀⠀⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠉⠛⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⢀⣼⠔⠊⡇⠀⣀⣀⠼⠂⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠛⠿⣿⣿⣿⣿⣿⣶⣿⡇⠀⣠⠧⠚⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠛⠿⣿⣿⠿⠓⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
