using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using MSL.utils;
using MSL.langs;
using System.Diagnostics;

namespace MSL.pages
{
    public class SponsorItem
    {
        public string Name { get; set; }
        public string Avatar { get; set; }
        public decimal Amount { get; set; }
        public long Time { get; set; }
        public string Source { get; set; }
        public string Slogan { get; set; }
        public bool IsGold { get; set; }

        public string DisplayAmount => $"¥{Amount:0.##}";
        public bool HasSlogan => IsGold && !string.IsNullOrWhiteSpace(Slogan);
        public Visibility SloganVisibility => HasSlogan ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RoleTextVisibility => !HasSlogan ? Visibility.Visible : Visibility.Collapsed;
        public string RoleText => IsGold ? "金牌赞助者" : "赞助者";
    }

    public class SponsorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GoldTemplate { get; set; }
        public DataTemplate NormalTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SponsorItem sponsor && sponsor.IsGold)
                return GoldTemplate;
            return NormalTemplate;
        }
    }

    public partial class About : Page
    {
        private bool isInit = false;

        public class Stargazer
        {
            public string User { get; set; }
            public string AvatarUrl { get; set; }
        }

        public class Contributor
        {
            public string User { get; set; }
            public string AvatarUrl { get; set; }
            public string Description { get; set; }
        }

        private List<Stargazer> _allStars = new List<Stargazer>();
        private ObservableCollection<Stargazer> _displayStars = new ObservableCollection<Stargazer>();
        private ObservableCollection<Contributor> _contributors = new ObservableCollection<Contributor>();
        private ObservableCollection<SponsorItem> _sponsors = new ObservableCollection<SponsorItem>();

        public About()
        {
            InitializeComponent();
            StarsItemsControl.ItemsSource = _displayStars;
            ContributorsItemsControl.ItemsSource = _contributors;
            SponsorsItemsControl.ItemsSource = _sponsors;
            this.Unloaded += About_Unloaded;
        }

        private void About_Unloaded(object sender, RoutedEventArgs e)
        {
            _displayStars.Clear();
            _allStars.Clear();
            _contributors.Clear();
            _sponsors.Clear();

            // 清除 WPF 图片缓存
            BitmapImage dummy = new BitmapImage();
            dummy.BeginInit();
            dummy.UriSource = null;
            dummy.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            dummy.CacheOption = BitmapCacheOption.None;
            // 不 EndInit，直接丢弃，只是为了触发缓存清理时机

            GC.Collect();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInit)
            {
                LoadSponsorsDataAsync();
                return;
            }
            isInit = true;
            LoadContributorsData();
            LoadSponsorsDataAsync();
            AbortSoftwareCard.Title = string.Format(
                LanguageManager.Instance["Page_About_AboutMSL"],
                ConfigStore.MSLVersion.ToString());
            // 致谢与支持卡片已完全隐藏，不再请求 Stars API（保留下方相关方法与数据结构）
        }

        private void LoadContributorsData()
        {
            _contributors.Add(new Contributor
            {
                User = "Weheal",
                AvatarUrl = "https://avatars.githubusercontent.com/u/77955152?v=4",
                Description = "🌟MSL开发者/创始人"
            });
            _contributors.Add(new Contributor
            {
                User = "xiaoyu",
                AvatarUrl = "https://avatars.githubusercontent.com/u/58876608?v=4",
                Description = "🌟MSL开发者"
            });
            _contributors.Add(new Contributor
            {
                User = "LxHTT",
                AvatarUrl = "https://avatars.githubusercontent.com/u/98154001?v=4",
                Description = "ME Frp 部分代码 & Java扫描算法"
            });
        }

        private JArray StarUserInfo;

        private async void LoadStarsDataAsync(bool isIniting=false)
        {
            try
            {
                if (isIniting)
                {
                    HttpResponse response = await HttpService.GetApiAsync("/stat/stars?project=MSL&count=100");
                    if (response.HttpResponseCode == HttpStatusCode.OK)
                    {
                        JObject json = JObject.Parse(response.HttpResponseContent.ToString());
                        if ((int)json["code"] == 200)
                        {
                            StarUserInfo = json["data"]["data"] as JArray;

                        }
                    }
                    else
                    {
                        return;
                    }
                }
                if (StarUserInfo != null)
                {
                    foreach (var item in StarUserInfo)
                    {
                        _allStars.Add(new Stargazer
                        {
                            User = item["user"].ToString(),
                            AvatarUrl = item["avatar"].ToString()
                        });
                    }
                }

                int loadCount = Math.Min(50, _allStars.Count);
                for (int i = 0; i < loadCount; i++)
                    _displayStars.Add(_allStars[i]);

                if (_allStars.Count > 50)
                    BtnLoadMoreStars.Visibility = Visibility.Visible;
            }
            catch (Exception) { }
        }

        private async void BtnLoadMoreStars_Click(object sender, RoutedEventArgs e)
        {
            /*
            BtnLoadMoreStars.Visibility = Visibility.Collapsed;
            int currentCount = _displayStars.Count;
            int totalCount = _allStars.Count;

            for (int i = currentCount; i < totalCount; i++)
            {
                _displayStars.Add(_allStars[i]);
                if (i % 20 == 0)
                    await System.Threading.Tasks.Task.Delay(1);
            }
            */
            Process.Start("https://github.com/MSLTeam/MSL/");
        }

        private async void LoadSponsorsDataAsync()
        {
            try
            {
                HttpResponse response = await HttpService.GetAsync("https://user.mslmc.net/api/sponsor/public", headerUAMode: 2);
                if (response.HttpResponseCode == HttpStatusCode.OK && response.HttpResponseContent != null)
                {
                    JObject json = JObject.Parse(response.HttpResponseContent.ToString());
                    if ((int)json["code"] == 200 && json["data"] != null)
                    {
                        _sponsors.Clear();
                        var data = json["data"];
                        var goldList = data["goldSponsors"] as JArray;
                        if (goldList != null)
                        {
                            foreach (var item in goldList)
                            {
                                _sponsors.Add(new SponsorItem
                                {
                                    Name = item["name"]?.ToString().Trim() ?? "",
                                    Avatar = item["avatar"]?.ToString() ?? "",
                                    Amount = item["amount"] != null ? (decimal)item["amount"] : 0m,
                                    Time = item["time"] != null ? (long)item["time"] : 0L,
                                    Source = item["source"]?.ToString() ?? "",
                                    Slogan = item["slogan"]?.ToString() ?? "",
                                    IsGold = true
                                });
                            }
                        }
                        var normalList = data["normalSponsors"] as JArray;
                        if (normalList != null)
                        {
                            foreach (var item in normalList)
                            {
                                _sponsors.Add(new SponsorItem
                                {
                                    Name = item["name"]?.ToString().Trim() ?? "",
                                    Avatar = item["avatar"]?.ToString() ?? "",
                                    Amount = item["amount"] != null ? (decimal)item["amount"] : 0m,
                                    Time = item["time"] != null ? (long)item["time"] : 0L,
                                    Source = item["source"]?.ToString() ?? "",
                                    Slogan = "",
                                    IsGold = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"获取赞助者列表异常: {ex.Message}");
            }
        }

        private void BtnAfdian_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://afdian.com/a/mslteam") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"打开爱发电链接失败: {ex.Message}");
            }
        }
    }
}
