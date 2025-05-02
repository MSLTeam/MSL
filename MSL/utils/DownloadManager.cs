using Downloader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MSL.utils
{
    public class DownloadManager
    {
        #region 单例模式
        private static readonly Lazy<DownloadManager> _instance = new Lazy<DownloadManager>(() => new DownloadManager());
        public static DownloadManager Instance => _instance.Value;
        private DownloadManager() { }
        #endregion

        #region 事件定义
        public delegate void DownloadItemProgressChangedEventHandler(string groupId, string itemId, DownloadProgressInfo progressInfo);
        public delegate void DownloadItemCompletedEventHandler(string groupId, string itemId, bool success, Exception error = null);
        public delegate void DownloadGroupCompletedEventHandler(string groupId, bool allSuccess);

        public event DownloadItemProgressChangedEventHandler DownloadItemProgressChanged;
        public event DownloadItemCompletedEventHandler DownloadItemCompleted;
        public event DownloadGroupCompletedEventHandler DownloadGroupCompleted;
        #endregion

        #region 私有字段
        private readonly ConcurrentDictionary<string, DownloadGroup> _downloadGroups = new ConcurrentDictionary<string, DownloadGroup>();
        private readonly ConcurrentDictionary<string, DownloadItem> _downloadItems = new ConcurrentDictionary<string, DownloadItem>();
        private readonly ConcurrentDictionary<string, DownloadService> _downloaders = new ConcurrentDictionary<string, DownloadService>();
        #endregion

        #region 公共属性
        public static int DefaultConcurrentDownloads { get; set; } = 3;
        public static int DefaultThreadsPerDownload { get; set; } = 8;
        #endregion

        #region 公共方法
        /// <summary>
        /// 创建一个新的下载组
        /// </summary>
        /// <param name="groupId">组ID，如果为null则自动生成</param>
        /// <param name="maxConcurrentDownloads">该组最大并发下载数，默认为3</param>
        /// <returns>下载组ID</returns>
        public string CreateDownloadGroup(string groupId = null, int maxConcurrentDownloads = default)
        {
            if(maxConcurrentDownloads <= 0)
                maxConcurrentDownloads = DefaultConcurrentDownloads;
            groupId = groupId ?? Guid.NewGuid().ToString();
            var group = new DownloadGroup
            {
                GroupId = groupId,
                MaxConcurrentDownloads = maxConcurrentDownloads,
                Status = DownloadGroupStatus.Ready
            };
            _downloadGroups[groupId] = group;
            return groupId;
        }

        /// <summary>
        /// 添加下载项到指定组
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <param name="url">下载URL</param>
        /// <param name="downloadPath">下载目录</param>
        /// <param name="filename">文件名</param>
        /// <param name="expectedSha256">预期SHA256（可选）</param>
        /// <param name="itemId">项ID，如果为null则自动生成</param>
        /// <param name="headerMode">下载请求头模式: 0=无, 1=MSL, 2=浏览器</param>
        /// <returns>下载项ID</returns>
        public string AddDownloadItem(string groupId, string url, string downloadPath, string filename,
                                      string expectedSha256 = "", string itemId = null, int headerMode = 1)
        {
            if (!_downloadGroups.ContainsKey(groupId))
                throw new ArgumentException($"Download group '{groupId}' does not exist");

            itemId = itemId ?? Guid.NewGuid().ToString();

            // 创建目录
            Directory.CreateDirectory(downloadPath);

            var item = new DownloadItem
            {
                ItemId = itemId,
                GroupId = groupId,
                Url = url,
                DownloadPath = downloadPath,
                Filename = filename,
                ExpectedSha256 = expectedSha256,
                HeaderMode = headerMode,
                Status = DownloadStatus.Pending,
                Progress = new DownloadProgressInfo()
            };

            _downloadItems[itemId] = item;
            _downloadGroups[groupId].Items.Add(itemId);

            return itemId;
        }

        /// <summary>
        /// 开始指定组的下载任务
        /// </summary>
        /// <param name="groupId">组ID</param>
        public void StartDownloadGroup(string groupId)
        {
            if (!_downloadGroups.TryGetValue(groupId, out var group))
                throw new ArgumentException($"Download group '{groupId}' does not exist");

            if (group.Status == DownloadGroupStatus.InProgress)
                return;

            group.Status = DownloadGroupStatus.InProgress;
            Task.Run(() => ProcessDownloadGroup(groupId));
        }

        /// <summary>
        /// 等待指定组的下载完成
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <returns>下载任务</returns>
        public Task<bool> WaitForGroupCompletionAsync(string groupId)
        {
            if (!_downloadGroups.TryGetValue(groupId, out var group))
                throw new ArgumentException($"Download group '{groupId}' does not exist");

            return group.CompletionTask.Task;
        }

        /// <summary>
        /// 取消指定组的所有下载
        /// </summary>
        /// <param name="groupId">组ID</param>
        public void CancelGroup(string groupId)
        {
            if (!_downloadGroups.TryGetValue(groupId, out var group))
                return;

            group.Status = DownloadGroupStatus.Cancelling;

            // 取消该组内所有正在下载的项
            foreach (var itemId in group.Items)
            {
                if (_downloadItems.TryGetValue(itemId, out var item) &&
                    (item.Status == DownloadStatus.InProgress || item.Status == DownloadStatus.Pending))
                {
                    CancelDownloadItem(itemId);
                }
            }
        }

        /// <summary>
        /// 取消指定下载项
        /// </summary>
        /// <param name="itemId">下载项ID</param>
        public void CancelDownloadItem(string itemId)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item))
                return;

            item.Status = DownloadStatus.Cancelling;

            if (_downloaders.TryGetValue(itemId, out var downloader))
            {
                downloader.CancelAsync();
            }
        }

        /// <summary>
        /// 暂停指定下载项
        /// </summary>
        /// <param name="itemId">下载项ID</param>
        public void PauseDownloadItem(string itemId)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item) || item.Status != DownloadStatus.InProgress)
                return;

            if (_downloaders.TryGetValue(itemId, out var downloader))
            {
                downloader.Pause();
                item.Status = DownloadStatus.Paused;
            }
        }

        /// <summary>
        /// 恢复指定下载项
        /// </summary>
        /// <param name="itemId">下载项ID</param>
        public void ResumeDownloadItem(string itemId)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item) || item.Status != DownloadStatus.Paused)
                return;

            if (_downloaders.TryGetValue(itemId, out var downloader))
            {
                downloader.Resume();
                item.Status = DownloadStatus.InProgress;
            }
        }

        /// <summary>
        /// 获取所有下载组信息
        /// </summary>
        /// <returns>下载组列表</returns>
        public IEnumerable<DownloadGroup> GetAllGroups()
        {
            return _downloadGroups.Values.ToList();
        }

        /// <summary>
        /// 获取指定组的所有下载项
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <returns>下载项列表</returns>
        public IEnumerable<DownloadItem> GetGroupItems(string groupId)
        {
            if (!_downloadGroups.TryGetValue(groupId, out var group))
                return Enumerable.Empty<DownloadItem>();

            return group.Items
                .Where(id => _downloadItems.ContainsKey(id))
                .Select(id => _downloadItems[id])
                .ToList();
        }

        /// <summary>
        /// 获取指定下载项信息
        /// </summary>
        /// <param name="itemId">下载项ID</param>
        /// <returns>下载项信息</returns>
        public DownloadItem GetDownloadItem(string itemId)
        {
            return _downloadItems.TryGetValue(itemId, out var item) ? item : null;
        }

        /// <summary>
        /// 清理指定组及其下载项
        /// </summary>
        /// <param name="groupId">组ID</param>
        public void RemoveGroup(string groupId)
        {
            if (!_downloadGroups.TryGetValue(groupId, out var group))
                return;

            // 如果组还在下载中，先取消所有下载
            if (group.Status == DownloadGroupStatus.InProgress)
            {
                CancelGroup(groupId);
            }

            // 移除所有下载项
            foreach (var itemId in group.Items.ToList())
            {
                _downloadItems.TryRemove(itemId, out _);
                _downloaders.TryRemove(itemId, out _);
            }

            // 移除组
            _downloadGroups.TryRemove(groupId, out _);
        }
        #endregion

        #region 私有方法
        private async void ProcessDownloadGroup(string groupId)
        {
            if (!_downloadGroups.TryGetValue(groupId, out var group))
                return;

            var pendingItems = group.Items
                .Where(id => _downloadItems.ContainsKey(id) && _downloadItems[id].Status == DownloadStatus.Pending)
                .ToList();

            var downloadTasks = new List<Task>();
            var semaphore = new SemaphoreSlim(group.MaxConcurrentDownloads);

            // 启动所有下载任务
            foreach (var itemId in pendingItems)
            {
                await semaphore.WaitAsync();

                var downloadTask = Task.Run(async () =>
                {
                    try
                    {
                        await DownloadItemAsync(itemId);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                downloadTasks.Add(downloadTask);
            }

            // 等待所有下载完成
            await Task.WhenAll(downloadTasks);

            // 检查组内所有下载是否成功
            var allSuccess = true;
            foreach (var itemId in group.Items)
            {
                if (_downloadItems.TryGetValue(itemId, out var item))
                {
                    if (item.Status != DownloadStatus.Completed)
                    {
                        allSuccess = false;
                        break;
                    }
                }
                else
                {
                    allSuccess = false;
                    break;
                }
            }

            // 更新组状态
            group.Status = allSuccess ? DownloadGroupStatus.Completed : DownloadGroupStatus.CompletedWithErrors;
            group.CompletionTask.SetResult(allSuccess);

            // 触发组完成事件
            DownloadGroupCompleted?.Invoke(groupId, allSuccess);
        }

        private async Task DownloadItemAsync(string itemId)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item))
                return;

            // 标记为进行中
            item.Status = DownloadStatus.InProgress;
            bool success = false;
            Exception error = null;

            try
            {
                // 创建下载配置
                var downloadOpt = new DownloadConfiguration
                {
                    ChunkCount = DefaultThreadsPerDownload,
                    ParallelDownload = true,
                    Timeout = 5000
                };

                // 设置用户代理
                downloadOpt.RequestConfiguration.UserAgent = GetUserAgent(item.HeaderMode);

                // 创建下载服务
                var downloader = new DownloadService(downloadOpt);
                _downloaders[itemId] = downloader;

                // 注册事件
                downloader.DownloadStarted += (sender, e) => OnDownloadStarted(itemId, e);
                downloader.DownloadProgressChanged += (sender, e) => OnDownloadProgressChanged(itemId, e);

                // 完成事件使用TaskCompletionSource来异步等待
                var downloadCompletionSource = new TaskCompletionSource<bool>();
                downloader.DownloadFileCompleted += (sender, e) =>
                {
                    OnDownloadFileCompleted(itemId, e, downloadCompletionSource);
                };

                // 开始下载
                await downloader.DownloadFileTaskAsync(item.Url, Path.Combine(item.DownloadPath, item.Filename));

                // 等待下载完成事件
                success = await downloadCompletionSource.Task;
            }
            catch (Exception ex)
            {
                success = false;
                error = ex;

                // 如果使用多线程下载失败，尝试使用单线程下载
                if (item.Status != DownloadStatus.Cancelled && item.Status != DownloadStatus.Cancelling)
                {
                    try
                    {
                        success = await FallbackDownloadAsync(item);
                    }
                    catch (Exception fallbackEx)
                    {
                        error = fallbackEx;
                    }
                }
            }
            finally
            {
                // 触发项完成事件
                DownloadItemCompleted?.Invoke(item.GroupId, itemId, success, error);

                // 清理资源
                if (_downloaders.TryRemove(itemId, out var downloader))
                {
                    downloader.Dispose();
                }
            }
        }

        private void OnDownloadStarted(string itemId, DownloadStartedEventArgs e)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item))
                return;

            item.Progress.TotalBytes = e.TotalBytesToReceive;
            item.Progress.StartTime = DateTime.Now;
        }

        private void OnDownloadProgressChanged(string itemId, Downloader.DownloadProgressChangedEventArgs e)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item))
                return;

            // 更新进度信息
            item.Progress.ReceivedBytes = e.ReceivedBytesSize;
            item.Progress.TotalBytes = e.TotalBytesToReceive;
            item.Progress.ProgressPercentage = e.ProgressPercentage;
            item.Progress.BytesPerSecond = e.BytesPerSecondSpeed;

            // 触发进度变更事件
            DownloadItemProgressChanged?.Invoke(item.GroupId, itemId, item.Progress);
        }

        private void OnDownloadFileCompleted(string itemId, System.ComponentModel.AsyncCompletedEventArgs e, TaskCompletionSource<bool> completionSource)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item))
            {
                completionSource.SetResult(false);
                return;
            }

            bool success = false;

            if (e.Cancelled)
            {
                item.Status = DownloadStatus.Cancelled;
                try
                {
                    File.Delete(Path.Combine(item.DownloadPath, item.Filename));
                }
                catch { }
            }
            else if (e.Error != null || !File.Exists(Path.Combine(item.DownloadPath, item.Filename)))
            {
                item.Status = DownloadStatus.Failed;
                item.ErrorMessage = e.Error?.Message ?? "File not found after download";
            }
            else
            {
                // 如果需要校验SHA256
                if (!string.IsNullOrEmpty(item.ExpectedSha256))
                {
                    if (!VerifyFileSHA256(Path.Combine(item.DownloadPath, item.Filename), item.ExpectedSha256))
                    {
                        item.Status = DownloadStatus.Failed;
                        item.ErrorMessage = "SHA256 verification failed";
                        try
                        {
                            File.Delete(Path.Combine(item.DownloadPath, item.Filename));
                        }
                        catch { }
                    }
                    else
                    {
                        item.Status = DownloadStatus.Completed;
                        success = true;
                    }
                }
                else
                {
                    item.Status = DownloadStatus.Completed;
                    success = true;
                }
            }

            completionSource.SetResult(success);
        }

        private async Task<bool> FallbackDownloadAsync(DownloadItem item)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(item.Url);
                request.UserAgent = GetUserAgent(item.HeaderMode);

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    long totalBytes = response.ContentLength;
                    item.Progress.TotalBytes = totalBytes;

                    using (Stream responseStream = response.GetResponseStream())
                    using (FileStream fileStream = new FileStream(Path.Combine(item.DownloadPath, item.Filename), FileMode.Create))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long totalDownloadedByte = 0;
                        DateTime lastProgressUpdate = DateTime.Now;
                        long lastBytesReceived = 0;

                        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            if (item.Status == DownloadStatus.Cancelling || item.Status == DownloadStatus.Cancelled)
                            {
                                fileStream.Close();
                                try { File.Delete(Path.Combine(item.DownloadPath, item.Filename)); } catch { }
                                return false;
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalDownloadedByte += bytesRead;

                            // 更新进度，但不要过于频繁
                            TimeSpan elapsed = DateTime.Now - lastProgressUpdate;
                            if (elapsed.TotalMilliseconds > 500)
                            {
                                double bytesPerSecond = (totalDownloadedByte - lastBytesReceived) / elapsed.TotalSeconds;

                                // 更新进度信息
                                item.Progress.ReceivedBytes = totalDownloadedByte;
                                item.Progress.ProgressPercentage = totalBytes > 0 ? (double)totalDownloadedByte * 100 / totalBytes : 0;
                                item.Progress.BytesPerSecond = bytesPerSecond;

                                // 触发进度变更事件
                                DownloadItemProgressChanged?.Invoke(item.GroupId, item.ItemId, item.Progress);

                                lastProgressUpdate = DateTime.Now;
                                lastBytesReceived = totalDownloadedByte;
                            }
                        }
                    }
                }

                // 验证文件完整性（如果需要）
                if (!string.IsNullOrEmpty(item.ExpectedSha256))
                {
                    if (!VerifyFileSHA256(Path.Combine(item.DownloadPath, item.Filename), item.ExpectedSha256))
                    {
                        item.Status = DownloadStatus.Failed;
                        item.ErrorMessage = "SHA256 verification failed";
                        try { File.Delete(Path.Combine(item.DownloadPath, item.Filename)); } catch { }
                        return false;
                    }
                }

                item.Status = DownloadStatus.Completed;
                return true;
            }
            catch (Exception ex)
            {
                item.Status = DownloadStatus.Failed;
                item.ErrorMessage = ex.Message;
                return false;
            }
        }

        private string GetUserAgent(int headerMode)
        {
            switch (headerMode)
            {
                case 1:
                    return "MSLTeam-MSL/" + ConfigStore.MSLVersion + " (Downloader)";
                case 2:
                    return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
                default:
                    return null;
            }
        }

        private bool VerifyFileSHA256(string filePath, string expectedHash)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] hash = sha.ComputeHash(stream);
                string calculatedHash = BitConverter.ToString(hash).Replace("-", string.Empty);

                return string.Equals(calculatedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
        }
        #endregion
    }

    #region 支持类和枚举
    /// <summary>
    /// 下载组状态枚举
    /// </summary>
    public enum DownloadGroupStatus
    {
        Ready,
        InProgress,
        Cancelling,
        Completed,
        CompletedWithErrors
    }

    /// <summary>
    /// 下载项状态枚举
    /// </summary>
    public enum DownloadStatus
    {
        Pending,
        InProgress,
        Paused,
        Cancelling,
        Cancelled,
        Completed,
        Failed
    }

    /// <summary>
    /// 下载组信息类
    /// </summary>
    public class DownloadGroup
    {
        public string GroupId { get; set; }
        public DownloadGroupStatus Status { get; set; }
        public int MaxConcurrentDownloads { get; set; }
        public List<string> Items { get; set; } = new List<string>();
        public TaskCompletionSource<bool> CompletionTask { get; set; } = new TaskCompletionSource<bool>();
    }

    /// <summary>
    /// 下载项信息类
    /// </summary>
    public class DownloadItem
    {
        public string ItemId { get; set; }
        public string GroupId { get; set; }
        public string Url { get; set; }
        public string DownloadPath { get; set; }
        public string Filename { get; set; }
        public string ExpectedSha256 { get; set; }
        public int HeaderMode { get; set; }
        public DownloadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public DownloadProgressInfo Progress { get; set; }
    }

    /// <summary>
    /// 下载进度信息类
    /// </summary>
    public class DownloadProgressInfo
    {
        public long ReceivedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double ProgressPercentage { get; set; }
        public double BytesPerSecond { get; set; }
        public DateTime StartTime { get; set; }

        public TimeSpan ElapsedTime => DateTime.Now - StartTime;

        public TimeSpan EstimatedTimeRemaining
        {
            get
            {
                if (BytesPerSecond <= 0 || ReceivedBytes >= TotalBytes)
                    return TimeSpan.Zero;

                long remainingBytes = TotalBytes - ReceivedBytes;
                double secondsRemaining = remainingBytes / BytesPerSecond;
                return TimeSpan.FromSeconds(secondsRemaining);
            }
        }
    }
    #endregion
}
