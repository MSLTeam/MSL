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
    /// DownloadManagerControl.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadManagerControl : UserControl
    {
        private readonly DownloadManager _downloadManager;
        private readonly ObservableCollection<DownloadItemViewModel> _downloadItems;
        private readonly DispatcherTimer _uiUpdateTimer;
        public bool AutoRemoveCompletedItems { get; set; } = true;

        public DownloadManagerControl()
        {
            InitializeComponent();

            // 获取下载管理器实例
            _downloadManager = DownloadManager.Instance;

            // 初始化可观察集合
            _downloadItems = new ObservableCollection<DownloadItemViewModel>();
            DownloadItemsListView.ItemsSource = _downloadItems;

            // 注册事件处理
            // _downloadManager.DownloadItemProgressChanged += DownloadManager_DownloadItemProgressChanged;
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
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _downloadItems.Add(new DownloadItemViewModel(item));
                    });
                }
            }
            if (_downloadItems.Count != 0)
                _uiUpdateTimer.Start(); // 启动定时器
            UpdateSummaryInfo();
        }

        // 移除下载项
        public void RemoveDownloadItem(string itemId)
        {
            var item = _downloadItems.FirstOrDefault(i => i.ItemId == itemId);
            if (item != null)
            {
                Dispatcher.Invoke(() =>
                {
                    _downloadItems.Remove(item);
                });
            }

            UpdateSummaryInfo();
        }

        // 清除所有下载项
        public void ClearAllItems()
        {
            Dispatcher.Invoke(() =>
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
            // 更新UI时避免频繁调用
            if (uiChangeCounter > 0)
            {
                uiChangeCounter--;
                return;
            }
            UpdateDownloadItemUI(_downloadManager.GetDownloadItem(itemId));
            uiChangeCounter = 512; // 设置下次更新的间隔
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

        #region UI操作处理

        private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string itemId)
            {
                var item = _downloadManager.GetDownloadItem(itemId);
                if (item != null)
                {
                    if (item.Status == DownloadStatus.InProgress)
                    {
                        _downloadManager.PauseDownloadItem(itemId);
                    }
                    else if (item.Status == DownloadStatus.Paused)
                    {
                        _downloadManager.ResumeDownloadItem(itemId);
                    }

                    UpdateDownloadItemUI(item);
                }
            }
        }

        private void CancelRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string itemId)
            {
                var item = _downloadManager.GetDownloadItem(itemId);
                if (item != null)
                {
                    if (item.Status == DownloadStatus.InProgress || item.Status == DownloadStatus.Paused)
                    {
                        _downloadManager.CancelDownloadItem(itemId);
                        UpdateDownloadItemUI(item);
                    }
                    else if (item.Status == DownloadStatus.Cancelled || item.Status == DownloadStatus.Completed || item.Status == DownloadStatus.Failed)
                    {
                        RemoveDownloadItem(itemId);
                    }
                }
            }
        }

        private void PauseAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var viewModel in _downloadItems)
            {
                if (viewModel.Status == DownloadStatus.InProgress)
                {
                    _downloadManager.PauseDownloadItem(viewModel.ItemId);
                }
            }
        }

        private void ResumeAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var viewModel in _downloadItems)
            {
                if (viewModel.Status == DownloadStatus.Paused)
                {
                    _downloadManager.ResumeDownloadItem(viewModel.ItemId);
                }
            }
        }

        private void CancelAllButton_Click(object sender, RoutedEventArgs e)
        {
            var groups = _downloadManager.GetAllGroups()
                .Where(g => g.Status == DownloadGroupStatus.InProgress)
                .Select(g => g.GroupId)
                .ToList();

            foreach (var groupId in groups)
            {
                _downloadManager.CancelDownloadGroup(groupId);
            }
        }

        private void RemoveCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            var items = _downloadManager.GetAllItems()
                .Where(i => (i.Status == DownloadStatus.Cancelled || i.Status == DownloadStatus.Completed))
                .Select(i => i.ItemId)
                .ToList();

            foreach (var itemId in items)
            {
                var item = _downloadItems.FirstOrDefault(i => i.ItemId == itemId);
                if (item != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _downloadItems.Remove(item);
                    });
                }

            }
            UpdateSummaryInfo();
        }

        #endregion

        #region 辅助方法

        private void UpdateDownloadItemUI(DownloadItem item)
        {
            if (item == null) return;

            Dispatcher.Invoke(() =>
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
            Dispatcher.Invoke(() =>
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
            private string _speedText;
            private string _remainingText;
            private string _statusText;
            private string _pauseResumeButtonText;
            private string _cancelRemoveButtonText;

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

                    // 更新按钮文本
                    CancelRemoveButtonText = (_status == DownloadStatus.Cancelled || _status == DownloadStatus.Completed || _status == DownloadStatus.Failed) ? "移除" :
                                           (_status == DownloadStatus.InProgress || _status == DownloadStatus.Paused) ? "取消" : "---";
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

            public string CancelRemoveButtonText
            {
                get => _cancelRemoveButtonText;
                set
                {
                    _cancelRemoveButtonText = value;
                    OnPropertyChanged(nameof(CancelRemoveButtonText));
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
                if (Status == DownloadStatus.Completed)
                    item.Progress.ProgressPercentage = 100;
                Progress = item.Progress;
            }

            private void UpdateDerivedProperties()
            {
                if (Progress == null) return;


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
