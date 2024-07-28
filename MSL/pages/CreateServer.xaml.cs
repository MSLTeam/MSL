using HandyControl.Controls;
using ICSharpCode.SharpZipLib.Zip;
using MSL.controls;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
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
        private string DownjavaName;
        private string servername;
        private string serverjava;
        private string serverbase;
        private string servercore;
        private string servermemory;
        private string serverargs;

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

        private void FastModeBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Hidden;
            FastModeGrid.Visibility = Visibility.Visible;
            Thread thread = new Thread(FastModeGetCore);
            thread.Start();
        }

        private void CustomModeBtn_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Hidden;
            tabCtrl.Visibility = Visibility.Visible;
        }

        private bool isImportPack = false;
        private async void ImportPack_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImportPack.SelectedIndex == 1)
            {
                ImportPack.SelectedIndex = 0;
                DownloadMods downloadMods = new DownloadMods(1)
                {
                    Owner = Window.GetWindow(Window.GetWindow(this))
                };
                downloadMods.ShowDialog();
                if (!File.Exists("MSL\\ServerPack.zip"))
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "下载失败！", "错误");
                    return;
                }
                string input = await Shows.ShowInput(Window.GetWindow(this), "服务器名称：", "MyServer");
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
                        await Task.Run(() => new FastZip().ExtractZip("MSL\\ServerPack.zip", serverPath, ""));
                        DirectoryInfo[] dirs = new DirectoryInfo(serverPath).GetDirectories();
                        if (dirs.Length == 1)
                        {
                            Functions.MoveFolder(dirs[0].FullName, serverPath);
                        }
                        File.Delete("MSL\\ServerPack.zip");
                    }
                    catch (Exception ex)
                    {
                        Window.GetWindow(this).Focus();
                        waitDialog.Close();
                        Shows.ShowMsgDialog(Window.GetWindow(this), "整合包解压失败！请确认您的整合包是.zip格式！\n错误代码：" + ex.Message, "错误");
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
                    }
                    else
                    {
                        Growl.Error("出现错误，获取Java版本列表失败！");
                    }

                    Growl.Info("整合包解压完成！请在此界面选择Java环境，Java的版本要和导入整合包的版本相对应，详情查看界面下方的表格");
                    sjava.IsSelected = true;
                    sjava.IsEnabled = true;
                    welcome.IsEnabled = false;
                }
            }
            else if (ImportPack.SelectedIndex == 2)
            {
                ImportPack.SelectedIndex = 0;
                bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "目前仅支持导入.zip格式的整合包文件，如果您要导入的是模组整合包，请确保您下载的整合包是服务器专用包（如RLCraft下载界面就有一个ServerPack的压缩包），否则可能会出现无法开服或者崩溃的问题！", "提示", true, "取消");
                if (dialog == true)
                {
                    string input = await Shows.ShowInput(Window.GetWindow(this), "服务器名称：", "MyServer");
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
                            ShowDialogs showDialogs = new ShowDialogs();
                            //Dialog waitDialog = null;
                            try
                            {
                                showDialogs.ShowTextDialog(Window.GetWindow(this), "解压整合包中，请稍等……");
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
                                showDialogs.CloseTextDialog();
                                Shows.ShowMsgDialog(Window.GetWindow(this), "整合包解压失败！请确认您的整合包是.zip格式！\n错误代码：" + ex.Message, "错误");
                                return;
                            }
                            showDialogs.CloseTextDialog();
                            MainGrid.Visibility = Visibility.Hidden;
                            tabCtrl.Visibility = Visibility.Visible;
                            isImportPack = true;
                            serverbase = serverPath;

                            List<string> strings = await AsyncGetJavaVersion();
                            if (strings != null)
                            {
                                selectJavaComb.ItemsSource = strings.ToList();
                            }
                            else
                            {
                                Growl.Error("出现错误，获取Java版本列表失败！");
                            }

                            Growl.Info("整合包解压完成！请在此界面选择Java环境，Java的版本要和导入整合包的版本相对应，详情查看界面下方的表格");
                            sjava.IsSelected = true;
                            sjava.IsEnabled = true;
                            welcome.IsEnabled = false;
                        }
                    }
                }
            }
        }

        private async void next3_Click(object sender, RoutedEventArgs e)
        {
            bool noNext = false;
            next3.IsEnabled = false;
            return5.IsEnabled = false;
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
                    Shows.ShowMsgDialog(Window.GetWindow(this), "检测Java可用性失败，您的Java似乎不可用！请检查是否选择正确！", "错误");
                    usedownloadjv.IsChecked = true;
                    noNext = true;
                }
                serverjava = txjava.Text;
                await Dispatcher.InvokeAsync(() =>
                {
                    CheckServerPackCore();
                });
            }
            else if (usejvPath.IsChecked == true)
            {
                serverjava = "Java";
                await Dispatcher.InvokeAsync(() =>
                {
                    CheckServerPackCore();
                });
            }
            else if (usecheckedjv.IsChecked == true)
            {
                string a = selectCheckedJavaComb.Items[selectCheckedJavaComb.SelectedIndex].ToString();
                serverjava = a.Substring(a.IndexOf(":") + 2);
                await Dispatcher.InvokeAsync(() =>
                {
                    CheckServerPackCore();
                });
            }
            else if (usedownloadjv.IsChecked == true)
            {
                try
                {
                    int dwnJava = 0;
                    await Dispatcher.Invoke(async () =>
                    {
                        dwnJava = await DownloadJava(selectJavaComb.SelectedItem.ToString(), HttpService.Get("download/java/" + selectJavaComb.SelectedItem.ToString()));
                    });
                    if (dwnJava == 1)
                    {
                        ShowDialogs showDialogs = new ShowDialogs();
                        showDialogs.ShowTextDialog(Window.GetWindow(this), "解压Java中……");
                        bool unzipJava = await UnzipJava();
                        showDialogs.CloseTextDialog();
                        if (unzipJava)
                        {
                            await Dispatcher.InvokeAsync(() =>
                            {
                                CheckServerPackCore();
                            });
                        }
                        else
                        {
                            noNext = true;
                        }
                    }
                    else if (dwnJava == 2)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            CheckServerPackCore();
                        });
                    }
                    else
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "下载取消！", "提示");
                        noNext = true;
                    }
                }
                catch
                {
                    Growl.Error("出现错误，请检查网络连接！");
                    noNext = true;
                }
            }
            next3.IsEnabled = true;
            return5.IsEnabled = true;
            if (!noNext && !isImportPack)
            {
                sserver.IsSelected = true;
                sserver.IsEnabled = true;
                sjava.IsEnabled = false;
            }
        }

        private async void CheckServerPackCore()
        {
            if (isImportPack)
            {
                sserver.IsSelected = true;
                sserver.IsEnabled = true;
                sjava.IsEnabled = false;
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
                    string selectFile = await Shows.ShowInput(Window.GetWindow(this), "开服器在整合包中检测到了以下jar文件，你可输选择一个作为开服核心（输入文件前对应的数字，取消为不选择以下文件）\n" + filestr);
                    if (selectFile != null)
                    {
                        txb3.Text = files[int.Parse(selectFile)];
                        if (Functions.CheckForgeInstaller(serverbase + "\\" + txb3.Text))
                        {
                            bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您选择的服务端疑似是forge安装器，是否将其展开安装？\n如果不展开安装，服务器可能无法开启！", "提示", true, "取消");
                            if (dialog)
                            {
                                string installReturn = await InstallForge(txb3.Text);
                                if (installReturn == null)
                                {
                                    Shows.ShowMsgDialog(Window.GetWindow(this), "下载失败！", "错误");
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
                    bool ret = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "开服器在整合包中检测到了jar文件" + files[0] + "，是否选择此文件为开服核心？", "提示", true, "取消");
                    if (ret)
                    {
                        txb3.Text = files[0];
                        if (Functions.CheckForgeInstaller(serverbase + "\\" + txb3.Text))
                        {
                            bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您选择的服务端疑似是forge安装器，是否将其展开安装？\n如果不展开安装，服务器可能无法开启！", "提示", true, "取消");
                            if (dialog)
                            {
                                string installReturn = await InstallForge(txb3.Text);
                                if (installReturn == null)
                                {
                                    Shows.ShowMsgDialog(Window.GetWindow(this), "下载失败！", "错误");
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
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "下载Java即代表您接受Java的服务条款：\nhttps://www.oracle.com/downloads/licenses/javase-license1.html", "信息");
                DownjavaName = fileName;

                bool downDialog = await Shows.ShowDownloader(Window.GetWindow(this), downUrl, "MSL", "Java.zip", "下载" + fileName + "中……");
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
                Shows.ShowMsgDialog(Window.GetWindow(this), "解压失败，Java压缩包可能已损坏，请重试！错误代码：" + ex.Message + "\n（注：若多次重试均无法解压的话，请自行去网络上下载安装并使用自定义模式来创建服务器）", "错误");
                return false;
            }
        }
        private void return2_Click(object sender, RoutedEventArgs e)
        {
            sjava.IsSelected = true;
            sjava.IsEnabled = true;
            sserver.IsEnabled = false;
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

        private void return3_Click(object sender, RoutedEventArgs e)
        {
            sserver.IsSelected = true;
            sserver.IsEnabled = true;
            sJVM.IsEnabled = false;
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

        private void return5_Click(object sender, RoutedEventArgs e)
        {
            if (isImportPack)
            {
                isImportPack = false;
                MainGrid.Visibility = Visibility.Visible;
                tabCtrl.Visibility = Visibility.Hidden;
                welcome.IsSelected = true;
                welcome.IsEnabled = true;
                sjava.IsEnabled = false;
                return;
            }
            welcome.IsSelected = true;
            welcome.IsEnabled = true;
            sjava.IsEnabled = false;
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
        private async void usecheckedjv_Checked(object sender, RoutedEventArgs e)
        {
            List<JavaScanner.JavaInfo> strings = null;
            int dialog = Shows.ShowMsg(Window.GetWindow(Window.GetWindow(this)), "即将开始检测电脑上的Java，此过程可能需要一些时间，请耐心等待。\n目前有两种检测模式，一种是简单检测，只检测一些关键目录，用时较少，普通用户可优先使用此模式。\n第二种是深度检测，将检测所有磁盘的所有目录，耗时可能会很久，请慎重选择！", "提示", true, "开始深度检测", "开始简单检测");
            if (dialog == 2)
            {
                usedownloadjv.IsChecked = true;
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
                selectCheckedJavaComb.ItemsSource = strings.Select(info => $"Java{info.Version}: {info.Path}").ToList();
                /*
                foreach (JavaScanner.JavaInfo info in strings)
                {
                    selectCheckedJavaComb.Items.Add(info.Version + ":" + info.Path);
                }
                */
                //selectCheckedJavaComb.ItemsSource = strings.ToList();
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
                Shows.ShowMsgDialog(Window.GetWindow(this), "检测环境变量失败，您的环境变量似乎不存在！", "错误");
                usedownloadjv.IsChecked = true;
            }
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
            }
        }
        private void useServerself_Checked(object sender, RoutedEventArgs e)
        {
            txb3.IsEnabled = true;
            a0002.IsEnabled = true;
        }

        private async void next_Click(object sender, RoutedEventArgs e)
        {
            servername = serverNameBox.Text;
            if (new Regex("[\u4E00-\u9FA5]").IsMatch(txb6.Text))
            {
                bool result = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "使用带有中文的路径可能造成编码错误，导致无法开服，您确定要继续吗？", "警告", true, "取消");
                if (result == false)
                {
                    return;
                }
            }
            else if (txb6.Text.IndexOf(" ") + 1 != 0)
            {
                bool result = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "使用带有空格的路径可能造成编码错误，导致无法开服，您确定要继续吗？", "警告", true, "取消");
                if (result == false)
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
            }
            else
            {
                Growl.Error("出现错误，获取Java版本列表失败！");
            }

            sjava.IsSelected = true;
            sjava.IsEnabled = true;
            welcome.IsEnabled = false;
        }

        private async void next2_Click(object sender, RoutedEventArgs e)
        {
            if (usedownloadserver.IsChecked == true)
            {
                DownloadServer downloadServer = new DownloadServer(serverbase, serverjava)
                {
                    Owner = Window.GetWindow(Window.GetWindow(this))
                };
                downloadServer.ShowDialog();
                if (File.Exists(serverbase + "\\" + downloadServer.downloadServerName))
                {
                    servercore = downloadServer.downloadServerName;
                    sJVM.IsSelected = true;
                    sJVM.IsEnabled = true;
                    sserver.IsEnabled = false;
                }
                else if (downloadServer.downloadServerName.StartsWith("@libraries/"))
                {
                    servercore = downloadServer.downloadServerName;
                    sJVM.IsSelected = true;
                    sJVM.IsEnabled = true;
                    sserver.IsEnabled = false;
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(serverbase);
                    string _filename = Path.GetFileName(txb3.Text);
                    if (File.Exists(serverbase + "\\" + _filename))
                    {
                        txb3.Text = _filename;
                    }
                    else
                    {
                        if (Path.GetDirectoryName(txb3.Text) != serverbase)
                        {
                            if (await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "所选的服务端核心文件并不在服务器目录中，是否将其复制进服务器目录？\n若不复制，请留意勿将核心文件删除！", "提示", true))
                            {
                                File.Copy(txb3.Text, serverbase + "\\" + _filename, true);
                                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "已将服务端核心复制到了服务器目录之中，您现在可以将源文件删除了！", "提示");
                                txb3.Text = _filename;
                            }
                        }
                        else if (!Path.IsPathRooted(txb3.Text) && File.Exists(AppDomain.CurrentDomain.BaseDirectory + txb3.Text))
                        {
                            txb3.Text = AppDomain.CurrentDomain.BaseDirectory + txb3.Text;
                        }
                    }
                    string fullFileName;
                    if (File.Exists(serverbase + "\\" + txb3.Text))
                    {
                        fullFileName = serverbase + "\\" + txb3.Text;
                    }
                    else
                    {
                        fullFileName = txb3.Text;
                    }
                    if (Functions.CheckForgeInstaller(fullFileName))
                    {
                        bool dialog = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "您选择的服务端疑似是forge安装器，是否将其展开安装？\n如果不展开安装，服务器可能无法开启！", "提示", true, "取消");
                        if (dialog)
                        {
                            string installReturn = await InstallForge(txb3.Text);
                            if (installReturn == null)
                            {
                                Shows.ShowMsgDialog(Window.GetWindow(this), "下载失败！", "错误");
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
                catch (Exception ex)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), ex.Message, "错误");
                }
            }
        }

        private async void usebasicfastJvm_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)usebasicfastJvm.IsChecked)
            {
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "使用优化参数需要手动设置大小相同的内存，请对上面的内存进行更改！Java11及以上请勿选择此优化参数！", "警告");
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
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "使用优化参数需要手动设置大小相同的内存，请对上面的内存进行更改！", "警告");
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

        private async void done_Click(object sender, RoutedEventArgs e)
        {
            if (usedefault.IsChecked == true)
            {
                servermemory = "";
            }
            else
            {
                servermemory = "-Xms" + txb4.Text + "M -Xmx" + txb5.Text + "M";
            }
            serverargs += txb7.Text;
            try
            {
                if (!Directory.Exists(serverbase))
                {
                    Directory.CreateDirectory(serverbase);
                }

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
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "创建完毕，请点击“开启服务器”按钮以开服", "信息");
                GotoServerList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("出现错误，请重试：" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Visibility = Visibility.Visible;
            tabCtrl.Visibility = Visibility.Hidden;
            FastModeGrid.Visibility = Visibility.Hidden;
        }

        //用于分类的字典
        public static Dictionary<string, List<string>> serverCoreTypes;
        string[] serverTypes;
        private void FastModeGetCore()
        {
            try
            {
                //获取分类
                var responseString = HttpService.Get("query/server_classify");
                serverCoreTypes = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(responseString);
                string jsonData = HttpService.Get("query/available_server_types");
                serverTypes = JsonConvert.DeserializeObject<string[]>(jsonData);
                Dispatcher.Invoke(() =>
                {
                    ServerCoreCombo.SelectedIndex = 0;
                });
            }
            catch (Exception a)
            {
                Growl.Info("获取服务端失败！请重试" + a.Message);
            }
        }
        List<string> typeVersions = new List<string>();
        private void ServerCoreCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FastModeNextBtn.IsEnabled = false;
            ServerVersionCombo.ItemsSource = null;
            typeVersions.Clear();
            tempServerCore.Clear();
            if (serverTypes == null)
            {
                Shows.ShowMsgDialog(Window.GetWindow(this), "服务端正在加载中，请稍后再选择！", "提示");
                return;
            }
            Thread thread = new Thread(GetServerVersion);
            thread.Start();
        }

        private void GetServerVersion()
        {
            int selectType = 0;
            Dispatcher.Invoke(() =>
            {
                ServerCoreCombo.IsEnabled = false;
                ServerCoreDescrip.Text = "加载中，请稍等……";
                selectType = ServerCoreCombo.SelectedIndex;
            });
            try
            {
                int i = 0;
                foreach (var serverType in serverTypes)
                {
                    int x = 0;
                    foreach (var coreTypes in serverCoreTypes)
                    {
                        if (x == selectType)
                        {
                            string _serverType = serverType;
                            /*
                            if (serverType.Contains("（"))
                            {
                                _serverType = serverType.Substring(0, serverType.IndexOf("（"));
                            }
                            */
                            foreach (var coreType in coreTypes.Value)
                            {
                                if (coreType == _serverType)
                                {
                                    try
                                    {
                                        var resultData = HttpService.Get("query/available_versions/" + _serverType);
                                        tempServerCore.Add(coreType, resultData);
                                        List<string> serverVersions = JsonConvert.DeserializeObject<List<string>>(resultData);
                                        foreach (var item in serverVersions)
                                        {
                                            if (!typeVersions.Contains(item) && !item.StartsWith("*"))
                                            {
                                                typeVersions.Add(item);
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            var resultData = HttpService.Get("query/available_versions/" + coreType);
                                            tempServerCore.Add(coreType, resultData);
                                            Dictionary<string, string> serverDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(resultData);
                                            foreach (var item in serverDetails.Keys)
                                            {
                                                if (!typeVersions.Contains(item) && !item.StartsWith("*"))
                                                {
                                                    typeVersions.Add(item);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                Shows.ShowMsgDialog(Window.GetWindow(this), "获取服务端失败！请重试！\n错误代码：" + ex.Message, "错误");
                                            });
                                            return;
                                        }
                                    }
                                }
                            }
                            x++;
                        }
                        else
                        {
                            x++;
                        }
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "出现错误：" + ex.Message, "err");
                    FastModeNextBtn.IsEnabled = true;
                    return;
                });
            }
            var sortedList = typeVersions.OrderByDescending(p => Functions.VersionCompare(p)).ToList();
            Dispatcher.Invoke(() =>
            {
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
            });
        }

        private readonly Dictionary<string, string> tempServerCore = new Dictionary<string, string>();
        private async void FastModeNextBtn_Click(object sender, RoutedEventArgs e)
        {
            servername = ServerNameBox.Text;
            if (new Regex("[\u4E00-\u9FA5]").IsMatch(txb6.Text))
            {
                bool result = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "开服器被放置于带有中文的目录里，中文目录可能会造成编码错误导致无法开服，您确定要继续吗？", "警告", true, "取消");
                if (result == false)
                {
                    return;
                }
            }
            else if (txb6.Text.IndexOf(" ") + 1 != 0)
            {
                bool result = await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "开服器被放置于带有空格的目录里，这种目录可能会造成编码错误导致无法开服，您确定要继续吗？", "警告", true, "取消");
                if (result == false)
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
            if (versionString != "latest")
            {
                if (versionString.Contains("-"))
                {
                    versionString = versionString.Substring(0, versionString.IndexOf("-"));
                }
                string[] components = versionString.Split('.');
                if (components.Length >= 3 && int.TryParse(components[2], out int _))
                {
                    versionString = $"{components[0]}.{components[1]}"; // remove the last component
                }

                Version _version = new Version(versionString);
                Version targetVersion1 = new Version("1.7");
                Version targetVersion2 = new Version("1.12");
                Version targetVersion3 = new Version("1.16");
                Version targetVersion4 = new Version("1.19");

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
                    //1.16< _version <=1.19
                    FinallyJavaDescrip.Text = "根据您的选择，最适合您服务器的Java版本为：Java17及以上";
                    javaVersion = "Java17";
                }
                else
                {
                    //1.19< _version
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
            FastModeGrid.Visibility = Visibility.Hidden;
            InstallGrid.Visibility = Visibility.Visible;
        }

        private async Task<List<string>> AsyncGetJavaVersion()
        {
            ShowDialogs showDialogs = new ShowDialogs();
            showDialogs.ShowTextDialog(Window.GetWindow(this), "获取Java版本列表中，请稍等……");
            await Task.Delay(200);
            try
            {
                string response = string.Empty;
                await Task.Run(() =>
                {
                    response = HttpService.Get("query/java");
                });
                await Task.Delay(200);
                JArray jArray = JArray.Parse(response);
                List<string> strings = new List<string>();
                foreach (var j in jArray)
                {
                    strings.Add(j.ToString());
                }
                showDialogs.CloseTextDialog();
                return strings;
            }
            catch
            {
                showDialogs.CloseTextDialog();
                return null;
            }
        }

        private async void FastModeInstallBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FastModeReturnBtn.IsEnabled = false;
                FastModeInstallBtn.IsEnabled = false;
                FastInstallProcess.Text = "当前进度:下载Java……";
                int dwnJava = 0;
                await Dispatcher.Invoke(async () =>
                {
                    dwnJava = await DownloadJava(FinallyJavaCombo.SelectedItem.ToString(), (await HttpService.GetApiContentAsync("download/java/" + FinallyJavaCombo.SelectedItem.ToString()))["data"]["url"].ToString());
                });
                if (dwnJava == 1)
                {
                    FastInstallProcess.Text = "当前进度:解压Java……";
                    ShowDialogs showDialogs = new ShowDialogs();
                    showDialogs.ShowTextDialog(Window.GetWindow(this), "解压Java中……");
                    bool unzipJava = await UnzipJava();
                    showDialogs.CloseTextDialog();
                    if (unzipJava)
                    {
                        FastInstallProcess.Text = "当前进度:下载服务端……";
                        await Dispatcher.InvokeAsync(() =>
                        {
                            FastModeInstallCore();
                        });
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
                    await Dispatcher.InvokeAsync(() =>
                    {
                        FastModeInstallCore();
                    });
                }
                else
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "下载取消！", "提示");
                    FastInstallProcess.Text = "取消安装！";
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
            }
            catch
            {
                Growl.Error("出现错误，请检查网络连接！");
                FastModeReturnBtn.IsEnabled = true;
                FastModeInstallBtn.IsEnabled = true;
            }
        }

        private async void FastModeInstallCore()
        {
            string finallyServerCore = FinallyCoreCombo.SelectedItem.ToString();
            string serverCoreType = finallyServerCore.Substring(0, finallyServerCore.LastIndexOf("-"));
            string filename = finallyServerCore + ".jar";
            JObject dlContext = await HttpService.GetApiContentAsync("download/server/" + serverCoreType + "/" +
                finallyServerCore.Substring(finallyServerCore.LastIndexOf("-") + 1));//获取链接
            string dlUrl = dlContext["data"]["url"].ToString();
            string sha256Exp = dlContext["data"]["sha256"].ToString();
            if (serverCoreType == "forge" || serverCoreType == "spongeforge" || serverCoreType == "neoforge")
            {
                int dwnDialog = await Shows.ShowDownloaderWithIntReturn(Window.GetWindow(this), dlUrl, serverbase, filename, "下载服务端中……", sha256Exp, true); //从这里请求服务端下载
                if (dwnDialog == 2)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "下载取消！（或服务端文件不存在）", "提示");
                    FastInstallProcess.Text = "取消安装！";
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
            }
            else
            {
                bool dwnDialog = await Shows.ShowDownloader(Window.GetWindow(this), dlUrl, serverbase, filename, "下载服务端中……", sha256Exp); //从这里请求服务端下载
                if (!dwnDialog || !File.Exists(serverbase + "\\" + filename))
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "下载取消！（或服务端文件不存在）", "提示");
                    FastInstallProcess.Text = "取消安装！";
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
            }

            if (serverCoreType == "spongeforge")
            {
                string forgeName = finallyServerCore.Replace("spongeforge", "forge");
                string _filename = forgeName + ".jar";
                JObject _dlContext = await HttpService.GetApiContentAsync("download/server/" + forgeName.Replace("-", "/"));
                string _dlUrl = _dlContext["data"]["url"].ToString();
                string _sha256Exp = _dlContext["data"]["sha256"].ToString();
                int _dwnDialog = await Shows.ShowDownloaderWithIntReturn(Window.GetWindow(this), _dlUrl, serverbase, _filename, "下载服务端中……", _sha256Exp, true);

                if (_dwnDialog == 2)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "下载取消！", "提示");
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
                    bool backupDownloadSuccess = await Shows.ShowDownloader(Window.GetWindow(this), backupUrl, serverbase, _filename, "备用链接下载中……", _sha256Exp);
                    if (!backupDownloadSuccess || !File.Exists(serverbase + "\\" + _filename))
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "下载取消！（或服务端文件不存在）", "错误");
                        FastInstallProcess.Text = "取消安装！";
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                }

                string installReturn = await InstallForge(_filename);
                if (installReturn == null)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "安装失败！", "错误");
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }

                servercore = installReturn;
                Directory.CreateDirectory(serverbase + "\\mods");
                File.Move(serverbase + "\\" + filename, serverbase + "\\mods\\" + filename);
            }

            /*
            else if (finallyServerCore.Contains("banner"))
            {

            }
            */
            else if (serverCoreType == "neoforge")
            {
                if (!File.Exists(serverbase + "\\" + filename))
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "下载失败！（或服务端文件不存在）", "提示");
                    FastInstallProcess.Text = "取消安装！";
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
                string installReturn = await InstallForge(filename);
                if (installReturn == null)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "安装失败！", "错误");
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
                servercore = installReturn;
            }
            else if (serverCoreType == "forge")
            {
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
                    bool backupDownloadSuccess = await Shows.ShowDownloader(Window.GetWindow(this), backupUrl, serverbase, filename, "备用链接下载中……", sha256Exp);
                    if (!backupDownloadSuccess || !File.Exists(serverbase + "\\" + filename))
                    {
                        Shows.ShowMsgDialog(Window.GetWindow(this), "下载取消！（或服务端文件不存在）", "错误");
                        FastInstallProcess.Text = "取消安装！";
                        FastModeReturnBtn.IsEnabled = true;
                        FastModeInstallBtn.IsEnabled = true;
                        return;
                    }
                }
                string installReturn = await InstallForge(filename);
                if (installReturn == null)
                {
                    Shows.ShowMsgDialog(Window.GetWindow(this), "安装失败！", "错误");
                    FastModeReturnBtn.IsEnabled = true;
                    FastModeInstallBtn.IsEnabled = true;
                    return;
                }
                servercore = installReturn;
            }
            else
            {
                servercore = filename;
            }

            FastInstallProcess.Text = "当前进度:完成！";
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
                await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "创建完毕，请点击“开启服务器”按钮以开服", "信息");
                GotoServerList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("出现错误，请重试：" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                FastModeReturnBtn.IsEnabled = true;
                FastModeInstallBtn.IsEnabled = true;
            }
        }

        private async Task<string> InstallForge(string filename)
        {
            //调用新版forge安装器
            string[] installForge = await Shows.ShowInstallForge(Window.GetWindow(this), serverbase + "\\" + filename, serverbase, serverjava);
            if (installForge[0] == "0")
            {
                if (await Shows.ShowMsgDialogAsync(Window.GetWindow(this), "自动安装失败！是否尝试使用命令行安装方式？", "错误", true))
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

        private void FastModeReturnBtn_Click(object sender, RoutedEventArgs e)
        {
            InstallGrid.Visibility = Visibility.Hidden;
            FastModeGrid.Visibility = Visibility.Visible;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            GC.Collect();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            GotoServerList();
        }
    }
}
