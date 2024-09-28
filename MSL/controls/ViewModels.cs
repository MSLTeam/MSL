using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSL.controls
{
    internal class SL_ServerInfo
    {
        public int ServerID { get; set; }
        public string ServerName { get; set; }
        public string ServerIcon { get; set; }
        public string ServerState { get; set; }
        public Brush ServerStateFore { get; set; }
        public SL_ServerInfo(int serverID, string serverName, string serverIcon, string serverState, Brush serverStateFore)
        {
            ServerID = serverID;
            ServerName = serverName;
            ServerIcon = serverIcon;
            ServerState = serverState;
            ServerStateFore = serverStateFore;
        }
    }

    internal class SR_PluginInfo
    {
        public string PluginName { get; set; }
        public SR_PluginInfo(string pluginName)
        {
            PluginName = pluginName;
        }
    }

    internal class SR_ModInfo
    {
        public string ModName { get; set; }
        public SR_ModInfo(string modName)
        {
            ModName = modName;
        }
    }

    internal class DM_ModsInfo
    {
        public string ID { set; get; }
        public string Icon { set; get; }
        public string Name { set; get; }
        public string WebsiteUrl { set; get; }

        public DM_ModsInfo(string id, string icon, string name, string websiteurl)
        {
            ID = id;
            Icon = icon;
            Name = name;
            WebsiteUrl = websiteurl;
        }
    }

    internal class DM_ModInfo
    {
        public string Icon { set; get; }
        public string Name { set; get; }
        public string DownloadUrl { set; get; }
        public string FileName { set; get; }
        public string Platform { set; get; }
        public string Dependency { set; get; }
        public string MCVersion { set; get; }

        public DM_ModInfo(string icon, string name, string downloadurl, string filename, string platform, string dependency, string mcversion)
        {
            Icon = icon;
            Name = name;
            DownloadUrl = downloadurl;
            FileName = filename;
            Platform = platform;
            Dependency = dependency;
            MCVersion = mcversion;
        }
    }

    internal class ListBoxSideMenu : Control
    {
        public ImageSource Icon { get; set; }
        public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(ListBoxSideMenu),
            new PropertyMetadata(default(string)));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
    }

    internal class TabControlHeader : Control
    {
        public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            "Icon", typeof(ImageSource),
            typeof(TabControlHeader),
            new PropertyMetadata(default(ImageSource)));

        public ImageSource Icon
        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public string Text { get; set; }
    }
}
