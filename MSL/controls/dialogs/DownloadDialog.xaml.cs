﻿using Downloader;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MSL
{
    /// <summary>
    /// DownloadDialog.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadDialog
    {
        public event DeleControl CloseDialog;
        public int _dialogReturn = 0; // 0未开始下载（或下载中），1下载完成，2下载取消，3下载失败
        public static int downloadthread = 8;
        private readonly string downloadPath;
        private readonly string filename;
        private readonly string downloadurl;
        private readonly string expectedSha256;
        private readonly bool closeDirectly;
        private readonly int headerMode; //0等于自动检测（MSL Downloader或无Header），1等于无Header，2等于MSL Downloader，3等于伪装浏览器Header
        private DownloadService downloader;
        private DispatcherTimer updateUITimer;

        public DownloadDialog(string _downloadurl, string _downloadPath, string _filename, string downloadinfo, string sha256 = "", bool _closeDirectly = false, int header = 0)
        {
            InitializeComponent();
            Directory.CreateDirectory(_downloadPath);
            downloadurl = _downloadurl;
            downloadPath = _downloadPath;
            filename = _filename;
            expectedSha256 = sha256;
            closeDirectly = _closeDirectly;
            headerMode = header;
            taskinfo.Text = downloadinfo;
            Thread thread = new Thread(Downloader);
            thread.Start();
        }
        private void Downloader()
        {
            var downloadOpt = new DownloadConfiguration();
            if (headerMode == 0)
            {
                string serverLink = MainWindow.serverLink;
                if (serverLink.Contains("/"))
                {
                    serverLink = serverLink.Substring(0, serverLink.IndexOf("/"));
                }
                if (downloadurl.Contains(serverLink))
                {
                    downloadOpt.RequestConfiguration.UserAgent = "MSL Downloader/" + new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                }
            }
            else if (headerMode == 2)
            {
                downloadOpt.RequestConfiguration.UserAgent = "MSL Downloader/" + new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            }
            else if (headerMode == 3)
            {
                downloadOpt.RequestConfiguration.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            }
            downloadOpt.Timeout = 5000;
            downloadOpt.ChunkCount = downloadthread; // file parts to download, default value is 1
            downloadOpt.ParallelDownload = true; // download parts of file as parallel or not. Default value is false
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
                // 初始化DispatcherTimer
                updateUITimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                updateUITimer.Tick += UpdateUITick;
                updateUITimer.Start();
            });
        }

        //下载完成的事件
        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                updateUITimer.Stop();
            }
            catch { Console.WriteLine("Stop UITimer Failed"); }
            if (_dialogReturn == 2)
            {
                Dispatcher.Invoke(() =>
                {
                    infolabel.Text = "取消成功！";
                    try
                    {
                        File.Delete(downloadPath + "\\" + filename);
                    }
                    catch { Console.WriteLine("Delete File Failed"); }
                });
            }
            else
            {
                if (File.Exists(downloadPath + "\\" + filename))
                {
                    _dialogReturn = 1;
                    Dispatcher.Invoke(() =>
                    {
                        infolabel.Text = "下载完成！";
                        pbar.Value = 100;
                    });
                    if (!string.IsNullOrEmpty(expectedSha256))
                    {
                        //有传入sha256，进行校验
                        if (VerifyFileSHA256(downloadPath + "\\" + filename, expectedSha256) == false)
                        {
                            //失败
                            _dialogReturn = 3;
                            Dispatcher.Invoke(() =>
                            {
                                button1.Content = "关闭";
                                infolabel.Text = "校验完整性失败！请重新下载！";
                                try
                                {
                                    File.Delete(downloadPath + "\\" + filename);
                                }
                                catch { }
                            });
                            if (closeDirectly)
                            {
                                Thread.Sleep(1000);
                                Dispatcher.Invoke(Close);
                            }
                            return;
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
                    return;
                }
            }
            Thread.Sleep(1000);
            Dispatcher.Invoke(Close);
        }

        private void DownloadFile()
        {
            // 使用Task异步执行下载任务
            Task.Run(() =>
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadurl);
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        long totalBytes = response.ContentLength;
                        using (Stream responseStream = response.GetResponseStream())
                        using (FileStream fileStream = new FileStream(Path.Combine(downloadPath, filename), FileMode.Create))
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead;
                            long totalDownloadedByte = 0;
                            // 创建Progress<T>来报告进度
                            var progress = new Progress<int>(percent =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    if (pbar != null)
                                    {
                                        pbar.Value = percent;
                                        infolabel.Text = $"下载中，进度{percent}%";
                                    }
                                });
                            });

                            while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (_dialogReturn == 2) break;
                                fileStream.Write(buffer, 0, bytesRead);
                                totalDownloadedByte += bytesRead;
                                // 计算并报告进度
                                int percentComplete = (int)(totalDownloadedByte * 100 / totalBytes);
                                ((IProgress<int>)progress).Report(percentComplete);
                            }
                        }
                    }
                    // 下载完成后更新UI
                    Dispatcher.Invoke(() =>
                    {
                        if (_dialogReturn == 2 && File.Exists(Path.Combine(downloadPath, filename)))
                        {
                            File.Delete(Path.Combine(downloadPath, filename));
                            infolabel.Text = "下载取消！";
                        }
                        else
                        {
                            _dialogReturn = 1;
                            infolabel.Text = "下载完成！";
                        }
                    });
                }
                catch (Exception ex)
                {
                    _dialogReturn = 3;
                    // 异常处理
                    Dispatcher.Invoke(() =>
                    {
                        button1.Content = "关闭";
                        infolabel.Text = "下载失败！" + ex.Message;
                    });
                }
                Thread.Sleep(1000);
            }).ContinueWith(t =>
            {
                if (_dialogReturn != 3 || closeDirectly)
                {
                    // 关闭对话框
                    Dispatcher.Invoke(Close);
                }
            });
        }

        private long receivedBytes;
        private long totalBytesToReceive;
        private double progressPercentage;
        private double bytesPerSecondSpeed;

        private void UpdateUITick(object sender, EventArgs e)
        {
            // 更新UI的方法
            if (pbar != null && infolabel != null)
            {
                infolabel.Text = $"已下载：{receivedBytes / 1024 / 1024}MB/{totalBytesToReceive / 1024 / 1024}MB 进度：{progressPercentage:f2}% 速度：{bytesPerSecondSpeed / 1024 / 1024:f2}MB/s";
                pbar.Value = progressPercentage;
            }
        }

        private void OnDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            // 更新变量，供UpdateUITick使用
            receivedBytes = e.ReceivedBytesSize;
            totalBytesToReceive = e.TotalBytesToReceive;
            progressPercentage = e.ProgressPercentage;
            bytesPerSecondSpeed = e.BytesPerSecondSpeed;
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
            if (button1.Content.ToString() == "关闭")
            {
                Close();
            }
            else
            {
                downloader.CancelAsync();
                _dialogReturn = 2;
            }
        }

        private void button1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (button1.Content.ToString() == "关闭")
            {
                Close();
            }
            else
            {
                downloader.CancelAsync();
                _dialogReturn = 2;
                Close();
            }
        }

        //用于校验sha256的函数
        public bool VerifyFileSHA256(string filePath, string expectedHash)
        {
            using (FileStream stream = File.OpenRead(filePath)) //文件流
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] hash = sha.ComputeHash(stream);
                string calculatedHash = BitConverter.ToString(hash).Replace("-", string.Empty);

                return string.Equals(calculatedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
        }
        private void Close()
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation scaleDownX = new DoubleAnimation(1, 1.1, TimeSpan.FromSeconds(0.15));
            DoubleAnimation scaleDownY = new DoubleAnimation(1, 1.1, TimeSpan.FromSeconds(0.15));
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));

            storyboard.Children.Add(scaleDownX);
            storyboard.Children.Add(scaleDownY);
            storyboard.Children.Add(fadeOut);

            if (Template.FindName("contentPresenter", this) is ContentPresenter contentPresenter)
            {
                Storyboard.SetTarget(scaleDownX, contentPresenter);
                Storyboard.SetTarget(scaleDownY, contentPresenter);
                Storyboard.SetTarget(fadeOut, contentPresenter);

                Storyboard.SetTargetProperty(scaleDownX, new PropertyPath("RenderTransform.ScaleX"));
                Storyboard.SetTargetProperty(scaleDownY, new PropertyPath("RenderTransform.ScaleY"));
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));

                storyboard.Completed += (s, a) =>
                {
                    Visibility = Visibility.Collapsed;
                    CloseDialog();
                };

                storyboard.Begin();
            }
        }
    }
}
