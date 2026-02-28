using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace MSL.utils
{
    public class FastCommandInfo
    {
        public string Remark { get; set; }  // 备注 (如: 给管理员)
        public string Cmd { get; set; }     // 指令 (如: op)
        public string Alias { get; set; }   // 别名 (如: o)

        public string DisplayText  // 用于在 ComboBox 和 ListBox 中展示的格式
        {
            get
            {
                if (Cmd == "/") return "/（指令）"; // 根命令
                string text = $"/{Cmd.TrimStart('/')}"; // 统一显示前缀“/”
                if (!string.IsNullOrEmpty(Remark)) text += $"（{Remark}）";
                if (!string.IsNullOrEmpty(Alias)) text += $" [{Alias}]";
                return text;
            }
        }
    }

    /// <summary>
    /// 插件/模组列表项用的公共接口
    /// </summary>
    public interface IFileItem
    {
        /// <summary>文件名（不含 .disabled 后缀）</summary>
        string FileName { get; }

        /// <summary>是否处于禁用状态</summary>
        bool IsDisabled { get; set; }
    }

    /// <summary>
    /// ServerRunner的插件列表项
    /// </summary>
    public class SR_PluginInfo : IFileItem
    {
        public string FileName { get; }
        public bool IsDisabled { get; set; }
        public string PluginName => FileName;  // 暂时保留原有属性名

        public SR_PluginInfo(string fileName) => FileName = fileName;
    }

    /// <summary>
    /// ServerRunner的模组列表项
    /// </summary>
    public class SR_ModInfo : IFileItem
    {
        public string FileName { get; }
        public bool IsDisabled { get; set; }
        public bool IsClient { get; set; }
        public string ModName => FileName;  // 暂时保留原有属性名

        public SR_ModInfo(string fileName, bool isClient)
        {
            FileName = fileName;
            IsClient = isClient;
        }
    }

    /// <summary>
    /// 封装插件/模组目录下的通用文件操作：
    /// 加载列表、切换启用状态、删除、添加。
    /// </summary>
    public static class FileListManager
    {
        private const string DisabledSuffix = ".disabled";

        /// <summary>
        /// 扫描目录，返回所有 .jar / .jar.disabled 文件对应的列表项。
        /// <paramref name="itemFactory"/> 接收 (fileName, isDisabled) 并返回 T 实例。
        /// </summary>
        public static List<T> LoadItems<T>(
            string directory,
            Func<string, bool, T> itemFactory) where T : IFileItem
        {
            var list = new List<T>();

            foreach (var file in new DirectoryInfo(directory).GetFiles("*.*"))
            {
                if (file.Name.EndsWith(DisabledSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    string originalName = file.Name.Substring(0, file.Name.Length - DisabledSuffix.Length);
                    var item = itemFactory(originalName, true);
                    item.IsDisabled = true;
                    list.Add(item);
                }
                else if (file.Name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                {
                    var item = itemFactory(file.Name, false);
                    item.IsDisabled = false;
                    list.Add(item);
                }
            }

            return list;
        }

        /// <summary>
        /// 对 <paramref name="items"/> 中的每一项切换启用 / 禁用状态（重命名文件）。
        /// </summary>
        public static void ToggleDisabled<T>(string directory, IEnumerable<T> items)
            where T : IFileItem
        {
            foreach (var item in items)
            {
                string enabledPath = Path.Combine(directory, item.FileName);
                string disabledPath = enabledPath + DisabledSuffix;

                if (!item.IsDisabled)
                {
                    // 当前启用 → 禁用
                    if (File.Exists(enabledPath))
                        File.Move(enabledPath, disabledPath);
                }
                else
                {
                    // 当前禁用 → 启用
                    if (File.Exists(disabledPath))
                        File.Move(disabledPath, enabledPath);
                }
            }
        }

        /// <summary>
        /// 删除 <paramref name="items"/> 对应的物理文件。
        /// </summary>
        public static void DeleteItems<T>(string directory, IEnumerable<T> items)
            where T : IFileItem
        {
            foreach (var item in items)
            {
                string filePath = item.IsDisabled
                    ? Path.Combine(directory, item.FileName + DisabledSuffix)
                    : Path.Combine(directory, item.FileName);

                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        /// <summary>
        /// 将 <paramref name="sourceFiles"/> 批量复制到 <paramref name="targetDirectory"/>。
        /// 文件名取自 <paramref name="safeFileNames"/>（与 OpenFileDialog.SafeFileNames 对应）。
        /// 出错时弹框提示，并返回 false。
        /// </summary>
        public static bool CopyFilesTo(
            string targetDirectory,
            string[] sourceFiles,
            string[] safeFileNames)
        {
            try
            {
                for (int i = 0; i < sourceFiles.Length; i++)
                    File.Copy(sourceFiles[i], Path.Combine(targetDirectory, safeFileNames[i]));

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
    }
}
