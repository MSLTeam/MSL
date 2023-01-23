using Downloader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MSL
{
    /// <summary>
    /// DownloadWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadWindow : Window
    {
        //DownLoadFile dlf = new DownLoadFile();
        public static int downloadthread = 8;
        public static string downloadinfo;
        public static string downloadPath;
        public static string filename;
        public static string downloadurl;
        bool ifStop=false;
        //DispatcherTimer timer1 = new DispatcherTimer();
        //DispatcherTimer timer2 = new DispatcherTimer();
        //static Thread thread;
        public DownloadWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /*
            dlf.DownLoadThreadNum = downloadthread;//下载线程数，不设置默认为3
            dlf.doSendMsg += SendMsgHander;//下载过程处理事件
            dlf.AddDown(downloadurl, downloadPath, 0, filename);
            dlf.StartDown();*/

            //taskinfo.Content = downloadinfo;
            //infolabel.Text = downloadinfo;
            //timer1.Tick += new EventHandler(timer1_Tick);
            //timer1.Interval = TimeSpan.FromSeconds(1);
            //timer1.Start();

            /*
            thread = new Thread(Downloader);
            thread.Start();*/
            taskinfo.Content = downloadinfo;
            ifStop = false;
            Thread thread = new Thread(Downloader);
            thread.Start();
        }
        void Downloader()
        {
            var downloadOpt = new DownloadConfiguration()
            {
                ChunkCount = downloadthread, // file parts to download, default value is 1
                ParallelDownload = true // download parts of file as parallel or not. Default value is false
            };
            var downloader = new DownloadService(downloadOpt);
            // Provide `FileName` and `TotalBytesToReceive` at the start of each downloads
            downloader.DownloadStarted += OnDownloadStarted;

            // Provide any information about chunker downloads, 
            // like progress percentage per chunk, speed, 
            // total received bytes and received bytes array to live streaming.
            //downloader.ChunkDownloadProgressChanged += OnChunkDownloadProgressChanged;

            // Provide any information about download progress, 
            // like progress percentage of sum of chunks, total speed, 
            // average speed, total received bytes and received bytes array 
            // to live streaming.
            downloader.DownloadProgressChanged += OnDownloadProgressChanged;

            // Download completed event that can include occurred errors or 
            // cancelled or download completed successfully.
            downloader.DownloadFileCompleted += OnDownloadFileCompleted;
            downloader.DownloadFileTaskAsync(downloadurl, downloadPath + @"\" + filename);
            while (ifStop != true)
            {
                Thread.Sleep(1000);
            }
            downloader.CancelAsync();
        }

        private void OnDownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                infolabel.Text = "获取下载地址……大小：" + e.TotalBytesToReceive/1024/1024+"MB";
            });
        }
        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (ifStop == true)
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    infolabel.Text = "取消成功！";
                    try
                    {
                        File.Delete(downloadPath + @"\" + filename);
                    }
                    catch { }
                });
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                {
                    infolabel.Text = "下载完成！";
                });
            }
            Thread.Sleep(1000);
            downloadinfo = null;
            downloadurl = null;
            //Close();
            this.Dispatcher.Invoke(new Action(
    delegate
    {
        // ------- 需要完成什么操作,写在这里就可以了, 主线程会触发该Action来完成-------
        Close();
    })
    );
        }
        private void OnDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                infolabel.Text = "已下载：" + e.ReceivedBytesSize / 1024 / 1024 + "MB/"+e.TotalBytesToReceive / 1024 / 1024 + "MB" + " 进度：" + e.ProgressPercentage.ToString("f2") + "%" + " 速度：" + (e.BytesPerSecondSpeed / 1024 / 1024).ToString("f2") + "MB/s";
            pbar.Value=e.ProgressPercentage;
            });
            //Thread.Sleep(1000);
        }
        /*
        private void SendMsgHander(DownMsg msg)
        {
            switch (msg.Tag)
            {
                case DownStatus.Start:
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        infolabel.Text = "获取下载地址……大小：" + msg.LengthInfo;
                    });
                    break;
                case DownStatus.End:
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        pbar.Value = 100;
                        infolabel.Text = "下载完成！";
                    });
                    break;
                case DownStatus.DownLoad:
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        if (infolabel.Text != "停止下载中，请耐心等待……双击取消按钮可强制关闭此窗口")
                        {
                            
                        }
                        pbar.Value = msg.Progress;
                    });
                    System.Windows.Forms.Application.DoEvents();
                    break;
                case DownStatus.Error:
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
                    {
                        infolabel.Text = "失败：" + msg.ErrMessage;
                        System.Windows.Forms.Application.DoEvents();
                        //thread = new Thread(Downloadfile);
                        //thread.Start();
                    });
                    break;
            }
        }*/

        public static class DispatcherHelper
        {
            [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            public static void DoEvents()
            {
                DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrames), frame);
                try { Dispatcher.PushFrame(frame); }
                catch (InvalidOperationException) { }
            }
            private static object ExitFrames(object frame)
            {
                ((DispatcherFrame)frame).Continue = false;
                return null;
            }
        }


        private void mainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //Downloader.CancelAsync();
            ifStop = true;
        }

        private void button1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ifStop = true;
            Close();
        }
    }
    /*
    private void Downloadfile()
    {
        this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
        {
            infolabel.Text = "连接下载地址中...";
        });
        //try
        //{
        HttpWebRequest Myrq = (HttpWebRequest)HttpWebRequest.Create(downloadurl);
        HttpWebResponse myrp;
        myrp = (HttpWebResponse)Myrq.GetResponse();
        long totalBytes = myrp.ContentLength;
        Stream st = myrp.GetResponseStream();
        FileStream so;
        System.Windows.MessageBox.Show(downloadPath);
        if (downloadPath.Substring(downloadPath.Length - 2, 1) == "\\")
        {
            so = new FileStream(downloadPath + filename, FileMode.Create);
        }
        else
        {
            so = new FileStream(downloadPath + "\\" + filename, FileMode.Create);
        }
        long totalDownloadedByte = 0;
        byte[] by = new byte[1024];
        int osize = st.Read(by, 0, (int)by.Length);
        while (osize > 0)
        {
            totalDownloadedByte = osize + totalDownloadedByte;
            DispatcherHelper.DoEvents();
            so.Write(by, 0, osize);
            osize = st.Read(by, 0, (int)by.Length);
            float percent = 0;
            percent = (float)totalDownloadedByte / (float)totalBytes * 100;
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                infolabel.Text = "下载中：" + percent.ToString("f2") + "%";
                pbar.Value = percent;
            });
            DispatcherHelper.DoEvents();
        }
        so.Close();
        st.Close();
        System.Windows.Forms.MessageBox.Show("ok");
        this.Close();

        //}
        //catch (Exception aaa)
        //{
        //   this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
        //   {
        //      infolabel.Text = "发生错误" + aaa;
        //   });
        //}
    }*/
}
