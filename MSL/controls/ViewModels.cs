using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    internal class TabControlHeader
    {
        public string Icon { get; set; }
        public string Text { get; set; }
    }
}
