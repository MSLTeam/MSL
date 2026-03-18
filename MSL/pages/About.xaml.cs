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

namespace MSL.pages
{
    public partial class About : Page
    {
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

        public About()
        {
            InitializeComponent();
            StarsItemsControl.ItemsSource = _displayStars;
            ContributorsItemsControl.ItemsSource = _contributors;
            this.Unloaded += About_Unloaded;
        }

        private void About_Unloaded(object sender, RoutedEventArgs e)
        {
            _displayStars.Clear();
            _allStars.Clear();
            _contributors.Clear();

            // 清除 WPF 图片缓存
            BitmapImage dummy = new BitmapImage();
            dummy.BeginInit();
            dummy.UriSource = new Uri("https://placeholder.invalid/clear");
            dummy.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            dummy.CacheOption = BitmapCacheOption.None;
            // 不 EndInit，直接丢弃，只是为了触发缓存清理时机

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            AbortSoftwareCard.Title = string.Format(
                LanguageManager.Instance["Page_About_AboutMSL"],
                ConfigStore.MSLVersion.ToString());

            LoadContributorsData();
            LoadStarsDataAsync();
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

        private async void LoadStarsDataAsync()
        {
            try
            {
                HttpResponse response = await HttpService.GetApiAsync("/stat/stars?project=MSL&count=300");
                if (response.HttpResponseCode == HttpStatusCode.OK)
                {
                    JObject json = JObject.Parse(response.HttpResponseContent.ToString());
                    if ((int)json["code"] == 200)
                    {
                        var dataArray = json["data"]["data"] as JArray;
                        if (dataArray != null)
                        {
                            foreach (var item in dataArray)
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
                }
            }
            catch (Exception) { }
        }

        private async void BtnLoadMoreStars_Click(object sender, RoutedEventArgs e)
        {
            BtnLoadMoreStars.Visibility = Visibility.Collapsed;
            int currentCount = _displayStars.Count;
            int totalCount = _allStars.Count;

            for (int i = currentCount; i < totalCount; i++)
            {
                _displayStars.Add(_allStars[i]);
                if (i % 20 == 0)
                    await System.Threading.Tasks.Task.Delay(1);
            }
        }
    }
}