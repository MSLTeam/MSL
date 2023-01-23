using HandyControl.Controls;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using MSL.controls;
using MSL.pages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static MSL.DownloadWindow;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Window = System.Windows.Window;

namespace MSL
{
    /// <summary>
    /// CreateServer.xaml 的交互逻辑
    /// </summary>
    public partial class CreateServer : Window
    {
        string DownjavaName;
        //public static string autoupdateserver="&";
        //bool safeClose = false;
        //public static Process CmdProcess = new Process();
        DispatcherTimer timer1 = new DispatcherTimer();
        public CreateServer()
        {
            timer1.Tick += new EventHandler(timer1_Tick);
            InitializeComponent();
        }
        public static void MoveFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                //获得源文件下所有文件
                List<string> files = new List<string>(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //覆盖模式
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(c, destFile);
                });
                //获得源文件下所有目录文件
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //Directory.Move必须要在同一个根目录下移动才有效，不能在不同卷中移动。
                    //Directory.Move(c, destDir);

                    //采用递归的方法实现
                    MoveFolder(c, destDir);
                });
                Directory.Delete(sourcePath);
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }
        private void next3_Click(object sender, RoutedEventArgs e)
        {
            if (useJVself.IsChecked == true)
            {
                MainWindow.serverjava = txjava.Text;
                //MainWindow.serverserver = txb3.Text;
                next3.IsEnabled = true;
                return1.IsEnabled = true;
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                label3.Visibility = Visibility.Visible;
                downloadjava.Visibility = Visibility.Visible;
                selectjava.Visibility = Visibility.Visible;
                return2.Visibility = Visibility.Visible;
                if (isImportPack)
                {
                    Growl.Info("整合包中通常会附带一个服务端核心，若您想要使用整合包中的服务端，请点击第二个按钮进行手动选择");
                }
                return;
            }
            if (usejvPath.IsChecked == true)
            {
                MainWindow.serverjava = "Java";
                //MainWindow.serverserver = txb3.Text;
                next3.IsEnabled = true;
                return1.IsEnabled = true;
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                label3.Visibility = Visibility.Visible;
                downloadjava.Visibility = Visibility.Visible;
                selectjava.Visibility = Visibility.Visible;
                return2.Visibility = Visibility.Visible;
                if (isImportPack)
                {
                    Growl.Info("整合包中通常会附带一个服务端核心，若您想要使用整合包中的服务端，请点击第二个按钮进行手动选择");
                }
                return;
            }
            try
            {
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                //version
                byte[] pageData1 = MyWebClient.DownloadData(MainWindow.serverLink + @"/web/otherdownload.txt");
                string nv1 = Encoding.UTF8.GetString(pageData1);
                //frpc
                int IndexofA1 = nv1.IndexOf("* ");
                string Ru1 = nv1.Substring(IndexofA1 + 2);
                string nv2 = nv1.Replace("* " + Ru1.Substring(0, Ru1.IndexOf(" *")) + " *", "");
                //jv83 *已删除
                //jv86
                int IndexofA3 = nv2.IndexOf("* ");
                string Ru3 = nv2.Substring(IndexofA3 + 2);
                string _dnjv86 = Ru3.Substring(0, Ru3.IndexOf(" *"));
                string nv4 = nv2.Replace("* " + _dnjv86 + " *", "");
                //jv16
                int IndexofA4 = nv4.IndexOf("* ");
                string Ru4 = nv4.Substring(IndexofA4 + 2);
                string _dnjv16 = Ru4.Substring(0, Ru4.IndexOf(" *"));
                string nv5 = nv4.Replace("* " + _dnjv16 + " *", "");
                //jv17
                int IndexofA5 = nv5.IndexOf("* ");
                string Ru5 = nv5.Substring(IndexofA5 + 2);
                string _dnjv17 = Ru5.Substring(0, Ru5.IndexOf(" *"));
                string nv6 = nv5.Replace("* " + _dnjv17 + " *", "");
                //jv18
                int IndexofA6 = nv6.IndexOf("* ");
                string Ru6 = nv6.Substring(IndexofA6 + 2);
                string _dnjv18 = Ru6.Substring(0, Ru6.IndexOf(" *"));
                next3.IsEnabled = false;
                return1.IsEnabled = false;
                if (usedownloadjv.IsChecked == true)
                {
                    if (selectJavaComb.SelectedIndex == 0)
                    {
                        DownloadJava("Java8", _dnjv86);
                    }
                    if (selectJavaComb.SelectedIndex == 1)
                    {
                        DownloadJava("Java16", _dnjv16);
                    }
                    if (selectJavaComb.SelectedIndex == 2)
                    {
                        DownloadJava("Java17", _dnjv17);
                    }
                    if (selectJavaComb.SelectedIndex == 3)
                    {
                        DownloadJava("Java18", _dnjv18);
                    }
                }
            }
            catch
            {
                MessageDialogShow.Show("出现错误！请检查您的网络连接！", "信息", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                messageDialog.Owner = this;
                messageDialog.ShowDialog();
            }
        }

        private void DownloadJava(string fileName, string downUrl)
        {
            //MessageBox.Show("下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "INFO", MessageBoxButton.OK, MessageBoxImage.Information);
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe"))
            {
                MessageDialogShow.Show("下载Java即代表您接受Java的服务条款https://www.oracle.com/downloads/licenses/javase-license1.html", "信息", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                messageDialog.Owner = this;
                messageDialog.ShowDialog();
                DownjavaName = fileName;
                //DownloadWindow.downloadurl = RserverLink +@"/web/Java8.exe";
                downloadurl = downUrl;
                downloadPath = AppDomain.CurrentDomain.BaseDirectory + "MSL";
                filename = "Java.zip";
                downloadinfo = "下载" + fileName + "中……";
                Window window = new DownloadWindow();
                window.Owner = this;
                window.ShowDialog();
                outlog.Content = "解压中...";
                try
                {
                    string javaDirName = "";
                    using (ZipFile zip = new ZipFile(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip"))
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
                    fastZip.ExtractZip(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip", AppDomain.CurrentDomain.BaseDirectory + "MSL", "");
                    outlog.Content = "解压完成，移动中...";
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Java.zip");
                    timer1.Interval = TimeSpan.FromSeconds(3);
                    timer1.Start();
                    if (AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName != AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName)
                    {
                        MoveFolder(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + javaDirName, AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName);
                    }
                }
                catch
                {
                    MessageBox.Show("安装失败，请查看是否有杀毒软件进行拦截！请确保添加信任或关闭杀毒软件后进行重新安装！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    outlog.Content = "安装失败！";
                    next3.IsEnabled = true;
                    return1.IsEnabled = true;
                }
                /*
                Form4 fw = new Form4();
                fw.ShowDialog();*/
            }
            else
            {
                MainWindow.serverjava = AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + fileName + @"\bin\java.exe";
                next3.IsEnabled = true;
                return1.IsEnabled = true;
                javagrid.Visibility = Visibility.Hidden;
                servergrid.Visibility = Visibility.Visible;
                label3.Visibility = Visibility.Visible;
                downloadjava.Visibility = Visibility.Visible;
                selectjava.Visibility = Visibility.Visible;
                return2.Visibility = Visibility.Visible;
                if (isImportPack)
                {
                    Growl.Info("整合包中通常会附带一个服务端核心，若您想要使用整合包中的服务端，请点击第二个按钮进行手动选择");
                }
            }
            
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe"))
            {
                try
                {
                    outlog.Content = "完成";
                    MainWindow.serverjava = AppDomain.CurrentDomain.BaseDirectory + @"MSL\" + DownjavaName + @"\bin\java.exe";
                    next3.IsEnabled = true;
                    return1.IsEnabled = true;
                    javagrid.Visibility = Visibility.Hidden;
                    servergrid.Visibility = Visibility.Visible;
                    label3.Visibility = Visibility.Visible;
                    downloadjava.Visibility = Visibility.Visible;
                    selectjava.Visibility = Visibility.Visible;
                    return2.Visibility = Visibility.Visible;
                    if (isImportPack)
                    {
                        Growl.Info("整合包中通常会附带一个服务端核心，若您想要使用整合包中的服务端，请点击第二个按钮进行手动选择");
                    }
                    timer1.Stop();
                }
                catch
                {
                    return;
                }
            }
        }

        private void next4_Click(object sender, RoutedEventArgs e)
        {
            if (new Regex("[\u4E00-\u9FA5]").IsMatch(txb6.Text))
            {
                var result = MessageBox.Show("使用带中文的路径可能会出现无法开服的致命bug，您确定要继续吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            else if(txb6.Text.IndexOf(" ")+1!=0)
            {
                var result = MessageBox.Show("使用带空格的路径可能会出现无法开服的致命bug，您确定要继续吗？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            //safeClose = true;
            if (System.IO.Path.IsPathRooted(txb6.Text))
            {
                MainWindow.serverbase = txb6.Text;
                if (!Directory.Exists(MainWindow.serverbase))
                {
                    Directory.CreateDirectory(MainWindow.serverbase);
                }
            }
            else
            {
                txb6.Text = AppDomain.CurrentDomain.BaseDirectory + txb6.Text;
                MainWindow.serverbase = txb6.Text;
                if (!Directory.Exists(MainWindow.serverbase))
                {
                    Directory.CreateDirectory(MainWindow.serverbase);
                }
            }
            sserver.IsSelected = true;
            sserver.IsEnabled = true;
            sserverbase.IsEnabled = false;
        }

        private void usedefault_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
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

        private void done_Click(object sender, RoutedEventArgs e)
        {
            if (usedefault.IsChecked == true)
            {
                MainWindow.serverJVM = "";
            }
            else
            {
                MainWindow.serverJVM = "-Xms" + txb4.Text + "M -Xmx" + txb5.Text + "M";
            }
            MainWindow.serverJVMcmd +=txb7.Text;
            try
            {
                Directory.CreateDirectory(MainWindow.serverbase);
                string text = "";
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini"))
                {
                    text = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                }
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini", text + "*|-n " + MainWindow.servername + "|-j " + MainWindow.serverjava + "|-s " + MainWindow.serverserver + "|-a " + MainWindow.serverJVM + "|-b " + MainWindow.serverbase + "|-c " + MainWindow.serverJVMcmd + "|*\n");
                MessageBox.Show("创建完毕，请点击“开启服务器”按钮以开服", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                MainWindow.serverJVMcmd = "";
                MainWindow.serverserver = "";
                MainWindow.serverJVM = "";
                MainWindow.serverbase = "";
                MainWindow.serverjava = "";
                MainWindow.servername = "";
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("出现错误，请重试：" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void return3_Click(object sender, RoutedEventArgs e)
        {
            sserver.IsSelected = true;
            sserver.IsEnabled = true;
            sJVM.IsEnabled = false;
            label3.Visibility = Visibility.Visible;
            downloadjava.Visibility = Visibility.Visible;
            selectjava.Visibility = Visibility.Visible;
            label5.Visibility = Visibility.Hidden;
            usedownloadjv.Visibility = Visibility.Hidden;
            selectJavaComb.Visibility = Visibility.Hidden;
            outlog.Visibility = Visibility.Hidden;
            jvhelp.Visibility = Visibility.Hidden;
            label4.Visibility = Visibility.Hidden;
            usejvPath.Visibility = Visibility.Hidden;
            useJVself.Visibility = Visibility.Hidden;
            txjava.Visibility = Visibility.Hidden;
            a0002_Copy.Visibility = Visibility.Hidden;
            next3.Visibility = Visibility.Hidden;
        }

        private void a0002_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "MSL";
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
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txb6.Text = dialog.SelectedPath;
            }
        }

        private void return4_Click(object sender, RoutedEventArgs e)
        {
            welcome.IsSelected = true;
            welcome.IsEnabled = true;
            sserverbase.IsEnabled = false;
        }
        private void return5_Click(object sender, RoutedEventArgs e)
        {
            sserverbase.IsSelected = true;
            sserverbase.IsEnabled = true;
            sserver.IsEnabled = false;
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

        private void useJVself_Checked(object sender, RoutedEventArgs e)
        {
            txjava.IsEnabled = true;
        }

        private void next_Click(object sender, RoutedEventArgs e)
        {
            sserverbase.IsSelected = true;
            sserverbase.IsEnabled = true;
            welcome.IsEnabled = false;
            MainWindow.servername = serverNameBox.Text;
            for (int a = 1; a != 0; a++)
            {
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server"))
                {
                    //MessageBox.Show(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server");
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server";
                    return;
                }
                else if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString()))
                {
                    //MessageBox.Show(AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString());
                    txb6.Text = AppDomain.CurrentDomain.BaseDirectory + @"MSL\Server" + a.ToString();
                    return;
                }
            }


        }

        bool isImportPack = false;
        private void importPack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.servername = this.serverNameBox.Text;
            string serverPath = "";
            for (int a = 1; a != 0; a++)
            {
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server"))
                {
                    serverPath = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server";
                    break;
                }
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server" + a.ToString()))
                {
                    serverPath = AppDomain.CurrentDomain.BaseDirectory + "MSL\\Server" + a.ToString();
                    break;
                }
            }
            string zipPath = "";
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "MSL";
            openfile.Title = "请选择整合包压缩文件";
            openfile.Filter = "ZIP文件|*.zip|所有文件类型|*.*";
            var res = openfile.ShowDialog();
            if (res == true)
            {
                zipPath = openfile.FileName;
            }
            new FastZip().ExtractZip(zipPath, serverPath, "");
            DirectoryInfo[] dirs = new DirectoryInfo(serverPath).GetDirectories();
            if (dirs.Length == 1)
            {
                MoveFolder(dirs[0].FullName, serverPath);
            }
            isImportPack = true;
            MainWindow.serverbase = serverPath;
            Growl.Info("整合包解压完成，且地址已自动选择，接下来您只需按照正常步骤进行操作即可");
            Growl.Info("整合包中通常不附带Java环境，故此处建议您选择第一个按钮进行下载");
            sserver.IsSelected = true;
            sserver.IsEnabled = true;
            welcome.IsEnabled = false;
            return5.IsEnabled = false;
        }

        private void usejvPath_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                txjava.IsEnabled = false;
            }
        }

        private void usejv8_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                next3.Visibility = Visibility.Visible;
                txjava.IsEnabled = false;
            }
        }

        private void usejv16_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                next3.Visibility = Visibility.Visible;
                txjava.IsEnabled = false;
            }
        }

        private void usejv17_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                next3.Visibility = Visibility.Visible;
                txjava.IsEnabled = false;
            }
        }

        private void usejv18_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                next3.Visibility = Visibility.Visible;
                txjava.IsEnabled = false;
            }
        }


        private void downloadserver_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.serverJVMcmd = "";
            DownloadServer downloadServer = new DownloadServer();
            downloadServer.Owner = this;
            downloadServer.ShowDialog();
            if (File.Exists(MainWindow.serverbase+@"\"+ MainWindow.serverserver))
            {
                txb3.Text = MainWindow.serverbase + @"\" + MainWindow.serverserver;
                sJVM.IsSelected = true;
                sJVM.IsEnabled = true;
                sserver.IsEnabled = false;
                next3.IsEnabled = true;
                return1.IsEnabled = true;
            }
            else if(MainWindow.serverJVMcmd!="")
            {
                txb3.Text = MainWindow.serverJVMcmd;
                sJVM.IsSelected = true;
                sJVM.IsEnabled = true;
                sserver.IsEnabled = false;
                next3.IsEnabled = true;
                return1.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("下载失败！");
            }
        }

        private void selectserver_Click(object sender, RoutedEventArgs e)
        {
            servertips.Visibility = Visibility.Hidden;
            downloadserver.Visibility = Visibility.Hidden;
            selectserver.Visibility = Visibility.Hidden;
            label1.Visibility = Visibility.Hidden;
            return2.Visibility = Visibility.Visible;
            txb3.Visibility = Visibility.Visible;
            next2.Visibility = Visibility.Visible;
            a0002.Visibility = Visibility.Visible;
            label2.Visibility = Visibility.Visible;
        }

        private void next2_Click(object sender, RoutedEventArgs e)
        {
            string _filename = Path.GetFileName(txb3.Text);
            if (Path.GetDirectoryName(txb3.Text) != MainWindow.serverbase)
            {
                File.Copy(txb3.Text, MainWindow.serverbase + @"\" + _filename);
                MessageDialogShow.Show("已将服务端文件移至服务器文件夹中！您可将源文件删除！", "提示", false, "", "确定");
                MessageDialog messageDialog = new MessageDialog();
                messageDialog.Owner = this;
                messageDialog.ShowDialog();
                txb3.Text = MainWindow.serverbase + @"\" + _filename;
            }
            MainWindow.serverserver = _filename;
            sJVM.IsSelected = true;
            sJVM.IsEnabled = true;
            sserver.IsEnabled = false;
            next3.IsEnabled = true;
            return1.IsEnabled = true;
        }

        private void selectjava_Click(object sender, RoutedEventArgs e)
        {
            tipsjv1.Visibility = Visibility.Hidden;
            label4.Visibility = Visibility.Visible;
            usejvPath.Visibility = Visibility.Visible;
            usejvPath.IsChecked = true;
            useJVself.Visibility = Visibility.Visible;
            txjava.Visibility = Visibility.Visible;
            a0002_Copy.Visibility = Visibility.Visible;
            next3.Visibility = Visibility.Visible;
            return1.Visibility = Visibility.Visible;
            return5.Visibility = Visibility.Hidden;
            label3.Visibility = Visibility.Hidden;
            downloadjava.Visibility = Visibility.Hidden;
            selectjava.Visibility = Visibility.Hidden;
        }

        private void downloadjava_Click(object sender, RoutedEventArgs e)
        {
            tipsjv1.Visibility = Visibility.Hidden;
            label5.Visibility = Visibility.Visible;
            usedownloadjv.Visibility = Visibility.Visible;
            selectJavaComb.Visibility = Visibility.Visible;
            usedownloadjv.IsChecked = true;
            selectJavaComb.SelectedIndex = 0;
            outlog.Visibility = Visibility.Visible;
            jvhelp.Visibility = Visibility.Visible;
            next3.Visibility = Visibility.Visible;
            return1.Visibility = Visibility.Visible;
            return5.Visibility = Visibility.Hidden;
            label3.Visibility = Visibility.Hidden;
            downloadjava.Visibility = Visibility.Hidden;
            selectjava.Visibility = Visibility.Hidden;
        }

        private void return1_Click(object sender, RoutedEventArgs e)
        {
            servertips.Visibility = Visibility.Visible;
            tipsjv1.Visibility = Visibility.Visible;
            downloadserver.Visibility = Visibility.Visible;
            selectserver.Visibility = Visibility.Visible;
            label1.Visibility = Visibility.Visible;
            javagrid.Visibility = Visibility.Visible;
            label3.Visibility = Visibility.Visible;
            downloadjava.Visibility = Visibility.Visible;
            selectjava.Visibility = Visibility.Visible;
            return1.Visibility = Visibility.Visible;
            return5.Visibility = Visibility.Visible;
            servergrid.Visibility = Visibility.Hidden;
            label5.Visibility = Visibility.Hidden;
            usedownloadjv.Visibility = Visibility.Hidden;
            selectJavaComb.Visibility = Visibility.Hidden;
            outlog.Visibility = Visibility.Hidden;
            jvhelp.Visibility = Visibility.Hidden;
            label4.Visibility = Visibility.Hidden;
            return1.Visibility = Visibility.Hidden;
            return2.Visibility = Visibility.Hidden;
            usejvPath.Visibility = Visibility.Hidden;
            useJVself.Visibility = Visibility.Hidden;
            txjava.Visibility = Visibility.Hidden;
            a0002_Copy.Visibility = Visibility.Hidden;
            next3.Visibility = Visibility.Hidden;
            next2.Visibility = Visibility.Hidden;
            txb3.Visibility = Visibility.Hidden;
            next2.Visibility = Visibility.Hidden;
            a0002.Visibility = Visibility.Hidden;
            label2.Visibility = Visibility.Hidden;
        }

        private void skip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.Create(AppDomain.CurrentDomain.BaseDirectory + @"MSL\ServerList.ini");
                Close();
            }
            catch
            {
                MessageBox.Show("出现错误，请重试" + "c0x1", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void usebasicfastJvm_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("使用优化参数需要手动设置大小相同的内存，请对上面的内存进行更改！", "警告", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            useJVM.IsChecked = true;
            usefastJvm.IsChecked = false;
            txb7.Text = "-XX:+AggressiveOpts";
            txb4.Text = "2048";
            txb5.Text = "2048";
        }
        private void usefastJvm_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("使用优化参数需要手动设置大小相同的内存，请对上面的内存进行更改！", "警告", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            useJVM.IsChecked = true;
            usebasicfastJvm.IsChecked = false;
            txb7.Text = "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 -XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC -XX:+AlwaysPreTouch -XX:G1NewSizePercent=30 -XX:G1MaxNewSizePercent=40 -XX:G1HeapRegionSize=8M -XX:G1ReservePercent=20 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 -XX:InitiatingHeapOccupancyPercent=15 -XX:G1MixedGCLiveThresholdPercent=90 -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 -Dusing.aikars.flags=https://mcflags.emc.gs -Daikars.new.flags=true";
            txb4.Text = "4096";
            txb5.Text = "4096";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindow.serverJVMcmd = "";
            MainWindow.serverserver = "";
            MainWindow.serverJVM = "";
            MainWindow.serverbase = "";
            MainWindow.serverjava = "";
            MainWindow.servername = "";
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Growl.SetGrowlParent(GrowlPanel, false);
        }
    }
}
