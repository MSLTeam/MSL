using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MSL.utils.Config
{
    /// <summary>
    /// server.properties 配置预设（全局共享，所有服务器实例可见）
    /// </summary>
    public class ServerPropertiesPreset
    {
        /// <summary>预设名称（唯一标识）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>预设创建/更新时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>预设保存的配置键值对</summary>
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 配置预设管理器：负责预设列表的加载与持久化。
    /// 预设文件存放于程序目录的 MSL\ServerPropertiesPresets.json，所有服务器共享。
    /// </summary>
    public static class ServerPropertiesPresetManager
    {
        /// <summary>预设文件路径（相对程序工作目录，与 ServerConfig 的 MSL\ServerList.json 保持一致）</summary>
        public static readonly string PresetPath = @"MSL\ServerPropertiesPresets.json";

        /// <summary>
        /// 加载全部预设
        /// </summary>
        public static List<ServerPropertiesPreset> LoadAll()
        {
            try
            {
                if (!File.Exists(PresetPath))
                    return new List<ServerPropertiesPreset>();

                string json = File.ReadAllText(PresetPath, Encoding.UTF8);
                var list = JsonConvert.DeserializeObject<List<ServerPropertiesPreset>>(json);
                return list ?? new List<ServerPropertiesPreset>();
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"[ServerPropertiesPreset] 加载预设失败: {ex.Message}");
                return new List<ServerPropertiesPreset>();
            }
        }

        /// <summary>
        /// 保存全部预设（整体覆写）
        /// </summary>
        public static void SaveAll(List<ServerPropertiesPreset> presets)
        {
            try
            {
                string json = JsonConvert.SerializeObject(presets, Formatting.Indented);
                File.WriteAllText(PresetPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogHelper.Write.Error($"[ServerPropertiesPreset] 保存预设失败: {ex.Message}");
                throw;
            }
        }
    }
}
