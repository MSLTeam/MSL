using MSL.utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static MSL.controls.dialogs.DownloadManagerControl;

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

        public void Dispose()
        {
            // 注销事件处理
            //_downloadManager.DownloadItemProgressChanged -= DownloadManager_DownloadItemProgressChanged;
            _downloadManager.DownloadItemCompleted -= DownloadManager_DownloadItemCompleted;
            _downloadManager.DownloadGroupCompleted -= DownloadManager_DownloadGroupCompleted;
            // 停止并清理定时器
            _uiUpdateTimer.Stop();
            _uiUpdateTimer.Tick -= UiUpdateTimer_Tick;
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
            Dispatcher.Invoke(_downloadItems.Clear);

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

        private void DownloadManager_DownloadItemCompleted(string groupId, string itemId, Exception error = null)
        {
            UpdateDownloadItemUI(_downloadManager.GetDownloadItem(itemId));

            var item = _downloadItems.FirstOrDefault(i => i.ItemId == itemId);
            // 完成后自动移除
            if (item.AutoRemove ||
                ((item.Status == DownloadStatus.Completed || item.Status == DownloadStatus.Cancelled) &&
                (AutoRemoveCompletedItems || item.AutoRemove)))
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
    }
}
