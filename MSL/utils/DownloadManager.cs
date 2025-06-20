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
        public static int DefaultConcurrentDownloads { get; set; } = 4;
        public static int DefaultThreadsPerDownload { get; set; } = 8;
        #endregion

        #region 公共方法
        /// <summary>
        /// 创建一个新的下载组
        /// </summary>
        /// <param name="groupId">组ID，如果为null则自动生成</param>
        /// <param name="maxConcurrentDownloads">该组最大并发下载数，默认为3</param>
        /// <returns>下载组ID</returns>
        public string CreateDownloadGroup(string groupId = null,bool isTempGroup=false, int maxConcurrentDownloads = default)
        {
            if (maxConcurrentDownloads == default)
                maxConcurrentDownloads = DefaultConcurrentDownloads;
            groupId = groupId ?? Guid.NewGuid().ToString();
            var group = new DownloadGroup
            {
                GroupId = groupId,
                MaxConcurrentDownloads = maxConcurrentDownloads,
                Status = DownloadGroupStatus.Ready,
                IsTempGroup = isTempGroup
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
        /// <param name="enableParallel">是否启用并行下载</param>
        /// <param name="headerMode">下载请求头模式: 0=无, 1=MSL, 2=浏览器</param>
        /// <param name="retryCount">下载失败重试次数</param>
        /// <returns>下载项ID</returns>
        public string AddDownloadItem(string groupId, string url, string downloadPath, string filename,
                                      string expectedSha256 = "", string itemId = null, bool enableParallel = true,
                                      DownloadUAMode uaMode = DownloadUAMode.MSL, int retryCount = 1)
        {
            if (!_downloadGroups.ContainsKey(groupId))
                throw new ArgumentException($"Download group '{groupId}' does not exist");
            LogHelper.Write.Info($"添加下载项: {url} 到组 {groupId}，下载位置 {downloadPath}，期待的sha256 {expectedSha256}。");

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
                EnableParallel = enableParallel,
                UAMode = uaMode,
                Status = DownloadStatus.Pending,
                Progress = new DownloadProgressInfo(),
                RetryCount = retryCount
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
            var task = group.CompletionTask.Task;
            if (group.IsTempGroup)
                RemoveDownloadGroup(groupId);
            return task;
        }

        /// <summary>
        /// 取消指定组的所有下载
        /// </summary>
        /// <param name="groupId">组ID</param>
        public void CancelDownloadGroup(string groupId)
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
        /// 获取所有下载项信息
        /// </summary>
        /// <returns>下载项列表</returns>
        public IEnumerable<DownloadItem> GetAllItems()
        {
            return _downloadItems.Values.ToList();
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
        public void RemoveDownloadGroup(string groupId)
        {
            if (!_downloadGroups.TryGetValue(groupId, out var group))
                return;

            // 如果组还在下载中，先取消所有下载
            if (group.Status == DownloadGroupStatus.InProgress)
            {
                CancelDownloadGroup(groupId);
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

            // 完成组下载，更新状态
            CompleteDownloadGroup(groupId);
        }

        /// <summary>
        /// 完成下载组并更新状态
        /// </summary>
        private void CompleteDownloadGroup(string groupId)
        {
            if (!_downloadGroups.TryGetValue(groupId, out var group))
                return;

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
            group.CompletionTask.TrySetResult(allSuccess);

            // 触发组完成事件
            DownloadGroupCompleted?.Invoke(groupId, allSuccess);
        }

        /// <summary>
        /// 下载单个项目的主要方法
        /// </summary>
        private async Task DownloadItemAsync(string itemId)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item))
                return;

            if (File.Exists(Path.Combine(item.DownloadPath, item.Filename)) &&
                !string.IsNullOrEmpty(item.ExpectedSha256) && VerifyFileSHA256(Path.Combine(item.DownloadPath, item.Filename), item.ExpectedSha256))
            {
                // SHA256匹配，标记为完成
                item.Status = DownloadStatus.Completed;
                CompleteDownloadItem(item, true, null);
                return;
            }

            // 标记为进行中
            item.Status = DownloadStatus.InProgress;
            bool success = false;
            Exception error = null;

            try
            {
                // 创建下载配置
                var downloadOpt = new DownloadConfiguration
                {
                    ParallelDownload = item.EnableParallel,
                    ChunkCount = ConfigStore.DownloadChunkCount,
                };

                // 设置UA
                downloadOpt.RequestConfiguration.UserAgent = GetUserAgent(item.UAMode);

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

                // 如果非取消状态，尝试备用下载
                if (!IsCancellingOrCancelled(item.Status))
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
                // 完成下载，更新最终状态和触发事件
                CompleteDownloadItem(item, success, error);
                // 处理下载结果和重试逻辑
                if (!success && !IsCancellingOrCancelled(item.Status) && item.RetryCount > 0)
                {
                    await RetryDownloadAsync(item);
                }
            }
        }

        /// <summary>
        /// 完成下载项并更新状态
        /// </summary>
        private void CompleteDownloadItem(DownloadItem item, bool success, Exception error)
        {
            // 更新状态
            if (IsCancellingOrCancelled(item.Status))
            {
                item.Status = DownloadStatus.Cancelled;
            }
            else
            {
                item.Status = success ? DownloadStatus.Completed : DownloadStatus.Failed;
                if (!success && error != null)
                {
                    item.ErrorMessage = error.Message;
                }
            }

            // 触发项完成事件
            DownloadItemCompleted?.Invoke(item.GroupId, item.ItemId, success, error);

            // 清理资源
            CleanupDownloader(item.ItemId);
        }

        /// <summary>
        /// 重试下载
        /// </summary>
        private async Task RetryDownloadAsync(DownloadItem item)
        {
            item.Status = DownloadStatus.Retrying;
            item.RetryCount--;

            await Task.Delay(1500);

            // 清理可能的不完整文件
            TryDeleteFile(Path.Combine(item.DownloadPath, item.Filename));

            // 延迟后重试
            await Task.Delay(1500);

            await DownloadItemAsync(item.ItemId);
        }

        /// <summary>
        /// 清理下载器资源
        /// </summary>
        private void CleanupDownloader(string itemId)
        {
            if (_downloaders.TryRemove(itemId, out var downloader))
            {
                downloader.Dispose();
            }
        }

        /// <summary>
        /// 检查状态是否为取消中或已取消
        /// </summary>
        private bool IsCancellingOrCancelled(DownloadStatus status)
        {
            return status == DownloadStatus.Cancelling || status == DownloadStatus.Cancelled;
        }

        /// <summary>
        /// 尝试删除文件，忽略异常
        /// </summary>
        private bool TryDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            catch { return false; }
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

        /// <summary>
        /// 处理下载完成事件
        /// </summary>
        private void OnDownloadFileCompleted(string itemId, System.ComponentModel.AsyncCompletedEventArgs e, TaskCompletionSource<bool> completionSource)
        {
            if (!_downloadItems.TryGetValue(itemId, out var item))
            {
                completionSource.SetResult(false);
                return;
            }

            string filePath = Path.Combine(item.DownloadPath, item.Filename);

            // 1. 检查是否取消
            if (e.Cancelled)
            {
                item.Status = DownloadStatus.Cancelled;
                TryDeleteFile(filePath);
                completionSource.SetResult(false);
                return;
            }

            // 2. 检查是否有错误
            if (e.Error != null)
            {
                item.ErrorMessage = e.Error.Message;
                completionSource.SetResult(false);
                return;
            }

            // 3. 检查文件是否存在
            if (!File.Exists(filePath))
            {
                item.ErrorMessage = "File not found after download";
                completionSource.SetResult(false);
                return;
            }

            // 4. 验证SHA256（如果需要）
            if (!string.IsNullOrEmpty(item.ExpectedSha256))
            {
                if (!VerifyFileSHA256(filePath, item.ExpectedSha256))
                {
                    item.ErrorMessage = "SHA256 verification failed";
                    TryDeleteFile(filePath);
                    completionSource.SetResult(false);
                    return;
                }
            }

            // 文件下载成功
            completionSource.SetResult(true);
        }

        /// <summary>
        /// 备用下载方法
        /// </summary>
        private async Task<bool> FallbackDownloadAsync(DownloadItem item)
        {
            string filePath = Path.Combine(item.DownloadPath, item.Filename);

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(item.Url);
                request.UserAgent = GetUserAgent(item.UAMode);

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    long totalBytes = response.ContentLength;
                    item.Progress.TotalBytes = totalBytes;

                    using (Stream responseStream = response.GetResponseStream())
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long totalDownloadedByte = 0;
                        DateTime lastProgressUpdate = DateTime.Now;
                        long lastBytesReceived = 0;

                        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            if (IsCancellingOrCancelled(item.Status))
                            {
                                fileStream.Close();
                                TryDeleteFile(filePath);
                                return false;
                            }

                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalDownloadedByte += bytesRead;

                            // 更新进度，但不要过于频繁
                            TimeSpan elapsed = DateTime.Now - lastProgressUpdate;
                            if (elapsed.TotalMilliseconds > 500)
                            {
                                UpdateFallbackProgress(item, totalDownloadedByte, lastBytesReceived, elapsed, totalBytes);
                                lastProgressUpdate = DateTime.Now;
                                lastBytesReceived = totalDownloadedByte;
                            }
                        }
                    }
                }

                // 验证文件完整性（如果需要）
                if (!string.IsNullOrEmpty(item.ExpectedSha256) && !VerifyFileSHA256(filePath, item.ExpectedSha256))
                {
                    item.ErrorMessage = "SHA256 verification failed";
                    TryDeleteFile(filePath);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                item.ErrorMessage = ex.Message;
                TryDeleteFile(filePath);
                return false;
            }
        }

        /// <summary>
        /// 更新备用下载的进度信息
        /// </summary>
        private void UpdateFallbackProgress(DownloadItem item, long totalDownloadedByte, long lastBytesReceived,
                                             TimeSpan elapsed, long totalBytes)
        {
            double bytesPerSecond = (totalDownloadedByte - lastBytesReceived) / elapsed.TotalSeconds;

            // 更新进度信息
            item.Progress.ReceivedBytes = totalDownloadedByte;
            item.Progress.ProgressPercentage = totalBytes > 0 ? (double)totalDownloadedByte * 100 / totalBytes : 0;
            item.Progress.BytesPerSecond = bytesPerSecond;

            // 触发进度变更事件
            DownloadItemProgressChanged?.Invoke(item.GroupId, item.ItemId, item.Progress);
        }

        /// <summary>
        /// 获取UA字符串
        /// </summary>
        private string GetUserAgent(DownloadUAMode uaMode)
        {
            return uaMode switch
            {
                DownloadUAMode.MSL => "MSLTeam-MSL/" + ConfigStore.MSLVersion + " (Downloader)",
                DownloadUAMode.Browser => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36",
                _ => null,
            };
        }

        /// <summary>
        /// 验证文件SHA256哈希值
        /// </summary>
        private bool VerifyFileSHA256(string filePath, string expectedHash)
        {
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    SHA256Managed sha = new SHA256Managed();
                    byte[] hash = sha.ComputeHash(stream);
                    string calculatedHash = BitConverter.ToString(hash).Replace("-", string.Empty);

                    return string.Equals(calculatedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
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
        Failed,
        Retrying
    }

    /// <summary>
    /// UAMode枚举
    /// </summary>
    public enum DownloadUAMode
    {
        None = 0,
        MSL = 1,
        Browser = 2
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
        public bool IsTempGroup { get; set; }
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
        public bool EnableParallel { get; set; }
        public DownloadUAMode UAMode { get; set; }
        public DownloadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public DownloadProgressInfo Progress { get; set; }
        public int RetryCount { get; set; }
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
