using MSL.utils;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MSL.controls.dialogs
{
    /// <summary>
    /// DownloadDisplayControl.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadDisplayControl : UserControl
    {
        private readonly DownloadManager _downloadManager;
        private readonly ObservableCollection<DownloadItemViewModel> _downloadItems;
        private readonly DispatcherTimer _uiUpdateTimer;
        public bool AutoRemoveCompletedItems { get; set; } = true; // 是否自动移除已完成的下载项

        public DownloadDisplayControl()
        {
            InitializeComponent();

            // 获取下载管理器实例
            _downloadManager = DownloadManager.Instance;

            // 初始化可观察集合
            _downloadItems = new ObservableCollection<DownloadItemViewModel>();
            DownloadItemsListView.ItemsSource = _downloadItems;

            // 注册事件处理
            //_downloadManager.DownloadItemProgressChanged += DownloadManager_DownloadItemProgressChanged;
            _downloadManager.DownloadItemCompleted += DownloadManager_DownloadItemCompleted;
            _downloadManager.DownloadGroupCompleted += DownloadManager_DownloadGroupCompleted;

            // 创建UI更新定时器（每秒更新一次UI）
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            // _uiUpdateTimer.Start();
        }

        // 添加新的下载组和项到UI
        public void AddDownloadGroup(string groupId, bool updateExisting = false)
        {
            var items = _downloadManager.GetGroupItems(groupId);

            foreach (var item in items)
            {
                // 检查是否已存在
                var existingItem = _downloadItems.FirstOrDefault(vm => vm.ItemId == item.ItemId);

                if (existingItem != null)
                {
                    if (updateExisting)
                    {
                        // 更新现有项
                        UpdateDownloadItemUI(item);
                    }
                }
                else
                {
                    // 添加新项
                    Dispatcher.Invoke(() =>
                    {
                        _downloadItems.Add(new DownloadItemViewModel(item));
                    });
                }
            }
            if(_downloadItems.Count!=0)
                _uiUpdateTimer.Start(); // 启动定时器
            UpdateSummaryInfo();
        }

        // 移除下载项
        public void RemoveDownloadItem(string itemId)
        {
            var item = _downloadItems.FirstOrDefault(i => i.ItemId == itemId);
            if (item != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _downloadItems.Remove(item);
                });
            }

            UpdateSummaryInfo();
        }

        // 清除所有下载项
        public void ClearAllItems()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _downloadItems.Clear();
            });

            UpdateSummaryInfo();
        }

        #region 事件处理方法
        /*
        private int uiChangeCounter = 0; // UI更新计数器
        private void DownloadManager_DownloadItemProgressChanged(string groupId, string itemId, DownloadProgressInfo progressInfo)
        {
            // 避免频繁更新UI
            if (uiChangeCounter > 0)
            {
                uiChangeCounter--;
                return;
            }
            UpdateDownloadItemUI(_downloadManager.GetDownloadItem(itemId));
            uiChangeCounter = 512;
        }
        */

        private void DownloadManager_DownloadItemCompleted(string groupId, string itemId, bool success, Exception error = null)
        {
            UpdateDownloadItemUI(_downloadManager.GetDownloadItem(itemId));

            // 完成后自动移除
            if (success && AutoRemoveCompletedItems)
            {
                RemoveDownloadItem(itemId);
            }
        }

        private void DownloadManager_DownloadGroupCompleted(string groupId, bool allSuccess)
        {
            if (_downloadItems.Count == 0)
                _uiUpdateTimer.Stop(); // 停止定时器
            UpdateSummaryInfo();
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            // 更新所有下载项的UI显示（主要是剩余时间计算）
            foreach (var viewModel in _downloadItems)
            {
                var item = _downloadManager.GetDownloadItem(viewModel.ItemId);
                if (item != null)
                {
                    viewModel.UpdateFromModel(item);
                }
            }

            UpdateSummaryInfo();
        }

        #endregion

        #region 辅助方法

        private void UpdateDownloadItemUI(DownloadItem item)
        {
            if (item == null) return;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                var viewModel = _downloadItems.FirstOrDefault(vm => vm.ItemId == item.ItemId);

                if (viewModel != null)
                {
                    viewModel.UpdateFromModel(item);
                }
                else
                {
                    _downloadItems.Add(new DownloadItemViewModel(item));
                }
            });
        }

        private void UpdateSummaryInfo()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int activeDownloads = _downloadItems.Count(item =>
                    item.Status == DownloadStatus.InProgress ||
                    item.Status == DownloadStatus.Pending);

                ActiveDownloadsTextBlock.Text = activeDownloads.ToString();
            });
        }

        #endregion

        // 下载项视图模型（支持数据绑定）
        public class DownloadItemViewModel : INotifyPropertyChanged
        {
            private string _itemId;
            private string _groupId;
            private string _url;
            private string _filename;
            private DownloadStatus _status;
            private string _errorMessage;
            private DownloadProgressInfo _progress;
            private string _progressText;
            private string _speedText;
            private string _remainingText;
            private string _statusText;
            private string _pauseResumeButtonText;

            public string ItemId
            {
                get => _itemId;
                set
                {
                    _itemId = value;
                    OnPropertyChanged(nameof(ItemId));
                }
            }

            public string GroupId
            {
                get => _groupId;
                set
                {
                    _groupId = value;
                    OnPropertyChanged(nameof(GroupId));
                }
            }

            public string Url
            {
                get => _url;
                set
                {
                    _url = value;
                    OnPropertyChanged(nameof(Url));
                }
            }

            public string Filename
            {
                get => _filename;
                set
                {
                    _filename = value;
                    OnPropertyChanged(nameof(Filename));
                }
            }

            public DownloadStatus Status
            {
                get => _status;
                set
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));

                    // 更新状态文本
                    StatusText = GetStatusText(_status);

                    // 更新按钮文本
                    PauseResumeButtonText = (_status == DownloadStatus.InProgress) ? "暂停" :
                                           (_status == DownloadStatus.Paused) ? "继续" : "---";
                }
            }

            public string ErrorMessage
            {
                get => _errorMessage;
                set
                {
                    _errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                }
            }

            public DownloadProgressInfo Progress
            {
                get => _progress;
                set
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));

                    UpdateDerivedProperties();
                }
            }

            public string ProgressText
            {
                get => _progressText;
                set
                {
                    _progressText = value;
                    OnPropertyChanged(nameof(ProgressText));
                }
            }

            public string SpeedText
            {
                get => _speedText;
                set
                {
                    _speedText = value;
                    OnPropertyChanged(nameof(SpeedText));
                }
            }

            public string RemainingText
            {
                get => _remainingText;
                set
                {
                    _remainingText = value;
                    OnPropertyChanged(nameof(RemainingText));
                }
            }

            public string StatusText
            {
                get => _statusText;
                set
                {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }

            public string PauseResumeButtonText
            {
                get => _pauseResumeButtonText;
                set
                {
                    _pauseResumeButtonText = value;
                    OnPropertyChanged(nameof(PauseResumeButtonText));
                }
            }

            public DownloadItemViewModel(DownloadItem item)
            {
                UpdateFromModel(item);
            }

            public void UpdateFromModel(DownloadItem item)
            {
                ItemId = item.ItemId;
                GroupId = item.GroupId;
                Url = item.Url;
                Filename = item.Filename;
                Status = item.Status;
                ErrorMessage = item.ErrorMessage;
                Progress = item.Progress;
            }

            private void UpdateDerivedProperties()
            {
                if (Progress == null) return;

                // 格式化进度文本（已下载/总大小）
                ProgressText = FormatBytes(Progress.ReceivedBytes) + " / " + FormatBytes(Progress.TotalBytes);

                // 格式化速度文本
                SpeedText = FormatBytesPerSecond(Progress.BytesPerSecond);

                // 格式化剩余时间文本
                RemainingText = FormatTimeRemaining(Progress.EstimatedTimeRemaining);
            }

            private string GetStatusText(DownloadStatus status)
            {
                switch (status)
                {
                    case DownloadStatus.Pending:
                        return "等待中";
                    case DownloadStatus.InProgress:
                        return "下载中";
                    case DownloadStatus.Paused:
                        return "已暂停";
                    case DownloadStatus.Cancelling:
                        return "取消中";
                    case DownloadStatus.Cancelled:
                        return "已取消";
                    case DownloadStatus.Completed:
                        return "已完成";
                    case DownloadStatus.Failed:
                        return $"失败: {ErrorMessage}";
                    case DownloadStatus.Retrying:
                        return $"失败，将重试: {ErrorMessage}";
                    default:
                        return status.ToString();
                }
            }

            private string FormatBytes(long bytes)
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = bytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            }

            private string FormatBytesPerSecond(double bytesPerSecond)
            {
                if (bytesPerSecond <= 0)
                    return "---";

                string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
                double speed = bytesPerSecond;
                int order = 0;

                while (speed >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    speed = speed / 1024;
                }

                return $"{speed:0.##} {sizes[order]}";
            }

            private string FormatTimeRemaining(TimeSpan timeSpan)
            {
                if (timeSpan.TotalSeconds < 1)
                    return "---";

                if (timeSpan.TotalHours >= 1)
                    return $"{(int)timeSpan.TotalHours}时{timeSpan.Minutes}分";

                if (timeSpan.TotalMinutes >= 1)
                    return $"{timeSpan.Minutes}分{timeSpan.Seconds}秒";

                return $"{timeSpan.Seconds}秒";
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
