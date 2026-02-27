using System;
using System.Windows.Media;

namespace MSL.utils
{
    public class ConfigStore
    {
        public static string ApiLink { get; set; } = "https://api.mslmc.cn/v3";
        public static Version MSLVersion { get; set; }
        public static string DeviceID { get; set; }
        public static bool GetServerInfo { get; set; } = false;
        public static bool GetPlayerInfo { get; set; } = false;
        public static int DownloadChunkCount { get; set; } = 4;

        // public static bool IsOldVersion { get; set; } 旧版本会加一个叹号提示（现在暂没使用此功能），后续可能会用上此字段

        public class LogColor
        {
            public static Color INFO { get; set; } = Colors.Green;
            public static Color WARN { get; set; } = Colors.Orange;
            public static Color ERROR { get; set; } = Colors.Red;
            public static Color HIGHLIGHT { get; set; } = Colors.DeepSkyBlue;
        }
    }
}
