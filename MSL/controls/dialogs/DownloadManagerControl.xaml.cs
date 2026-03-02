using MSL.utils;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MSL.controls.dialogs
{
    public partial class DownloadManagerControl : UserControl
    {
        private readonly DownloadManager _downloadManager;
        private readonly ObservableCollection<DownloadItemViewModel> _downloadItems;
        private readonly DispatcherTimer _uiUpdateTimer;
        public bool AutoRemoveCompletedItems { get; set; } = false;

        // 记录当前"独占显示"的组ID，null表示不限制
        private string _exclusiveGroupId = null;

        public DownloadManagerControl()
        {
            InitializeComponent();

            _downloadManager = DownloadManager.Instance;
            _downloadItems = new ObservableCollection<DownloadItemViewModel>();
            DownloadItemsListView.ItemsSource = _downloadItems;

            _downloadManager.DownloadItemCompleted += DownloadManager_DownloadItemCompleted;
            _downloadManager.DownloadGroupCompleted += DownloadManager_DownloadGroupCompleted;

            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        }

        public void Dispose()
        {
            ClearAllItems();
            _downloadManager.DownloadItemCompleted -= DownloadManager_DownloadItemCompleted;
            _downloadManager.DownloadGroupCompleted -= DownloadManager_DownloadGroupCompleted;
            _uiUpdateTimer.Stop();
            _uiUpdateTimer.Tick -= UiUpdateTimer_Tick;
        }

        public void AddDownloadGroup(string groupId, bool updateExisting = false, bool autoRemove = false, bool onlyShowMe = false)
        {
            var items = _downloadManager.GetGroupItems(groupId);

            // 如果开启 onlyShowMe，先将所有非本组的项隐藏
            if (onlyShowMe)
            {
                _exclusiveGroupId = groupId;
                foreach (var existingVm in _downloadItems)
                {
                    if (existingVm.GroupId != groupId)
                        existingVm.IsVisible = false;
                }
            }

            foreach (var item in items)
            {
                var existingItem = _downloadItems.FirstOrDefault(vm => vm.ItemId == item.ItemId);

                if (existingItem != null)
                {
                    if (updateExisting)
                        UpdateDownloadItemUI(item);

                    // 确保本组项可见
                    if (onlyShowMe)
                        existingItem.IsVisible = true;
                }
                else
                {
                    var model = new DownloadItemViewModel(item)
                    {
                        AutoRemove = autoRemove,
                        // 若当前有其他独占组在显示，且新加的不是那个组，则隐藏；否则可见
                        IsVisible = (_exclusiveGroupId == null || _exclusiveGroupId == groupId)
                    };
                    _downloadItems.Add(model);
                }
            }

            if (_downloadItems.Count != 0)
                _uiUpdateTimer.Start();

            UpdateSummaryInfo();
        }

        /// <summary>
        /// 恢复所有项的可见性，取消独占显示模式
        /// </summary>
        public void ShowAllItems()
        {
            _exclusiveGroupId = null;
            foreach (var vm in _downloadItems)
                vm.IsVisible = true;

            UpdateSummaryInfo();
        }

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

        public void ClearAllItems()
        {
            _exclusiveGroupId = null;
            Dispatcher.Invoke(_downloadItems.Clear);
            UpdateSummaryInfo();
        }

        #region 事件处理方法

        private async void DownloadManager_DownloadItemCompleted(string groupId, string itemId, Exception error = null)
        {
            UpdateDownloadItemUI(_downloadManager.GetDownloadItem(itemId));

            var item = _downloadItems.FirstOrDefault(i => i.ItemId == itemId);
            if (item != null &&
                (item.Status == DownloadStatus.Completed || item.Status == DownloadStatus.Cancelled || item.Status == DownloadStatus.Failed) &&
                (AutoRemoveCompletedItems || item.AutoRemove))
            {
                await Task.Delay(1000);
                RemoveDownloadItem(itemId);
            }
        }

        private void DownloadManager_DownloadGroupCompleted(string groupId, bool allSuccess)
        {
            if (_downloadItems.Count == 0)
                _uiUpdateTimer.Stop();

            // 若完成的组正是当前独占组，自动恢复显示所有项
            if (_exclusiveGroupId == groupId)
                ShowAllItems();

            UpdateSummaryInfo();
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            foreach (var viewModel in _downloadItems)
            {
                var item = _downloadManager.GetDownloadItem(viewModel.ItemId);
                if (item != null)
                    viewModel.UpdateFromModel(item);
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
                        _downloadManager.PauseDownloadItem(itemId);
                    else if (item.Status == DownloadStatus.Paused)
                        _downloadManager.ResumeDownloadItem(itemId);

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
                    _downloadManager.PauseDownloadItem(viewModel.ItemId);
            }
        }

        private void ResumeAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var viewModel in _downloadItems)
            {
                if (viewModel.Status == DownloadStatus.Paused)
                    _downloadManager.ResumeDownloadItem(viewModel.ItemId);
            }
        }

        private void CancelAllButton_Click(object sender, RoutedEventArgs e)
        {
            var groups = _downloadManager.GetAllGroups()
                .Where(g => g.Status == DownloadGroupStatus.InProgress)
                .Select(g => g.GroupId)
                .ToList();

            foreach (var groupId in groups)
                _downloadManager.CancelDownloadGroup(groupId);
        }

        private void RemoveCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            var items = _downloadManager.GetAllItems()
                .Where(i => i.Status == DownloadStatus.Cancelled || i.Status == DownloadStatus.Completed)
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
                    viewModel.UpdateFromModel(item);
                else
                    _downloadItems.Add(new DownloadItemViewModel(item));
            });
        }

        private void UpdateSummaryInfo()
        {
            Dispatcher.Invoke(() =>
            {
                // 只统计当前可见项中的活跃下载
                int activeDownloads = _downloadItems.Count(item =>
                    item.IsVisible &&
                    (item.Status == DownloadStatus.InProgress || item.Status == DownloadStatus.Pending));

                ActiveDownloadsTextBlock.Text = activeDownloads.ToString();
            });
        }

        #endregion

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
            private bool _autoRemove;
            private bool _isVisible = true;

            public bool IsVisible
            {
                get => _isVisible;
                set
                {
                    if (_isVisible == value) return;
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                    OnPropertyChanged(nameof(ItemVisibility));  // 同时通知Visibility转换属性
                }
            }

            // 供XAML直接绑定的Visibility属性（省去Converter）
            public Visibility ItemVisibility => _isVisible ? Visibility.Visible : Visibility.Collapsed;

            public string ItemId
            {
                get => _itemId;
                set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
            }

            public string GroupId
            {
                get => _groupId;
                set { _groupId = value; OnPropertyChanged(nameof(GroupId)); }
            }

            public string Url
            {
                get => _url;
                set { _url = value; OnPropertyChanged(nameof(Url)); }
            }

            public string Filename
            {
                get => _filename;
                set { _filename = value; OnPropertyChanged(nameof(Filename)); }
            }

            public DownloadStatus Status
            {
                get => _status;
                set
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    StatusText = GetStatusText(_status);
                    PauseResumeButtonText = (_status == DownloadStatus.InProgress) ? "暂停" :
                                           (_status == DownloadStatus.Paused) ? "继续" : "---";
                    CancelRemoveButtonText = (_status == DownloadStatus.Cancelled || _status == DownloadStatus.Completed || _status == DownloadStatus.Failed) ? "移除" :
                                           (_status == DownloadStatus.InProgress || _status == DownloadStatus.Paused) ? "取消" : "---";
                }
            }

            public bool AutoRemove
            {
                get => _autoRemove;
                set { _autoRemove = value; OnPropertyChanged(nameof(AutoRemove)); }
            }

            public string ErrorMessage
            {
                get => _errorMessage;
                set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
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
                set { _speedText = value; OnPropertyChanged(nameof(SpeedText)); }
            }

            public string RemainingText
            {
                get => _remainingText;
                set { _remainingText = value; OnPropertyChanged(nameof(RemainingText)); }
            }

            public string StatusText
            {
                get => _statusText;
                set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
            }

            public string PauseResumeButtonText
            {
                get => _pauseResumeButtonText;
                set { _pauseResumeButtonText = value; OnPropertyChanged(nameof(PauseResumeButtonText)); }
            }

            public string CancelRemoveButtonText
            {
                get => _cancelRemoveButtonText;
                set { _cancelRemoveButtonText = value; OnPropertyChanged(nameof(CancelRemoveButtonText)); }
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
                SpeedText = FormatBytesPerSecond(Progress.BytesPerSecond);
                RemainingText = FormatTimeRemaining(Progress.EstimatedTimeRemaining);
            }

            private string GetStatusText(DownloadStatus status)
            {
                switch (status)
                {
                    case DownloadStatus.Pending: return "等待中";
                    case DownloadStatus.InProgress: return "下载中";
                    case DownloadStatus.Paused: return "已暂停";
                    case DownloadStatus.Cancelling: return "取消中";
                    case DownloadStatus.Cancelled: return "已取消";
                    case DownloadStatus.Completed: return "已完成";
                    case DownloadStatus.Failed: return $"失败: {ErrorMessage}";
                    case DownloadStatus.Retrying: return $"失败，将重试: {ErrorMessage}";
                    default: return status.ToString();
                }
            }

            private string FormatBytesPerSecond(double bytesPerSecond)
            {
                if (bytesPerSecond <= 0) return "---";
                string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
                double speed = bytesPerSecond;
                int order = 0;
                while (speed >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    speed /= 1024;
                }
                return $"{speed:0.##} {sizes[order]}";
            }

            private string FormatTimeRemaining(TimeSpan timeSpan)
            {
                if (timeSpan.TotalSeconds < 1) return "---";
                if (timeSpan.TotalHours >= 1) return $"{(int)timeSpan.TotalHours}时{timeSpan.Minutes}分";
                if (timeSpan.TotalMinutes >= 1) return $"{timeSpan.Minutes}分{timeSpan.Seconds}秒";
                return $"{timeSpan.Seconds}秒";
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}