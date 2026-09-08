using Downloader;
using MSL.langs;
using MSL.utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
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
        public event App.DeleControl CloseDialog;
        public int _dialogReturn = 0; // 0未开始下载（或下载中），1下载完成，2下载取消，3下载失败
        private readonly bool enableParalle = true; // 是否启用多线程下载
        private readonly string downloadPath;
        private readonly string filename;
        private readonly string downloadurl;
        private readonly string expectedSha256;
        private readonly bool closeDirectly;
        private readonly int headerMode; // 0等于无Header，1等于MSL Downloader，2等于伪装浏览器Header
        private DownloadService downloader;
        private DispatcherTimer updateUITimer;
        private readonly bool useNativeHttpClient = true;
        private CancellationTokenSource _nativeCts;
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);

        public DownloadDialog(string _downloadurl, string _downloadPath, string _filename, string downloadinfo, string sha256 = "", bool _closeDirectly = false, bool _enableParalle = true, int header = 1, bool _useNativeHttpClient = false)
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
            enableParalle = _enableParalle;
            useNativeHttpClient = _useNativeHttpClient;
            Task.Run(Downloader);
        }

        private async void Downloader()
        {
            LogHelper.Write.Info($"开始下载：{filename} ，下载地址：{downloadurl} ，保存路径：{downloadPath} ，启用多线程下载：{enableParalle} ，Header模式：{headerMode}。");

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                                                      | SecurityProtocolType.Tls11
                                                      | (SecurityProtocolType)12288;
            }
            catch
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            if (File.Exists(Path.Combine(downloadPath, filename)))
            {
                if (!string.IsNullOrEmpty(expectedSha256))
                {
                    if (VerifyFileSHA256(Path.Combine(downloadPath, filename), expectedSha256))
                    {
                        _dialogReturn = 1;
                        _ = Task.Run(async () =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                infolabel.Text = Lang.SR_FileExistsDownloadDone;
                                StatusLab.Text = LanguageManager.Instance["DownloadDialog_DownloadComplete"];
                            });
                            await Task.Delay(1000);
                            Dispatcher.Invoke(() =>
                            {
                                Close();
                            });
                        });
                        return;
                    }
                }
            }

            if (useNativeHttpClient)
            {
                await StartNativeHttpClientDownloadAsync();
                return;
            }

            var downloadOpt = new DownloadConfiguration();
            downloadOpt.RequestConfiguration.UserAgent = DownloadUA();
            if (enableParalle)
            {
                downloadOpt.ParallelDownload = true; // download parts of file as parallel or not. Default value is false
                downloadOpt.ChunkCount = ConfigStore.DownloadChunkCount; // file parts to download, default value is 1
            }
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
            _ = downloader.DownloadFileTaskAsync(downloadurl, downloadPath + "\\" + filename);

            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                Dispatcher.Invoke(() =>
                {
                    if (StatusLab.Text.Contains("加载中"))
                        StatusLab.Text = "加载中（若长时间无响应，请取消重试或使用代理）";
                });
            });
        }

        private void OnDownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                PauseBtn.IsEnabled = true;
                infolabel.Text = string.Format(LanguageManager.Instance["DownloadDialog_OnDownloadStarted"], e.TotalBytesToReceive / 1024 / 1024);
                // 初始化DispatcherTimer
                updateUITimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                updateUITimer.Tick += UpdateUITick;
                updateUITimer.Start();
                StatusLab.Text = Lang.SR_Downloading;
            });
        }

        // 下载完成的事件
        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    PauseBtn.IsEnabled = false;
                    updateUITimer?.Stop();
                });
            }
            catch { Console.WriteLine("Stop UITimer Failed"); }
            if (e.Cancelled || _dialogReturn == 2)
            {
                Dispatcher.Invoke(() =>
                {
                    infolabel.Text = LanguageManager.Instance["DownloadDialog_DownloadCancel"];
                    StatusLab.Text = LanguageManager.Instance["DownloadDialog_DownloadCancel"];
                    button1.Content = LanguageManager.Instance["Close"];
                    try
                    {
                        File.Delete(downloadPath + "\\" + filename);
                    }
                    catch { Console.WriteLine("Delete File Failed"); }
                });
            }
            else
            {
                if (e.Error != null || !File.Exists(downloadPath + "\\" + filename))
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusLab.Text = Lang.SR_Downloading;
                        pbar.Value = 0;
                        Thread thread = new Thread(DownloadFile);
                        thread.Start();
                    });
                    /* 此处已转移至备用下载方案（Thread(DownloadFile)）失败时的执行逻辑
                    if (closeDirectly)
                    {
                        Thread.Sleep(1000);
                        Dispatcher.Invoke(Close);
                    }
                    */
                    return;
                }

                if (!string.IsNullOrEmpty(expectedSha256))
                {
                    //有传入sha256，进行校验
                    if (VerifyFileSHA256(downloadPath + "\\" + filename, expectedSha256) == false)
                    {
                        //失败
                        _dialogReturn = 3;
                        Dispatcher.Invoke(() =>
                        {
                            button1.Content = LanguageManager.Instance["Close"];
                            infolabel.Text = LanguageManager.Instance["DownloadDialog_CheckIntegrityFailed"];
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
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    infolabel.Text = LanguageManager.Instance["DownloadDialog_DownloadComplete"];
                    pbar.Value = 100;
                });

                _dialogReturn = 1;
            }
            Thread.Sleep(1000);
            Dispatcher.Invoke(Close);
        }

        private string DownloadUA()
        {
            if (headerMode == 1)
            {
                return "MSLTeam-MSL/" + ConfigStore.MSLVersion + " (Downloader)";
            }
            else if (headerMode == 2)
            {
                return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            }
            return null;
        }

        #region --- 原生 HttpClient 下载 ---

        private async Task StartNativeHttpClientDownloadAsync()
        {
            _nativeCts = new CancellationTokenSource();
            _pauseEvent.Set();

            Dispatcher.Invoke(() =>
            {
                PauseBtn.IsEnabled = true;
                StatusLab.Text = Lang.SR_Downloading;
                updateUITimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                updateUITimer.Tick += UpdateUITick;
                updateUITimer.Start();
            });

            string fullPath = Path.Combine(downloadPath, filename);
            var handler = new HttpClientHandler
            {
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                             | (System.Security.Authentication.SslProtocols)12288,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            try
            {
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) })
                {
                    string ua = DownloadUA();
                    if (!string.IsNullOrEmpty(ua))
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);
                    }

                    using (var response = await client.GetAsync(downloadurl, HttpCompletionOption.ResponseHeadersRead, _nativeCts.Token))
                    {
                        response.EnsureSuccessStatusCode();
                        totalBytesToReceive = response.Content.Headers.ContentLength ?? -1;

                        using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                        using (var streamToWriteTo = File.Create(fullPath))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;
                            long totalRead = 0;
                            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                            long lastBytes = 0;
                            double lastTime = 0;

                            while ((bytesRead = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length, _nativeCts.Token)) > 0)
                            {
                                _pauseEvent.Wait(_nativeCts.Token);

                                await streamToWriteTo.WriteAsync(buffer, 0, bytesRead, _nativeCts.Token);
                                totalRead += bytesRead;

                                receivedBytes = totalRead;
                                if (totalBytesToReceive > 0)
                                {
                                    progressPercentage = (double)totalRead / totalBytesToReceive * 100;
                                }

                                double elapsed = stopwatch.Elapsed.TotalSeconds;
                                if (elapsed - lastTime >= 0.5)
                                {
                                    bytesPerSecondSpeed = (totalRead - lastBytes) / (elapsed - lastTime);
                                    lastBytes = totalRead;
                                    lastTime = elapsed;
                                }
                            }
                        }
                    }
                }

                // 成功完成逻辑
                if (!string.IsNullOrEmpty(expectedSha256))
                {
                    if (!VerifyFileSHA256(fullPath, expectedSha256))
                    {
                        _dialogReturn = 3;
                        Dispatcher.Invoke(() =>
                        {
                            button1.Content = LanguageManager.Instance["Close"];
                            infolabel.Text = LanguageManager.Instance["CheckIntegrityFailed"];
                            try { File.Delete(fullPath); } catch { }
                        });
                        if (closeDirectly)
                        {
                            Thread.Sleep(1000);
                            Dispatcher.Invoke(Close);
                        }
                        return;
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    PauseBtn.IsEnabled = false;
                    updateUITimer?.Stop();
                    infolabel.Text = LanguageManager.Instance["DownloadComplete"];
                    pbar.Value = 100;
                });

                _dialogReturn = 1;
                Thread.Sleep(1000);
                Dispatcher.Invoke(Close);
            }
            catch (OperationCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    PauseBtn.IsEnabled = false;
                    updateUITimer?.Stop();
                    infolabel.Text = LanguageManager.Instance["DownloadCancel"];
                    StatusLab.Text = LanguageManager.Instance["DownloadCancel"];
                    button1.Content = LanguageManager.Instance["Close"];
                    try
                    {
                        if (File.Exists(fullPath)) File.Delete(fullPath);
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"原生 HttpClient 下载失败: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    StatusLab.Text = Lang.SR_Downloading;
                    pbar.Value = 0;
                    Thread thread = new Thread(DownloadFile);
                    thread.Start();
                });
            }
        }

        #endregion

        private void DownloadFile()
        {
            // 使用Task异步执行下载任务
            Task.Run(() =>
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadurl);
                    request.UserAgent = DownloadUA();
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
                                        infolabel.Text = string.Format(LanguageManager.Instance["DownloadDialog_Mode2_Downloading"], percent);
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
                            infolabel.Text = LanguageManager.Instance["DownloadDialog_DownloadCancel"];
                            StatusLab.Text = LanguageManager.Instance["DownloadDialog_DownloadCancel"];
                            button1.Content = LanguageManager.Instance["Close"];
                            PauseBtn.IsEnabled = false;
                        }
                        else
                        {
                            _dialogReturn = 1;
                            infolabel.Text = LanguageManager.Instance["DownloadDialog_DownloadComplete"];
                        }
                    });
                }
                catch (Exception ex)
                {
                    _dialogReturn = 3;
                    // 异常处理
                    Dispatcher.Invoke(() =>
                    {
                        button1.Content = LanguageManager.Instance["Close"];
                        infolabel.Text = LanguageManager.Instance["DownloadDialog_DownloadFailed"];
                        StatusLab.Text = LanguageManager.Instance["DownloadDialog_DownloadFailed"] + "\n" + ex.Message;
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
                infolabel.Text = string.Format(LanguageManager.Instance["DownloadDialog_Downloading"],
                    receivedBytes / 1024 / 1024,
                    totalBytesToReceive / 1024 / 1024,
                    progressPercentage.ToString("F2"),
                    (bytesPerSecondSpeed / 1024 / 1024).ToString("F2"));
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

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PauseBtn.Content.ToString() == Lang.SR_Pause)
            {
                if (useNativeHttpClient)
                {
                    _pauseEvent.Reset();
                }
                else
                {
                    downloader?.Pause();
                }
                PauseBtn.Content = Lang.SR_Resume;
                StatusLab.Text = Lang.SR_Paused;
            }
            else
            {
                if (useNativeHttpClient)
                {
                    _pauseEvent.Set();
                }
                else
                {
                    downloader?.Resume();
                }
                PauseBtn.Content = Lang.SR_Pause;
                StatusLab.Text = Lang.SR_Downloading;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (button1.Content.ToString() == LanguageManager.Instance["Close"])
            {
                Close();
            }
            else
            {
                _dialogReturn = 2;
                if (useNativeHttpClient)
                {
                    _pauseEvent.Set();
                    _nativeCts?.Cancel();
                }
                else
                {
                    downloader?.CancelAsync();
                }

                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    Dispatcher.Invoke(() =>
                    {
                        if (StatusLab.Text.ToString() != LanguageManager.Instance["DownloadDialog_DownloadCancel"])
                        {
                            StatusLab.Text = Lang.SR_CancellingTask + "\n" + LanguageManager.Instance["DownloadDialog_DoubleClickForceClose"];
                        }
                    });
                });
            }
        }

        private void button1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (button1.Content.ToString() == LanguageManager.Instance["Close"])
            {
                Close();
            }
            else
            {
                _dialogReturn = 2;
                if (useNativeHttpClient)
                {
                    _pauseEvent.Set();
                    _nativeCts?.Cancel();
                }
                else
                {
                    downloader?.CancelAsync();
                }
                Close();
            }
        }

        // 用于校验sha256的函数
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
                    if (downloader != null)
                    {
                        downloader.DownloadStarted -= OnDownloadStarted;
                        downloader.DownloadProgressChanged -= OnDownloadProgressChanged;
                        downloader.DownloadFileCompleted -= OnDownloadFileCompleted;
                    }
                    CloseDialog();
                };

                storyboard.Begin();
            }
        }
    }
}
