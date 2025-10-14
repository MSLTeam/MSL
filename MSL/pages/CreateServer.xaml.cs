using HandyControl.Controls;
using ICSharpCode.SharpZipLib.Zip;
using MSL.controls;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using Window = System.Windows.Window;

namespace MSL.pages
{
    /// <summary>
    /// CreateServer.xaml 的交互逻辑
    /// </summary>
    public partial class CreateServer : Page
    {
        public static event DeleControl GotoServerList;
        private int returnMode = 0; //1：WelcomeGrid，2：FastModeGrid，3：FastModeInstallGrid，4：CustomModeDir，5：CustomModeJava，6：CustomModeServerCore，7：CustomModeFinally，8Finally：SelectTerminal
        private string DownjavaName;
        private string servername;
        private string serverjava;
        private string serverbase;
        private string servercore;
        private string servermemory;
        private string serverargs;
        private short launchmode = 0; // 启动模式（指启动服务器所用的cmd参数），默认为0，即"-jar server.jar"；若为1，即是自定义启动命令模式

        public CreateServer()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            for (int a = 1; a != 0; a++)
            {
                if (Directory.Exists(@"MSL\Server") && !(Directory.GetDirectories(@"MSL\Server").Length > 0 || Directory.GetFiles(@"MSL\Server").Length > 0))
                {
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server";
                    return;
                }
                else if (!Directory.Exists(@"MSL\Server"))
                {
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server";
                    return;
                }
                else if (Directory.Exists(@"MSL\Server" + a.ToString()) && !(Directory.GetDirectories(@"MSL\Server" + a.ToString()).Length > 0 || Directory.GetFiles(@"MSL\Server" + a.ToString()).Length > 0))
                {
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server" + a.ToString();
                    return;
                }
                else if (!Directory.Exists(@"MSL\Server" + a.ToString()))
                {
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server" + a.ToString();
                    return;
                }
            }
        }

        private async void FastModeBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            FastModeGrid.Visibility = Visibility.Visible;
            returnMode = 1;
            await FastModeGetCore();
        }

        private void CustomModeBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Collapsed;
            tabCtrl.Visibility = Visibility.Visible;
            returnMode = 1;
        }

        private bool isImportPack = false;
        private async void ImportPack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImportPack.SelectedIndex == 1)
            {
                ImportPack.SelectedIndex = 0;
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "请务必下载文件名含有“server”且为.zip格式的服务端整合包！否则会出现软件无法读取或开服失败的问题！", "下载须知");
                DownloadMod downloadMod = new DownloadMod("MSL\\Downloads", 0, 1, false, true, true)
                {
                    Owner = Window.GetWindow(Window.GetWindow(this))
                };
                downloadMod.ShowDialog();
                string dFilename = downloadMod.FileName;
                if (dFilename == null)
                {
                    return;
                }
                if (!File.Exists($"MSL\\Downloads\\{dFilename}"))
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载失败！", "错误");
                    return;
                }
                if (Path.GetExtension($"MSL\\Downloads\\{dFilename}") != ".zip")
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "您所下载的整合包文件不符合导入格式（目前软件仅支持导入.zip文件）！请检查您所下载的文件是否为服务端专用包并重试！\n错误的格式：" + Path.GetExtension($"MSL\\Downloads\\{dFilename}"), "错误");
                    return;
                }
                string input = await MagicShow.ShowInput(Window.GetWindow(this), "服务器名称：", "MyServer");
                if (input != null)
                {
                    servername = input;
                    string serverPath = "";
                    for (int a = 1; a != 0; a++)
                    {
                        if (!Directory.Exists("MSL\\Server"))
                        {
                            serverPath = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server";
                            break;
                        }
                        if (!Directory.Exists("MSL\\Server" + a.ToString()))
                        {
                            serverPath = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server" + a.ToString();
                            break;
                        }
                    }
                    Dialog waitDialog = null;
                    try
                    {
                        waitDialog = Dialog.Show(new TextDialog("解压整合包中，请稍等……"));
                        await Task.Run(() => new FastZip().ExtractZip("MSL\\Downloads\\" + dFilename, serverPath, ""));
                        DirectoryInfo[] dirs = new DirectoryInfo(serverPath).GetDirectories();
                        if (dirs.Length == 1)
                        {
                            Functions.MoveFolder(dirs[0].FullName, serverPath);
                        }
                        File.Delete("MSL\\Downloads\\" + dFilename);
                    }
                    catch (Exception ex)
                    {
                        Window.GetWindow(this).Focus();
                        waitDialog.Close();
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "整合包解压失败！请确认您的整合包是.zip格式！\n错误代码：" + ex.Message, "错误");
                        return;
                    }
                    Window.GetWindow(this).Focus();
                    waitDialog.Close();
                    MainGrid.Visibility = Visibility.Hidden;
                    tabCtrl.Visibility = Visibility.Visible;
                    isImportPack = true;
                    serverbase = serverPath;

                    List<string> strings = await AsyncGetJavaVersion();
                    if (strings != null)
                    {
                        selectJavaComb.ItemsSource = strings.ToList();
                        selectJavaComb.SelectedIndex = 0;
                    }
                    else
                    {
                        Growl.Error("出现错误，获取Java版本列表失败！");
                    }

                    Growl.Info("整合包解压完成！请在此界面选择Java环境，Java的版本要和导入整合包的版本相对应，详情查看界面下方的表格");
                    sjava.IsSelected = true;
                    sjava.IsEnabled = true;
                    welcome.IsEnabled = false;
                    returnMode = 1;
                }
            }
            else if (ImportPack.SelectedIndex == 2)
            {
                ImportPack.SelectedIndex = 0;
                bool dialog = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "目前仅支持导入.zip格式的整合包文件，如果您要导入的是模组整合包，请确保您下载的整合包是服务器专用包（如RLCraft下载界面就有一个ServerPack的压缩包），否则可能会出现无法开服或者崩溃的问题！", "提示", true, "取消");
                if (dialog == true)
                {
                    string input = await MagicShow.ShowInput(Window.GetWindow(this), "服务器名称：", "MyServer");
                    if (input != null)
                    {
                        servername = input;
                        string serverPath = "";
                        for (int a = 1; a != 0; a++)
                        {
                            if (!Directory.Exists("MSL\\Server"))
                            {
                                serverPath = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server";
                                break;
                            }
                            if (!Directory.Exists("MSL\\Server" + a.ToString()))
                            {
                                serverPath = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server" + a.ToString();
                                break;
                            }
                        }
                        OpenFileDialog openfile = new OpenFileDialog
                        {
                            InitialDirectory = "MSL",
                            Title = "请选择整合包压缩文件",
                            Filter = "ZIP文件|*.zip|所有文件类型|*.*"
                        };
                        var res = openfile.ShowDialog();
                        if (res == true)
                        {
                            MagicDialog MagicDialog = new MagicDialog();
                            //Dialog waitDialog = null;
                            try
                            {
                                MagicDialog.ShowTextDialog(Window.GetWindow(this), "解压整合包中，请稍等……");
                                //waitDialog = Dialog.Show(new TextDialog("解压整合包中，请稍等……"));
                                await Task.Run(() => new FastZip().ExtractZip(openfile.FileName, serverPath, ""));
                                DirectoryInfo[] dirs = new DirectoryInfo(serverPath).GetDirectories();
                                if (dirs.Length == 1)
                                {
                                    Functions.MoveFolder(dirs[0].FullName, serverPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                MagicDialog.CloseTextDialog();
                                MagicShow.ShowMsgDialog(Window.GetWindow(this), "整合包解压失败！请确认您的整合包是.zip格式！\n错误代码：" + ex.Message, "错误");
                                return;
                            }
                            MagicDialog.CloseTextDialog();
                            MainGrid.Visibility = Visibility.Hidden;
                            tabCtrl.Visibility = Visibility.Visible;
                            isImportPack = true;
                            serverbase = serverPath;

                            List<string> strings = await AsyncGetJavaVersion();
                            if (strings != null)
                            {
                                selectJavaComb.ItemsSource = strings.ToList();
                                selectJavaComb.SelectedIndex = selectJavaComb.Items.Count - 1;
                            }
                            else
                            {
                                Growl.Error("出现错误，获取Java版本列表失败！");
                            }

                            Growl.Info("整合包解压完成！请在此界面选择Java环境，Java的版本要和导入整合包的版本相对应，详情查看界面下方的表格");
                            sjava.IsSelected = true;
                            sjava.IsEnabled = true;
                            welcome.IsEnabled = false;
                            returnMode = 1;
                        }
                    }
                }
            }
        }

        private async Task CheckServerPackCore()
        {
            if (isImportPack)
            {
                sserver.IsSelected = true;
                sserver.IsEnabled = true;
                sjava.IsEnabled = false;

                string forge = Functions.InstallForge("", serverbase, "", "");
                if (forge != null)
                {
                    bool ret = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "开服器在整合包中检测到了Forge服务端启动文件" + forge + "，是否选择为开服核心？", "提示", true, "取消");
                    if (ret)
                    {
                        txb3.Text = forge;
                        servercore = txb3.Text;
                        sJVM.IsSelected = true;
                        sJVM.IsEnabled = true;
                        sserver.IsEnabled = false;
                    }
                    return;
                }

                DirectoryInfo directoryInfo = new DirectoryInfo(serverbase);
                FileInfo[] fileInfo = directoryInfo.GetFiles("*.jar");
                List<string> files = new List<string>();
                foreach (var file in fileInfo)
                {
                    files.Add(file.Name);
                }
                if (files.Count > 1)
                {
                    string filestr = "";
                    int i = 0;
                    foreach (var file in files)
                    {
                        filestr += "\n" + i.ToString() + "." + file;
                        i++;
                    }
                    string selectFile = await MagicShow.ShowInput(Window.GetWindow(this), "开服器在整合包中检测到了以下jar文件，你可输选择一个作为开服核心（输入文件前对应的数字，取消为不选择以下文件）\n" + filestr);
                    if (selectFile != null)
                    {
                        txb3.Text = files[int.Parse(selectFile)];
                        if (Functions.CheckForgeInstaller(serverbase + "\\" + txb3.Text))
                        {
                            bool dialog = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您选择的服务端疑似是forge安装器，是否将其展开安装？\n如果不展开安装，服务器可能无法开启！", "提示", true, "取消");
                            if (dialog)
                            {
                                string installReturn = await InstallForge(txb3.Text);
                                if (installReturn == null)
                                {
                                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载失败！", "错误");
                                    return;
                                }
                                txb3.Text = installReturn;
                            }
                        }
                        servercore = txb3.Text;
                        sJVM.IsSelected = true;
                        sJVM.IsEnabled = true;
                        sserver.IsEnabled = false;
                    }
                }
                else if (files.Count == 1)
                {
                    bool ret = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "开服器在整合包中检测到了jar文件" + files[0] + "，是否选择此文件为开服核心？", "提示", true, "取消");
                    if (ret)
                    {
                        txb3.Text = files[0];
                        if (Functions.CheckForgeInstaller(serverbase + "\\" + txb3.Text))
                        {
                            bool dialog = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您选择的服务端疑似是forge安装器，是否将其展开安装？\n如果不展开安装，服务器可能无法开启！", "提示", true, "取消");
                            if (dialog)
                            {
                                string installReturn = await InstallForge(txb3.Text);
                                if (installReturn == null)
                                {
                                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载失败！", "错误");
                                    return;
                                }
                                txb3.Text = installReturn;
                            }
                        }
                        servercore = txb3.Text;
                        sJVM.IsSelected = true;
                        sJVM.IsEnabled = true;
                        sserver.IsEnabled = false;
                    }
                }
                else if (files.Count == 0)
                {
                    Growl.Info("开服器未在整合包中找到核心文件，请您进行下载或手动选择已有核心，核心的版本要和整合包对应的游戏版本一致");
                }
            }
        }
        private async Task<int> DownloadJava(string fileName, string downUrl)
        {
            if (!File.Exists(@"MSL\" + fileName + @"\bin\java.exe"))
            {
                DownjavaName = fileName;
                bool downDialog = await MagicShow.ShowDownloader(Window.GetWindow(this), downUrl, "MSL", "Java.zip", "下载" + fileName + "中……");
                if (downDialog)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                serverjava = AppDomain.CurrentDomain.BaseDirectory + "MSL\\" + fileName + "\\bin\\java.exe";
                return 2;
            }
        }
        private async Task<bool> UnzipJava()
        {
            try
            {
                string javaDirName = "";
                using (ZipFile zip = new ZipFile(@"MSL\Java.zip"))
                {
                    foreach (ZipEntry entry in zip)
                    {
                        if (entry.IsDirectory == true)
                        {
                            int c0 = entry.Name.Length - entry.Name.Replace("/", "").Length;
                            if (c0 == 1)
                            {
                                javaDirName = entry.Name.Replace("/", "");
                                break;
                            }
                        }
                    }
                }
                FastZip fastZip = new FastZip();
                await Task.Run(() => fastZip.ExtractZip(@"MSL\Java.zip", "MSL", ""));
                File.Delete(@"MSL\Java.zip");
                if (@"MSL\" + javaDirName != @"MSL\" + DownjavaName)
                {
                    Functions.MoveFolder(@"MSL\" + javaDirName, @"MSL\" + DownjavaName);
                }
                while (!File.Exists(@"MSL\" + DownjavaName + @"\bin\java.exe"))
                {
                    await Task.Delay(1000);
                }
                serverjava = AppDomain.CurrentDomain.BaseDirectory + "MSL\\" + DownjavaName + "\\bin\\java.exe";
                return true;
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "解压失败，Java压缩包可能已损坏，请重试！错误代码：" + ex.Message + "\n（注：若多次重试均无法解压的话，请自行去网络上下载安装并使用自定义模式来创建服务器）", "错误");
                return false;
            }
        }

        private void usedefault_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                txb4.IsEnabled = false;
                txb5.IsEnabled = false;
            }
        }

        private void useJVM_Checked(object sender, RoutedEventArgs e)
        {
            txb4.IsEnabled = true;
            txb5.IsEnabled = true;
        }

        private void a0002_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Title = "请选择文件";
            openfile.Filter = "JAR文件|*.jar|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                txb3.Text = openfile.FileName;
            }
        }

        private void a0003_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txb6.Text = dialog.SelectedPath;
            }
        }

        private void a0002_Copy_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openfile.Title = "请选择文件，通常为java.exe";
            openfile.Filter = "EXE文件|*.exe|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                txjava.Text = openfile.FileName;
            }
        }
        private void usedownloadjv_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                txjava.IsEnabled = false;
                a0002_Copy.IsEnabled = false;
            }
        }

        private void usecheckedjv_Checked(object sender, RoutedEventArgs e)
        {
            if (selectCheckedJavaComb.Items.Count == 0)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "请先进行检测", "提示");
                usedownloadjv.IsChecked = true;
                return;
            }
        }

        private async void SearchJavaBtn_Click(object sender, RoutedEventArgs e)
        {
            List<JavaScanner.JavaInfo> strings = null;
            int dialog = MagicShow.ShowMsg(Window.GetWindow(Window.GetWindow(this)), "即将开始检测电脑上的Java，此过程可能需要一些时间，请耐心等待。\n目前有两种检测模式，一种是简单检测，只检测一些关键目录，用时较少，普通用户可优先使用此模式。\n第二种是深度检测，将检测所有磁盘的所有目录，耗时可能会很久，请慎重选择！", "提示", true, "开始深度检测", "开始简单检测");
            if (dialog == 2)
            {
                return;
            }
            txjava.IsEnabled = false;
            a0002_Copy.IsEnabled = false;
            Dialog waitDialog = Dialog.Show(new TextDialog("检测中，请稍等……"));
            JavaScanner javaScanner = new();
            if (dialog == 1)
            {
                await Task.Run(async () => { Thread.Sleep(200); strings = await javaScanner.ScanJava(); });
            }
            else
            {
                await Task.Run(() => { Thread.Sleep(200); strings = javaScanner.SearchJava(); });
            }
            Window.GetWindow(this).Focus();
            waitDialog.Close();
            if (strings != null)
            {
                var javaList = strings.Select(info => $"Java{info.Version}: {info.Path}").ToList();
                selectCheckedJavaComb.ItemsSource = javaList;
                try
                {
                    JObject keyValuePairs = new JObject((JObject)JsonConvert.DeserializeObject(File.ReadAllText("MSL\\config.json")));
                    JArray jArray = new JArray(javaList);
                    if (keyValuePairs["javaList"] == null)
                    {
                        keyValuePairs.Add("javaList", jArray);
                    }
                    else
                    {
                        keyValuePairs["javaList"] = jArray;
                    }
                    File.WriteAllText("MSL\\config.json", Convert.ToString(keyValuePairs), Encoding.UTF8);
                }
                catch
                {
                    Console.WriteLine("Write Local-Java-List Failed(To Configuration)");
                }
            }
            if (selectCheckedJavaComb.Items.Count > 0)
            {
                Growl.Success("检测完毕！");
                selectCheckedJavaComb.SelectedIndex = 0;
            }
            else
            {
                Growl.Info("检测完毕，暂未找到Java");
                usedownloadjv.IsChecked = true;
            }
        }

        private async void usejvPath_Checked(object sender, RoutedEventArgs e)
        {
            Growl.Info("正在检查环境变量可用性，请稍等……");
            txjava.IsEnabled = false;
            a0002_Copy.IsEnabled = false;
            (bool javaAvailability, string javainfo) = await JavaScanner.CheckJavaAvailabilityAsync("java");
            if (javaAvailability)
            {
                Growl.Success("环境变量可用性检查完毕，您的环境变量正常！");
                usejvPath.Content = "使用环境变量：" + javainfo;
            }
            else
            {
                Growl.Error("检测环境变量失败");
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "检测环境变量失败，您的环境变量似乎不存在！", "错误");
                usedownloadjv.IsChecked = true;
            }
        }

        private void usejvNull_Checked(object sender, RoutedEventArgs e)
        {
            txjava.IsEnabled = false;
            a0002_Copy.IsEnabled = false;
        }

        private void useJVself_Checked(object sender, RoutedEventArgs e)
        {
            txjava.IsEnabled = true;
            a0002_Copy.IsEnabled = true;
        }
        private void usedownloadserver_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                txb3.IsEnabled = false;
                a0002.IsEnabled = false;
                textCustomCmd.IsEnabled = false;
            }
        }
        private void useServerself_Checked(object sender, RoutedEventArgs e)
        {
            txb3.IsEnabled = true;
            a0002.IsEnabled = true;
            textCustomCmd.IsEnabled = false;
        }

        private void useCustomCmd_Checked(object sender, RoutedEventArgs e)
        {
            textCustomCmd.IsEnabled = true;
            txb3.IsEnabled = false;
            a0002.IsEnabled = false;
        }

        private async void CustomModeDirNext_Click(object sender, RoutedEventArgs e)
        {
            servername = serverNameBox.Text;
            if ((new Regex("[\u4E00-\u9FA5]").IsMatch(txb6.Text)) || txb6.Text.Contains(" "))
            {
                if (!await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "使用带有中文字符或空格的路径可能造成编码错误，导致无法开服，您确定要继续吗？", "警告", true))
                {
                    return;
                }
            }
            if (!Path.IsPathRooted(txb6.Text))
            {
                serverbase = AppDomain.CurrentDomain.BaseDirectory + txb6.Text;
            }
            else
            {
                txb6.Text = txb6.Text;
                serverbase = txb6.Text;
            }

            List<string> strings = await AsyncGetJavaVersion();
            if (strings != null)
            {
                selectJavaComb.ItemsSource = strings.ToList();
                selectJavaComb.SelectedIndex = selectJavaComb.Items.Count - 1;
            }
            else
            {
                Growl.Error("出现错误，获取Java版本列表失败！");
            }

            try
            {
                JObject keyValuePairs = new JObject((JObject)JsonConvert.DeserializeObject(File.ReadAllText("MSL\\config.json")));
                if (keyValuePairs["javaList"] != null)
                {
                    selectCheckedJavaComb.ItemsSource = null;
                    selectCheckedJavaComb.ItemsSource = keyValuePairs["javaList"];
                    selectCheckedJavaComb.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load Local-Java-List Failed(From Configuration)" + ex.ToString());
            }
            sjava.IsSelected = true;
            sjava.IsEnabled = true;
            welcome.IsEnabled = false;
            returnMode = 4;
        }

        private async void CustomModeJavaNext_Click(object sender, RoutedEventArgs e)
        {
            bool noNext = false;
            CustomModeJavaNext.IsEnabled = false;
            CustomModeJavaReturn.IsEnabled = false;
            usedownloadserver.IsEnabled = true;
            usedownloadserver.IsChecked = true;
            useServerself.IsEnabled = true;
            if (useJVself.IsChecked == true)
            {
                Growl.Info("正在检查所选Java可用性，请稍等……");
                (bool javaAvailability, string javainfo) = await JavaScanner.CheckJavaAvailabilityAsync(txjava.Text);
                if (javaAvailability)
                {
                    Growl.Info("所选Java版本：" + javainfo);
                }
                else
                {
                    Growl.Error("检测Java可用性失败");
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "检测Java可用性失败，您的Java似乎不可用！请检查是否选择正确！", "错误");
                    usedownloadjv.IsChecked = true;
                    noNext = true;
                }
                serverjava = txjava.Text;
                await CheckServerPackCore();
            }
            else if (usejvPath.IsChecked == true)
            {
                serverjava = "Java";
                await CheckServerPackCore();
            }
            else if (usejvNull.IsChecked == true)
            {
                serverjava = "";
                usedownloadserver.IsEnabled = false;
                useServerself.IsEnabled = false;
                useCustomCmd.IsChecked = true;
                await CheckServerPackCore();
            }
            else if (usecheckedjv.IsChecked == true)
            {
                string a = selectCheckedJavaComb.Items[selectCheckedJavaComb.SelectedIndex].ToString();
                serverjava = a.Substring(a.IndexOf(":") + 2);
                await CheckServerPackCore();
            }
            else if (usedownloadjv.IsChecked == true)
            {
                try
                {
                    int dwnJava = 0;
                    dwnJava = await DownloadJava(selectJavaComb.SelectedItem.ToString(), (await HttpService.GetApiContentAsync("download/java/" + selectJavaComb.SelectedItem.ToString()))["data"]["url"].ToString());
                    if (dwnJava == 1)
                    {
                        MagicDialog MagicDialog = new MagicDialog();
                        MagicDialog.ShowTextDialog(Window.GetWindow(this), "解压Java中……");
                        bool unzipJava = await UnzipJava();
                        MagicDialog.CloseTextDialog();
                        if (unzipJava)
                        {
                            await CheckServerPackCore();
                        }
                        else
                        {
                            noNext = true;
                        }
                    }
                    else if (dwnJava == 2)
                    {
                        await CheckServerPackCore();
                    }
                    else
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载取消！", "提示");
                        noNext = true;
                    }
                }
                catch
                {
                    Growl.Error("出现错误，请检查网络连接！");
                    noNext = true;
                }
            }
            CustomModeJavaNext.IsEnabled = true;
            CustomModeJavaReturn.IsEnabled = true;
            if (!noNext && !isImportPack)
            {
                sserver.IsSelected = true;
                sserver.IsEnabled = true;
                sjava.IsEnabled = false;
                returnMode = 5;
            }
        }

        private async void CustomModeServerCoreNext_Click(object sender, RoutedEventArgs e)
        {
            if (usedownloadserver.IsChecked == true) // 下载服务端核心
            {
                DownloadServer downloadServer = new DownloadServer(serverbase, DownloadServer.Mode.CreateServer, serverjava)
                {
                    Owner = Window.GetWindow(Window.GetWindow(this))
                };
                downloadServer.ShowDialog();
                if (downloadServer.FileName != null)
                {
                    if (File.Exists(serverbase + "\\" + downloadServer.FileName))
                    {
                        servercore = downloadServer.FileName;
                        sJVM.IsSelected = true;
                        sJVM.IsEnabled = true;
                        sserver.IsEnabled = false;
                        returnMode = 6;
                    }
                    else if (downloadServer.FileName.StartsWith("@libraries/"))
                    {
                        servercore = downloadServer.FileName;
                        sJVM.IsSelected = true;
                        sJVM.IsEnabled = true;
                        sserver.IsEnabled = false;
                        returnMode = 6;
                    }
                }
                downloadServer.Dispose();
            }
            else if (useServerself.IsChecked == true) // 自定义服务端核心文件
            {
                try
                {
                    Directory.CreateDirectory(serverbase);
                    // 检查文件是否存在在服务器文件夹
                    string _filename = Path.GetFileName(txb3.Text);
                    if (File.Exists(serverbase + "\\" + _filename)) // 存在
                    {
                        txb3.Text = _filename;
                    }
                    else // 不存在（？）
                    {
                        if (!Path.IsPathRooted(txb3.Text) && File.Exists(AppDomain.CurrentDomain.BaseDirectory + txb3.Text))  // 哦其实是相对路径，在MSL.exe所在的文件夹内（呼~）
                        {
                            txb3.Text = AppDomain.CurrentDomain.BaseDirectory + txb3.Text; // 如果是相对路径的话就得改成绝对路径了（因为服务端文件在MSL.exe所在文件夹而非服务器运行目录）
                            await MoveFileInServerBase(_filename); // 然后再询问是否将文件移动到服务器目录（见此代码块下方代码块）
                        }
                        else if (Path.GetDirectoryName(txb3.Text) != serverbase) // 绝对不存在！！！（恼！）
                        {
                            await MoveFileInServerBase(_filename); // 是否将文件移动到服务器目录（见此代码块下方代码块）
                        }
                    }

                    // 检测用户输入的是单个文件还是完整路径
                    string fullFileName;
                    if (File.Exists(serverbase + "\\" + txb3.Text))
                    {
                        fullFileName = serverbase + "\\" + txb3.Text;
                    }
                    else
                    {
                        fullFileName = txb3.Text;
                    }

                    // 检查是否为forge端
                    if (Functions.CheckForgeInstaller(fullFileName))
                    {
                        bool dialog = await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "您选择的服务端疑似是forge安装器，是否将其展开安装？\n如果不展开安装，服务器可能无法开启！", "提示", true, "取消");
                        if (dialog)
                        {
                            string installReturn = await InstallForge(txb3.Text);
                            if (installReturn == null)
                            {
                                MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载失败！", "错误");
                                return;
                            }
                            txb3.Text = installReturn;
                        }
                    }
                    servercore = txb3.Text;
                    sJVM.IsSelected = true;
                    sJVM.IsEnabled = true;
                    sserver.IsEnabled = false;
                    returnMode = 6;
                }
                catch (Exception ex)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), ex.Message, "错误");
                }
            }
            else // 自定义指令模式
            {
                launchmode = 1; // 1是自定义命令模式
                serverargs = textCustomCmd.Text; //存放完整的args
                // 若为自定义命令模式，就跳过设置开服内存和JVM参数的阶段
                servermemory = "";
                SelectTerminalGrid.Visibility = Visibility.Visible;
                tabCtrl.Visibility = Visibility.Collapsed;
                returnMode = 6;
            }
        }

        private async Task MoveFileInServerBase(string _filename)
        {
            if (await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "所选的服务端核心文件并不在服务器目录中，是否将其复制进服务器目录？\n若不复制，请留意勿将核心文件删除！", "提示", true))
            {
                File.Copy(txb3.Text, serverbase + "\\" + _filename, true);
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "已将服务端核心复制到了服务器目录之中，您现在可以将源文件删除了！", "提示");
                txb3.Text = _filename;
            }
        }

        private void CustomModeFinallyNext_Click(object sender, RoutedEventArgs e)
        {
            if (usedefault.IsChecked == true)
            {
                servermemory = "";
            }
            else
            {
                if (string.IsNullOrEmpty(txb4.Text) || string.IsNullOrEmpty(txb5.Text))
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "请填写开服内存信息！", "错误");
                    return;
                }
                if (txb4.Text.All(char.IsDigit) == true && txb5.Text.All(char.IsDigit) == true)
                {
                    string xmsUnit = "M";
                    string xmxUnit = "M";
                    if (XmsUnit.SelectedIndex == 1)
                    {
                        xmsUnit = "G";
                    }
                    if (XmxUnit.SelectedIndex == 1)
                    {
                        xmxUnit = "G";
                    }
                    servermemory = "-Xms" + txb4.Text + xmsUnit + " -Xmx" + txb5.Text + xmxUnit;
                }
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "开服内存参数不正确（只能为纯数字）！", "错误");
                    return;
                }
            }
            serverargs += txb7.Text;
            if (!Directory.Exists(serverbase))
            {
                Directory.CreateDirectory(serverbase);
            }
            SelectTerminalGrid.Visibility = Visibility.Visible;
            tabCtrl.Visibility = Visibility.Collapsed;
            returnMode = 7;
        }

        private async void usebasicfastJvm_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)usebasicfastJvm.IsChecked)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "使用优化参数需要手动设置大小相同的内存，请对上面的内存进行更改！Java11及以上请勿选择此优化参数！", "警告");
                useJVM.IsChecked = true;
                usefastJvm.IsChecked = false;
                txb7.Text = "-XX:+AggressiveOpts";
                txb4.Text = "2048";
                txb5.Text = "2048";
            }
            else
            {
                txb7.Text = string.Empty;
            }
        }

        private async void usefastJvm_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)usefastJvm.IsChecked)
            {
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "使用优化参数需要手动设置大小相同的内存，请对上面的内存进行更改！", "警告");
                useJVM.IsChecked = true;
                usebasicfastJvm.IsChecked = false;
                txb7.Text = "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 -XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC -XX:+AlwaysPreTouch -XX:G1NewSizePercent=30 -XX:G1MaxNewSizePercent=40 -XX:G1HeapRegionSize=8M -XX:G1ReservePercent=20 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 -XX:InitiatingHeapOccupancyPercent=15 -XX:G1MixedGCLiveThresholdPercent=90 -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 -Dusing.aikars.flags=https://mcflags.emc.gs -Daikars.new.flags=true";
                txb4.Text = "4096";
                txb5.Text = "4096";
            }
            else
            {
                txb7.Text = string.Empty;
            }
        }

        //用于分类的字典
        public static JObject serverCoreTypes;
        string[] serverTypes;
        private async Task FastModeGetCore()
        {
            try
            {
                //获取分类
                var responseString = (await HttpService.GetApiContentAsync("query/server_classify"))["data"].ToString();
                serverCoreTypes = (JObject)JsonConvert.DeserializeObject(responseString);
                string jsonData = (await HttpService.GetApiContentAsync("query/available_server_types"))["data"]["types"].ToString();
                serverTypes = JsonConvert.DeserializeObject<string[]>(jsonData);
                ServerCoreCombo.SelectedIndex = 0;
            }
            catch (Exception a)
            {
                Growl.Info("获取服务端失败！请重试" + a.Message);
            }
        }

        private List<string> typeVersions = new List<string>();
        private async void ServerCoreCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerCoreCombo.SelectedIndex == -1)
            {
                return;
            }
            FastModeNextBtn.IsEnabled = false;
            ServerVersionCombo.ItemsSource = null;
            typeVersions.Clear();
            tempServerCore.Clear();
            if (serverTypes == null)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "服务端正在加载中，请稍后再选择！", "提示");
                return;
            }
            await GetServerVersion();
        }

        private async Task GetServerVersion()
        {
            ServerCoreCombo.IsEnabled = false;
            ServerCoreDescrip.Text = "加载中，请稍等……";
            try
            {
                int i = 0;
                foreach (var serverType in serverCoreTypes)
                {
                    if (i == ServerCoreCombo.SelectedIndex)
                    {
                        //MessageBox.Show(serverType.Key + "\n" + serverType.Value);
                        await ProcessServerType((JArray)serverType.Value);
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "出现错误：" + ex.Message, "ERR");
                FastModeNextBtn.IsEnabled = true;
                return;
            }
            var sortedList = typeVersions.OrderByDescending(p => Functions.VersionCompare(p)).ToList();
            ServerCoreCombo.IsEnabled = true;
            FastModeNextBtn.IsEnabled = true;
            ServerVersionCombo.ItemsSource = sortedList;
            ServerVersionCombo.SelectedIndex = 0;
            switch (ServerCoreCombo.SelectedIndex)
            {
                case 0:
                    ServerCoreDescrip.Text = "插件服务器：指在服务端添加插件（客户端无需添加），通过更改服务端底层来增加功能，这种方式极易做到对服务器、服务器用户玩家进行管理，如权限组、封禁系统等，但这种方式不能修改客户端内容，所以也导致很多功能很难实现，如添加新的物品，只能通过更改材质包的方式让客户端显示新的物品";
                    break;
                case 1:
                    ServerCoreDescrip.Text = "插件模组混合服务器（Forge加载器）：这种服务器将插件服务端和Forge服务端合二为一，既吸取了二者的优点（服务器管理功能可通过添加插件做到，添加新物品更改游戏玩法可通过添加模组做到），同时又有许多缺点（如服务器不稳定，同时添加插件和模组，极易造成冲突问题，且也存在模组服务器服务端和客户端需要同步模组的问题）";
                    break;
                case 2:
                    ServerCoreDescrip.Text = "插件模组混合服务器（Fabric加载器）：这种服务器将插件服务端和Fabric服务端合二为一，既吸取了二者的优点（服务器管理功能可通过添加插件做到，添加新物品更改游戏玩法可通过添加模组做到），同时又有许多缺点（如服务器不稳定，同时添加插件和模组，极易造成冲突问题，且也存在模组服务器服务端和客户端需要同步模组的问题）";
                    break;
                case 3:
                    ServerCoreDescrip.Text = "模组服务器（Forge加载器）：指通过Forge加载器，添加模组来增加功能（服务端和客户端均需添加），这种方式既可以更改服务端的内容，也可以更改客户端的内容，所以插件服务器无法实现的功能在这里即可轻易做到，但是这种方式很难做到插件服的管理功能，且需要客户端的模组和服务端进行同步，会给玩家造成一定的麻烦";
                    break;
                case 4:
                    ServerCoreDescrip.Text = "模组服务器（Fabric加载器）：指通过Fabric加载器，添加模组来增加功能（服务端和客户端均需添加），这种方式既可以更改服务端的内容，也可以更改客户端的内容，所以插件服务器无法实现的功能在这里即可轻易做到，但是这种方式很难做到插件服的管理功能，且需要客户端的模组和服务端进行同步，会给玩家造成一定的麻烦";
                    break;
                case 5:
                    ServerCoreDescrip.Text = "原版服务器：Mojang纯原生服务器，不能添加任何插件或模组，给您原汁原味的体验";
                    break;
                case 6:
                    ServerCoreDescrip.Text = "基岩版服务器：专为基岩版提供的服务器，这种服务器在配置等方面和Java版服务器不太一样，同时开服器也不太适配，更改配置文件等相关操作只能您手动操作";
                    break;
                case 7:
                    ServerCoreDescrip.Text = "代理服务器：指Java版群组服务器的转发服务器，这种服务器相当于一个桥梁，将玩家在不同的服务器之间进行传送转发，使用这种服务器您首先需要开启一个普通服务器，因为这种服务器没有游戏内容，如果没有普通服务器进行连接，玩家根本无法进入，且目前开服器并不兼容这种服务器，创建完毕后您需在列表右键该服务器并使用“命令行开服”功能来启动";
                    break;
            }
        }

        private async Task ProcessServerType(JArray serverType)
        {
            foreach (var coreType in serverType)
            {
                //MessageBox.Show(coreType.ToString());
                var serverVersions = await TryGetServerVersions(coreType.ToString());
                if (serverVersions == null)
                {
                    Console.WriteLine("获取" + coreType + "服务端失败！继续下一个……");
                    continue;
                }

                foreach (var version in serverVersions)
                {
                    if (!typeVersions.Contains(version))
                    {
                        typeVersions.Add(version);
                    }
                }
            }
        }

        private async Task<List<string>> TryGetServerVersions(string serverType)
        {
            try
            {
                var resultData = (await HttpService.GetApiContentAsync("query/available_versions/" + serverType))["data"]["versionList"].ToString();
                tempServerCore.Add(serverType, resultData);
                return JsonConvert.DeserializeObject<List<string>>(resultData);
            }
            catch
            {
                return null;
            }
        }

        private readonly Dictionary<string, string> tempServerCore = new Dictionary<string, string>();
        private async void FastModeNextBtn_Click(object sender, RoutedEventArgs e)
        {
            servername = ServerNameBox.Text;
            if ((new Regex("[\u4E00-\u9FA5]").IsMatch(txb6.Text)) || txb6.Text.Contains(" "))
            {
                if (!await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "开服器被放置于带有中文字符或空格的目录里，这可能会造成编码错误，从而无法开服，您确定要继续吗？", "警告", true))
                {
                    return;
                }
            }
            serverbase = txb6.Text;
            FinallyCoreCombo.Items.Clear();
            FastModeNextBtn.IsEnabled = false;

            foreach (var _item in tempServerCore)
            {
                List<string> serverVersions = JsonConvert.DeserializeObject<List<string>>(_item.Value);
                foreach (var version in serverVersions)
                {
                    if (version == ServerVersionCombo.SelectedItem.ToString() && !FinallyCoreCombo.Items.Contains(_item.Key + "-" + version))
                    {
                        FinallyCoreCombo.Items.Add(_item.Key + "-" + version);
                    }
                }
            }

            List<string> strings = await AsyncGetJavaVersion();
            if (strings != null)
            {
                FinallyJavaCombo.ItemsSource = strings.ToList();
            }
            else
            {
                Growl.Error("出现错误，获取Java版本列表失败！");
            }

            string javaVersion;
            string versionString = ServerVersionCombo.Items[ServerVersionCombo.SelectedIndex].ToString();
            if (versionString.Contains("-"))
            {
                versionString = versionString.Substring(0, versionString.IndexOf("-"));
            }
            if (Regex.IsMatch(versionString, @"^[\d.]+$"))
            {
                string[] components = versionString.Split('.');
                if (components.Length >= 3 && int.TryParse(components[2], out int _))
                {
                    versionString = $"{components[0]}.{components[1]}"; // remove the last component
                }

                Version _version = new Version(versionString);
                Version targetVersion1 = new Version("1.7");
                Version targetVersion2 = new Version("1.12");
                Version targetVersion3 = new Version("1.16");
                Version targetVersion4 = new Version("1.20.4");

                if (_version <= targetVersion1)
                {
                    //_version <=1.7
                    FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java7-Java8";
                    javaVersion = "Java8";
                }
                else if (_version <= targetVersion2)
                {
                    //1.7< _version <=1.12
                    FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java8-Java11";
                    javaVersion = "Java8";
                }
                else if (_version <= targetVersion3)
                {
                    //1.12< _version <=1.16
                    FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java11-Java17（或更高）";
                    javaVersion = "Java11";
                }
                else if (_version <= targetVersion4)
                {
                    //1.16< _version <=1.20.4
                    FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java17及以上";
                    javaVersion = "Java17";
                }
                else
                {
                    //1.20.4< _version
                    FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java21及以上";
                    javaVersion = "Java21";
                }
            }
            else
            {
                FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java8-Java21（或更高）";
                javaVersion = "Java21";
            }
            FinallyJavaCombo.SelectedIndex = FinallyJavaCombo.Items.Count - 1;
            foreach (var item in FinallyJavaCombo.Items)
            {
                if (item.ToString() == javaVersion)
                {
                    FinallyJavaCombo.SelectedItem = item;
                    break;
                }
            }
            FinallyCoreCombo.SelectedIndex = 0;
            FastModeNextBtn.IsEnabled = true;
            FastModeGrid.Visibility = Visibility.Collapsed;
            InstallGrid.Visibility = Visibility.Visible;
            returnMode = 2;
        }

        private async Task<List<string>> AsyncGetJavaVersion()
        {
            MagicDialog MagicDialog = new MagicDialog();
            MagicDialog.ShowTextDialog(Window.GetWindow(this), "获取Java版本列表中，请稍等……");
            await Task.Delay(100);
            try
            {
                string response = string.Empty;
                response = (await HttpService.GetApiContentAsync("query/java"))["data"]["versionList"].ToString();
                await Task.Delay(100);
                JArray jArray = JArray.Parse(response);
                List<string> strings = new List<string>();
                foreach (var j in jArray)
                {
                    strings.Add(j.ToString());
                }
                MagicDialog.CloseTextDialog();
                return strings;
            }
            catch
            {
                MagicDialog.CloseTextDialog();
                return null;
            }
        }

        private async void FastModeInstallBtn_Click(object sender, RoutedEventArgs e)
        {
            FastModeReturnBtn.IsEnabled = false;
            FastModeInstallBtn.IsEnabled = false;
            try
            {
                FastInstallProcess.Text = "当前进度:下载Java……";
                int dwnJava = 0;
                dwnJava = await DownloadJava(FinallyJavaCombo.SelectedItem.ToString(), (await HttpService.GetApiContentAsync("download/java/" + FinallyJavaCombo.SelectedItem.ToString()))["data"]["url"].ToString());
                if (dwnJava == 1)
                {
                    FastInstallProcess.Text = "当前进度:解压Java……";
                    MagicDialog MagicDialog = new MagicDialog();
                    MagicDialog.ShowTextDialog(Window.GetWindow(this), "解压Java中……");
                    bool unzipJava = await UnzipJava();
                    MagicDialog.CloseTextDialog();
                    if (unzipJava)
                    {
                        FastInstallProcess.Text = "当前进度:下载服务端……";
                        await FastModeInstallCore();
                    }
                    else
                    {
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                }
                else if (dwnJava == 2)
                {
                    FastInstallProcess.Text = "当前进度:下载服务端……";
                    await FastModeInstallCore();
                }
                else
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载取消！", "提示");
                    FastInstallProcess.Text = "取消安装！";
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
            }
            catch(Exception ex)
            {
                MagicShow.ShowMsgDialog(Window.GetWindow(this), "出现错误，请重试！\n"+ex.Message, "错误");
                FastInstallProcess.Text = "取消安装！";
                FastModeReturnBtn.IsEnabled = true;
                FastModeInstallBtn.IsEnabled = true;
                return;
            }
        }

        private async Task FastModeInstallCore()
        {
            string finallyServerCore = FinallyCoreCombo.SelectedItem.ToString();
            string serverCoreType = finallyServerCore.Substring(0, finallyServerCore.LastIndexOf("-"));
            string serverCoreVersion = finallyServerCore.Substring(finallyServerCore.LastIndexOf("-") + 1);
            string filename = finallyServerCore + ".jar";
            JObject dlContext = await HttpService.GetApiContentAsync("download/server/" + serverCoreType + "/" + serverCoreVersion);//获取链接
            string dlUrl = dlContext["data"]["url"].ToString();
            string sha256Exp = dlContext["data"]["sha256"]?.ToString() ?? string.Empty;

            bool _enableParalle = true;
            if (serverCoreType == "vanilla")
                _enableParalle = false;

            if (serverCoreType == "forge" || serverCoreType == "spongeforge" || serverCoreType == "neoforge")
            {
                int dwnDialog = await MagicShow.ShowDownloaderWithIntReturn(Window.GetWindow(this), dlUrl, serverbase, filename, "下载服务端中……", sha256Exp, true, false); //从这里请求服务端下载
                if (dwnDialog == 2)
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载取消！", "提示");
                    FastInstallProcess.Text = "取消安装！";
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
            }
            else
            {
                bool dwnDialog = await MagicShow.ShowDownloader(Window.GetWindow(this), dlUrl, serverbase, filename, "下载服务端中……", sha256Exp, enableParalle: _enableParalle); //从这里请求服务端下载
                if (!dwnDialog || !File.Exists(serverbase + "\\" + filename))
                {
                    MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载取消！（或服务端文件不存在）", "提示");
                    FastInstallProcess.Text = "取消安装！";
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
            }

            string installReturn;
            switch (serverCoreType)
            {
                case "spongeforge":
                    string forgeName = finallyServerCore.Replace("spongeforge", "forge");
                    string _filename = forgeName + ".jar";
                    JObject _dlContext = await HttpService.GetApiContentAsync("download/server/" + forgeName.Replace("-", "/"));
                    string _dlUrl = _dlContext["data"]["url"].ToString();
                    string _sha256Exp = _dlContext["data"]["sha256"]?.ToString() ?? string.Empty;
                    int _dwnDialog = await MagicShow.ShowDownloaderWithIntReturn(Window.GetWindow(this), _dlUrl, serverbase, _filename, "下载服务端中……", _sha256Exp, true);

                    if (_dwnDialog == 2)
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载取消！", "提示");
                        FastInstallProcess.Text = "取消安装！";
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }

                    // Check if file exists and download succeeded
                    if (!File.Exists(serverbase + "\\" + _filename))
                    {
                        // Extract version info and create backup URL
                        var query = new Uri(_dlUrl).Query;
                        var queryDictionary = System.Web.HttpUtility.ParseQueryString(query);
                        string mcVersion = queryDictionary["mcversion"];
                        string forgeVersion = queryDictionary["version"];
                        string[] components = mcVersion.Split('.');
                        string _mcMajorVersion = mcVersion;
                        if (components.Length >= 3 && int.TryParse(components[2], out int _))
                        {
                            _mcMajorVersion = $"{components[0]}.{components[1]}"; // remove the last component
                        }
                        if (new Version(_mcMajorVersion) < new Version("1.10"))
                        {
                            forgeVersion += "-" + mcVersion;
                        }
                        string backupUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{mcVersion}-{forgeVersion}/{forgeName}-{forgeVersion}-installer.jar";

                        // Attempt to download from backup URL
                        bool backupDownloadSuccess = await MagicShow.ShowDownloader(Window.GetWindow(this), backupUrl, serverbase, _filename, "备用链接下载中……", _sha256Exp);
                        if (!backupDownloadSuccess || !File.Exists(serverbase + "\\" + _filename))
                        {
                            MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载取消！（或服务端文件不存在）", "错误");
                            FastInstallProcess.Text = "取消安装！";
                            FastModeReturnBtn.IsEnabled = true;
                            FastModeInstallBtn.IsEnabled = true;
                            return;
                        }
                    }
                    //sponge应当作为模组加载，所以要再下载一个forge才是服务端
                    try
                    {
                        //移动到mods文件夹
                        Directory.CreateDirectory(serverbase + "\\mods\\");
                        if (File.Exists(serverbase + "\\mods\\" + filename))
                        {
                            File.Delete(serverbase + "\\mods\\" + filename);
                        }
                        File.Move(serverbase + "\\" + filename, serverbase + "\\mods\\" + filename);
                    }
                    catch (Exception e)
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "Sponge核心移动失败！\n请重试！" + e.Message, "错误");
                        return;
                    }
                    installReturn = await InstallForge(_filename);
                    if (installReturn == null)
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "安装失败！", "错误");
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }

                    servercore = installReturn;
                    break;
                    /*
                case "banner":
                    //banner应当作为模组加载，所以要再下载一个fabric才是服务端
                    try
                    {
                        //移动到mods文件夹
                        Directory.CreateDirectory(serverbase + "\\mods\\");
                        if (File.Exists(serverbase + "\\mods\\" + filename))
                        {
                            File.Delete(serverbase + "\\mods\\" + filename);
                        }
                        File.Move(serverbase + "\\" + filename, serverbase + "\\mods\\" + filename);
                    }
                    catch (Exception e)
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "Banner端移动失败！\n请重试！" + e.Message, "错误");
                        return;
                    }

                    //下载一个fabric端
                    //获取版本号
                    string bannerVersion = filename.Replace("banner-", "").Replace(".jar", "");
                    bool dwnFabric = await MagicShow.ShowDownloader(Window.GetWindow(this), (await HttpService.GetApiContentAsync("download/server/fabric/" + bannerVersion))["data"]["url"].ToString(), serverbase, $"fabric-{bannerVersion}.jar", "下载Fabric端中···");
                    if (!dwnFabric || !File.Exists(serverbase + "\\" + $"fabric-{bannerVersion}.jar"))
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "Fabric端下载取消（或服务端文件不存在）！", "错误");
                        return;
                    }

                    //下载Vanilla端
                    if (!await DownloadVanilla(serverbase + "\\.fabric\\server", serverCoreVersion + "-server.jar", serverCoreVersion))
                    {
                        FastInstallProcess.Text = "请重试！";
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                    servercore = $"fabric-{bannerVersion}.jar";
                    break; */
                case "neoforge":
                    if (!File.Exists(serverbase + "\\" + filename))
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载失败！（或服务端文件不存在）", "提示");
                        FastInstallProcess.Text = "取消安装！";
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                    installReturn = await InstallForge(filename);
                    if (installReturn == null)
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "安装失败！", "错误");
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                    servercore = installReturn;
                    break;
                case "forge":
                    // Check if file exists and download succeeded
                    if (!File.Exists(serverbase + "\\" + filename))
                    {
                        // Extract version info and create backup URL
                        var query = new Uri(dlUrl).Query;
                        var queryDictionary = System.Web.HttpUtility.ParseQueryString(query);
                        string mcVersion = queryDictionary["mcversion"];
                        string forgeVersion = queryDictionary["version"];
                        string[] components = mcVersion.Split('.');
                        string _mcMajorVersion = mcVersion;
                        if (components.Length >= 3 && int.TryParse(components[2], out int _))
                        {
                            _mcMajorVersion = $"{components[0]}.{components[1]}"; // remove the last component
                        }
                        if (new Version(_mcMajorVersion) < new Version("1.10"))
                        {
                            forgeVersion += "-" + mcVersion;
                        }
                        string backupUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{mcVersion}-{forgeVersion}/{finallyServerCore}-{forgeVersion}-installer.jar";

                        // Attempt to download from backup URL
                        bool backupDownloadSuccess = await MagicShow.ShowDownloader(Window.GetWindow(this), backupUrl, serverbase, filename, "备用链接下载中……", sha256Exp);
                        if (!backupDownloadSuccess || !File.Exists(serverbase + "\\" + filename))
                        {
                            MagicShow.ShowMsgDialog(Window.GetWindow(this), "下载取消！（或服务端文件不存在）", "错误");
                            FastInstallProcess.Text = "取消安装！";
                            FastModeReturnBtn.IsEnabled = true;
                            FastModeInstallBtn.IsEnabled = true;
                            return;
                        }
                    }
                    installReturn = await InstallForge(filename);
                    if (installReturn == null)
                    {
                        MagicShow.ShowMsgDialog(Window.GetWindow(this), "安装失败！", "错误");
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                    servercore = installReturn;
                    break;
                case "fabric":
                    //下载Vanilla端
                    if (!await DownloadVanilla(serverbase + "\\.fabric\\server", serverCoreVersion + "-server.jar", serverCoreVersion))
                    {
                        FastInstallProcess.Text = "请重试！";
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                    servercore = filename;
                    break;
                case "paper":
                case "leaves":
                case "folia":
                case "purpur":
                case "leaf":
                    if (!await DownloadVanilla(serverbase + "\\cache", "mojang_" + serverCoreVersion + ".jar", serverCoreVersion))
                    {
                        FastInstallProcess.Text = "请重试！";
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                    servercore = filename;
                    break;
                default:
                    servercore = filename;
                    break;
            }
            FastModeReturnBtn.IsEnabled = true;
            FastModeInstallBtn.IsEnabled = true;
            FastInstallProcess.Text = "当前进度:下载完成！";
            SelectTerminalGrid.Visibility = Visibility.Visible;
            InstallGrid.Visibility = Visibility.Collapsed;
            returnMode = 3;
        }

        private async Task<bool> DownloadVanilla(string path, string filename, string version)
        {
            try
            {
                JObject downContext = await HttpService.GetApiContentAsync("download/server/vanilla/" + version);
                string downUrl = downContext["data"]["url"].ToString();

                string sha256Exp = downContext["data"]["sha256"]?.ToString() ?? string.Empty;

                int dwnDialog = await MagicShow.ShowDownloaderWithIntReturn(Window.GetWindow(this), downUrl, path, filename, "下载依赖中（原版服务端）……", sha256Exp, true, false);
                if (dwnDialog == 2)
                {
                    if (!await MagicShow.ShowMsgDialogAsync("Vanilla端下载失败！此依赖在服务器运行时依旧会进行下载，在此处您要暂时跳过吗？", "错误", true))
                        return false;
                    else
                        return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                if (!await MagicShow.ShowMsgDialogAsync("Vanilla端下载失败！此依赖在服务器运行时依旧会进行下载，在此处您要暂时跳过吗？\n错误: "+ex.Message, "错误", true))
                    return false;
                else
                    return true;
            }

        }

        private async Task<string> InstallForge(string filename)
        {
            //调用新版forge安装器
            string[] installForge = await MagicShow.ShowInstallForge(Window.GetWindow(this), serverbase, filename, serverjava);
            if (installForge[0] == "0")
            {
                if (await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "自动安装失败！是否尝试使用命令行安装方式？", "错误", true))
                {
                    return Functions.InstallForge(serverjava, serverbase, filename, string.Empty, false);
                }
                else
                {
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return null;
                }
            }
            else if (installForge[0] == "1")
            {
                string _ret = Functions.InstallForge(serverjava, serverbase, filename, installForge[1]);
                if (_ret == null)
                {
                    return Functions.InstallForge(serverjava, serverbase, filename, installForge[1], false);
                }
                else
                {
                    return _ret;
                }
            }
            else if (installForge[0] == "3")
            {
                return Functions.InstallForge(serverjava, serverbase, filename, string.Empty, false);
            }
            else
            {
                FastInstallProcess.Text = "已取消！";
                FastModeReturnBtn.IsEnabled = true;
                FastModeInstallBtn.IsEnabled = true;
                return null;
            }
        }


        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            GotoServerList();
            ReInit();
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            switch (returnMode)
            {
                case 1:
                    if (isImportPack)
                    {
                        isImportPack = false;
                        welcome.IsSelected = true;
                        welcome.IsEnabled = true;
                        sjava.IsEnabled = false;
                    }
                    MainGrid.Visibility = Visibility.Visible;
                    tabCtrl.Visibility = Visibility.Collapsed;
                    FastModeGrid.Visibility = Visibility.Collapsed;
                    returnMode = 0;
                    break;
                case 2:
                    FastModeGrid.Visibility = Visibility.Visible;
                    InstallGrid.Visibility = Visibility.Collapsed;
                    returnMode = 1;
                    break;
                case 3:
                    InstallGrid.Visibility = Visibility.Visible;
                    SelectTerminalGrid.Visibility = Visibility.Collapsed;
                    returnMode = 2;
                    break;
                case 4:
                    welcome.IsSelected = true;
                    welcome.IsEnabled = true;
                    sjava.IsEnabled = false;
                    returnMode = 1;
                    break;
                case 5:
                    sjava.IsSelected = true;
                    sjava.IsEnabled = true;
                    sserver.IsEnabled = false;
                    returnMode = 4;
                    break;
                case 6:
                    sserver.IsSelected = true;
                    sserver.IsEnabled = true;
                    sJVM.IsEnabled = false;
                    if (launchmode == 1)
                    {
                        tabCtrl.Visibility = Visibility.Visible;
                        SelectTerminalGrid.Visibility = Visibility.Collapsed;
                        launchmode = 0;
                    }
                    returnMode = 5;
                    break;
                case 7:
                    tabCtrl.Visibility = Visibility.Visible;
                    SelectTerminalGrid.Visibility = Visibility.Collapsed;
                    returnMode = 6;
                    break;
            }
        }

        private void TraditionModeBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                ConptyModeBtn.IsChecked = false;
            }
        }

        private void ConptyModeBtn_Checked(object sender, RoutedEventArgs e)
        {
            TraditionModeBtn.IsChecked = false;
            MagicShow.ShowMsgDialog(Window.GetWindow(this), "若使用此模式出现问题，可在服务器运行窗口的“更多功能”界面修改此项。", "提示");
        }

        private async void DoneBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(@"MSL\ServerList.json"))
                {
                    File.WriteAllText(@"MSL\ServerList.json", string.Format("{{{0}}}", "\n"));
                }
                JObject _json = new JObject
                    {
                        { "name", servername },
                        { "java", serverjava },
                        { "base", serverbase },
                        { "core", servercore },
                        { "memory", servermemory },
                        { "args", serverargs }
                    };
                if (launchmode == 1)
                {
                    _json.Add("mode", launchmode);
                }
                if (ConptyModeBtn.IsChecked == true)
                {
                    _json.Add("useConpty", "True");
                }
                if (!string.IsNullOrEmpty(txb_ygg_api.Text.Trim()))
                {
                    _json.Add("ygg_api", txb_ygg_api.Text.Trim());
                }
                JObject jsonObject = JObject.Parse(File.ReadAllText(@"MSL\ServerList.json", Encoding.UTF8));
                List<string> keys = jsonObject.Properties().Select(p => p.Name).ToList();
                var _keys = keys.Select(x => Convert.ToInt32(x));
                int[] ikeys = _keys.ToArray();
                Array.Sort(ikeys);
                int i = 0;

                foreach (int key in ikeys)
                {
                    if (i == key)
                    {
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
                jsonObject.Add(i.ToString(), _json);
                File.WriteAllText(@"MSL\ServerList.json", Convert.ToString(jsonObject), Encoding.UTF8);
                await MagicShow.ShowMsgDialogAsync(Window.GetWindow(this), "创建完毕，请点击“开启服务器”按钮以开服", "信息");
                GotoServerList();
                ReInit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("出现错误，请重试：" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReInit()
        {
            returnMode = 0;
            launchmode = 0;
            DownjavaName = null;
            servername = null;
            serverjava = null;
            serverbase = null;
            servercore = null;
            servermemory = null;
            serverargs = null;
            ServerCoreCombo.SelectedIndex = -1;
            FastInstallProcess.Text = string.Empty;
            serverNameBox.Text = "MyServer";
            txb6.Text = string.Empty;
            usedownloadjv.IsChecked = true;
            selectJavaComb.ItemsSource = null;
            usejvPath.Content = "使用环境变量";
            selectCheckedJavaComb.ItemsSource = null;
            txjava.Text = string.Empty;
            usedownloadserver.IsChecked = true;
            txb3.Text = string.Empty;
            textCustomCmd.Text = string.Empty;
            usedefault.IsChecked = true;
            txb4.Text = string.Empty;
            txb5.Text = string.Empty;
            usebasicfastJvm.IsChecked = false;
            usefastJvm.IsChecked = false;
            txb7.Text = string.Empty;
            TraditionModeBtn.IsChecked = true;
            isImportPack = false;
            MainGrid.Visibility = Visibility.Visible;
            tabCtrl.Visibility = Visibility.Collapsed;
            FastModeGrid.Visibility = Visibility.Collapsed;
            InstallGrid.Visibility = Visibility.Collapsed;
            SelectTerminalGrid.Visibility = Visibility.Collapsed;
            welcome.IsSelected = true;
            welcome.IsEnabled = true;
            sjava.IsEnabled = false;
            sserver.IsEnabled = false;
            sJVM.IsEnabled = false;
            GC.Collect(); // find finalizable objects
            GC.WaitForPendingFinalizers(); // wait until finalizers executed
            GC.Collect(); // collect finalized objects
        }

        // 快捷设置ygg api
        private void YggLittleskin_Click(object sender, RoutedEventArgs e)
        {
            txb_ygg_api.Text = "https://littleskin.cn/api/yggdrasil";
        }

        private void YggDocs_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.mslmc.cn/docs/advanced/yggdrasil/");
        }

        private void YggMSL_Click(object sender, RoutedEventArgs e)
        {
            txb_ygg_api.Text = "https://skin.mslmc.net/api/yggdrasil";
        }
    }
}
