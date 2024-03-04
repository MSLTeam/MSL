using Downloader;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MSL
{
    /// <summary>
    /// DownloadWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadWindow : HandyControl.Controls.Window
    {
        public static int downloadthread = 8;
        public bool isStopDwn;
        private readonly string downloadPath;
        private readonly string filename;
        private readonly string downloadurl;
        private readonly string expectedSha256;
        private DownloadService downloader;

        public DownloadWindow(string _downloadurl, string _downloadPath, string _filename, string downloadinfo,string sha256 = "")
        {
            InitializeComponent();

            if (!Directory.Exists(_downloadPath))
            {
                Directory.CreateDirectory(_downloadPath);
            }
            downloadurl = _downloadurl;
            downloadPath = _downloadPath;
            filename = _filename;
            if (sha256 != "" )
            {
                expectedSha256 = sha256;
            }
            else
            {
                expectedSha256 = "";
            }

            taskinfo.Text = downloadinfo;
            isStopDwn = false;
            Thread thread = new Thread(Downloader);
            thread.Start();
        }
        private void Downloader()
        {
            var downloadOpt = new DownloadConfiguration()
            {
                ChunkCount = downloadthread, // file parts to download, default value is 1
                ParallelDownload = true // download parts of file as parallel or not. Default value is false
            };
            downloader = new DownloadService(downloadOpt);
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
            downloader.DownloadFileTaskAsync(downloadurl, downloadPath + "\\" + filename);
        }

        private void OnDownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                infolabel.Text = "获取下载地址……大小：" + e.TotalBytesToReceive / 1024 / 1024 + "MB";
            });
        }

        //下载完成的事件
        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {

            if (isStopDwn == true)
            {
                Dispatcher.Invoke(() =>
                {
                    infolabel.Text = "取消成功！";
                    try
                    {
                        File.Delete(downloadPath + "\\" + filename);
                    }
                    catch { }
                });
                Thread.Sleep(1000);
                Dispatcher.Invoke(() =>
                {
                    Close();
                });
            }
            else
            {
                if (File.Exists(downloadPath + "\\" + filename))
                {
                    if (expectedSha256 == "")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            infolabel.Text = "下载完成！";
                            pbar.Value = 100;
                        });
                        Thread.Sleep(1000);
                        Dispatcher.Invoke(() =>
                        {
                            Close();
                        });
                    }
                    else
                    {
                        //有传入sha256，进行校验
                        if (VerifyFileSHA256(downloadPath + "\\" + filename, expectedSha256) == true)
                        {
                            //成功
                            Dispatcher.Invoke(() =>
                            {
                                infolabel.Text = "下载完成！";
                                pbar.Value = 100;
                            });
                            Thread.Sleep(1000);
                            Dispatcher.Invoke(() =>
                            {
                                Close();
                            });

                        }
                        else
                        {
                            //失败
                            Dispatcher.Invoke(() =>
                            {
                                infolabel.Text = "校验完整性失败！请重新下载！";
                                try
                                {
                                    File.Delete(downloadPath + "\\" + filename);
                                }
                                catch { }
                            });
                            Thread.Sleep(1000);
                            Dispatcher.Invoke(() =>
                            {
                                Close();
                            });
                        }
                    }

                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        pbar.Value = 0;
                        Thread thread = new Thread(DownloadFile);
                        thread.Start();
                    });
                }
            }
        }

        void DownloadFile()
        {
            Dispatcher.Invoke(() =>
            {
                infolabel.Text = "连接下载地址中...";
            });
            try
            {
                HttpWebRequest Myrq = (HttpWebRequest)HttpWebRequest.Create(downloadurl);
                HttpWebResponse myrp;
                myrp = (HttpWebResponse)Myrq.GetResponse();
                long totalBytes = myrp.ContentLength;
                Stream st = myrp.GetResponseStream();
                FileStream so = new FileStream(downloadPath + "\\" + filename, FileMode.Create);
                long totalDownloadedByte = 0;
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                Dispatcher.Invoke(() =>
                {
                    if (pbar != null)
                    {
                        pbar.Maximum = (int)totalBytes;
                    }
                });
                while (osize > 0)
                {
                    if (isStopDwn)
                    {
                        break;
                    }
                    totalDownloadedByte = osize + totalDownloadedByte;
                    DispatcherHelper.DoEvents();
                    so.Write(by, 0, osize);
                    osize = st.Read(by, 0, (int)by.Length);
                    float percent = 0;
                    percent = (float)totalDownloadedByte / (float)totalBytes * 100;
                    Dispatcher.Invoke(() =>
                    {
                        if (pbar != null)
                        {
                            pbar.Value = (int)totalDownloadedByte;
                        }
                        infolabel.Text = "下载中，进度" + percent.ToString("f2") + "%";
                    });
                    DispatcherHelper.DoEvents();
                }
                so.Close();
                st.Close();
                Dispatcher.Invoke(() =>
                {
                    if (isStopDwn && File.Exists(downloadPath + "\\" + filename))
                    {
                        File.Delete(downloadPath + "\\" + filename);
                    }
                    else
                    {
                        infolabel.Text = "下载完成！";
                    }
                });
                Thread.Sleep(1000);
                Dispatcher.Invoke(() =>
                {
                    Close();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    infolabel.Text = "下载失败！" + ex.Message;
                });
                Thread.Sleep(1000);
                Dispatcher.Invoke(() =>
                {
                    Close();
                });

            }
        }

        int counter = 0;
        private void OnDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            if (counter <= 200)
            {
                counter++;
            }
            else
            {
                counter = 0;
                Dispatcher.Invoke(() =>
                {
                    infolabel.Text = "已下载：" + e.ReceivedBytesSize / 1024 / 1024 + "MB/" + e.TotalBytesToReceive / 1024 / 1024 + "MB" + " 进度：" + e.ProgressPercentage.ToString("f2") + "%" + " 速度：" + (e.BytesPerSecondSpeed / 1024 / 1024).ToString("f2") + "MB/s";
                    pbar.Value = e.ProgressPercentage;
                });
            }

            //Thread.Sleep(1000);
        }

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

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            downloader.CancelAsync();
            isStopDwn = true;
        }

        private void button1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            isStopDwn = true;
            Close();
        }

        //用于校验sha256的函数
        public bool VerifyFileSHA256(string filePath, string expectedHash)
        {
            using (FileStream stream = File.OpenRead(filePath)) //文件流
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] hash = sha.ComputeHash(stream);
                string calculatedHash = BitConverter.ToString(hash).Replace("-", String.Empty);

                return String.Equals(calculatedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
