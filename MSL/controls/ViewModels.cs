using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSL.controls
{
    internal class ServerInfo
    {
        public string ServerName { get; set; }
        public string ServerIcon { get; set; }
        public string ServerState { get; set; }
        public Brush ServerStateFore { get; set; }
        public ServerInfo(string serverName, string serverIcon, string serverState, Brush serverStateFore)
        {
            ServerName = serverName;
            ServerIcon = serverIcon;
            ServerState = serverState;
            ServerStateFore = serverStateFore;
        }
    }

    internal class PluginInfo
    {
        public string PluginName { get; set; }
        public PluginInfo(string pluginName)
        {
            PluginName = pluginName;
        }
    }

    internal class ModInfo
    {
        public string ModName { get; set; }
        public ModInfo(string modName)
        {
            ModName = modName;
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
