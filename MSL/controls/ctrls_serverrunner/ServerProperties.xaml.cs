using HandyControl.Controls;
using Microsoft.Win32;
using MSL.utils;
using MSL.utils.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TextBox = System.Windows.Controls.TextBox;
using Window = System.Windows.Window;

namespace MSL.controls.ctrls_serverrunner
{
    /// <summary>
    /// ServerProperties.xaml 的交互逻辑
    /// </summary>
    public partial class ServerProperties : UserControl
    {
        public ServerProperties(ServerRunner fatherControl,MCServerService fatherService, string serverBase)
        {
            InitializeComponent();
            FatherControl = fatherControl;
            FatherService= fatherService;
            Rserverbase = serverBase;
            LegacyConfigPresetPath = Path.Combine(serverBase, "config-presets.json");
        }

        private readonly ServerRunner FatherControl;
        private readonly MCServerService FatherService;
        private readonly string Rserverbase;
        private static readonly string ConfigPresetPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "MSL",
            "config-presets.json");
        private static readonly string LegacyConfigPresetGenerationPath = ConfigPresetPath + ".generation";
        private static readonly object ConfigPresetFileLock = new object();
        private static readonly string ConfigPresetMutexName = BuildConfigPresetMutexName();
        private readonly string LegacyConfigPresetPath;
        private Dictionary<string, TextBox> configTextBoxes = new Dictionary<string, TextBox>();
        private readonly List<string> configKeyOrder = new List<string>();
        private ConfigPresetDefinition _selectedConfigPreset;
        private bool _configPresetsLoaded;
        private bool _configPresetOperationInProgress;
        private bool _hasEditableServerProperties;
        private bool _isDisposed;
        private int _configPresetStateGeneration;
        private CancellationTokenSource _configPresetConfirmationCancellation;
        private string _configPresetRevision;
        private string _serverPropertiesRevision;
        public ObservableCollection<ConfigPresetDefinition> ConfigPresets { get; } = new ObservableCollection<ConfigPresetDefinition>();
        public ObservableCollection<ConfigPresetItem> ConfigPresetItems { get; } = new ObservableCollection<ConfigPresetItem>();

        public void ClearConfigPresetState()
        {
            _configPresetStateGeneration++;
            CancelConfigPresetConfirmation();
            ResetConfigPresetToolbar();
        }

        #region 核心函数

        /// <summary>
        /// 读取指定配置项的值
        /// </summary>
        /// <param name="key">配置项键名</param>
        /// <returns>配置项值，如果不存在返回null</returns>
        public string GetConfigValue(string key)
        {
            try
            {
                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                if (!File.Exists(propertiesPath))
                    return null;

                Encoding encoding = Functions.GetTextFileEncodingType(propertiesPath);
                string[] lines = File.ReadAllLines(propertiesPath, encoding);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string lineKey = trimmedLine.Substring(0, separatorIndex).Trim();
                        if (lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            return trimmedLine.Substring(separatorIndex + 1).Trim();
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 设置指定配置项的值
        /// </summary>
        /// <param name="key">配置项键名</param>
        /// <param name="value">配置项值</param>
        /// <returns>是否设置成功</returns>
        public bool SetConfigValue(string key, string value)
        {
            try
            {
                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                if (!File.Exists(propertiesPath))
                    return false;

                return WithServerPropertiesFileLock(propertiesPath,
                    () => SetConfigValueLocked(propertiesPath, key, value));
            }
            catch
            {
                return false;
            }
        }

        private static bool SetConfigValueLocked(string propertiesPath, string key, string value)
        {
            Encoding encoding = Functions.GetTextFileEncodingType(propertiesPath);
            string[] lines = File.ReadAllLines(propertiesPath, encoding);
            bool keyFound = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmedLine = lines[i].Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                int separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex > 0)
                {
                    string lineKey = trimmedLine.Substring(0, separatorIndex).Trim();
                    if (lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = key + "=" + value;
                        keyFound = true;
                        break;
                    }
                }
            }

            if (keyFound)
            {
                WriteLinesAtomically(
                    propertiesPath,
                    lines,
                    encoding == Encoding.UTF8 ? new UTF8Encoding(false) : encoding);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 读取所有配置项
        /// </summary>
        /// <returns>配置项字典</returns>
        private Dictionary<string, string> GetAllConfigs()
        {
            try
            {
                string ignoredRevision;
                return GetAllConfigs(out ignoredRevision);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private Dictionary<string, string> GetAllConfigs(out string revision)
        {
            string propertiesPath = Path.Combine(Rserverbase, "server.properties");
            revision = null;
            if (!File.Exists(propertiesPath))
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int attempt = 0; attempt < 3; attempt++)
            {
                string revisionBeforeRead = ComputeFileFingerprint(propertiesPath);
                Encoding encoding = Functions.GetTextFileEncodingType(propertiesPath);
                string[] lines = File.ReadAllLines(propertiesPath, encoding);
                string revisionAfterRead = ComputeFileFingerprint(propertiesPath);
                if (!string.Equals(revisionBeforeRead, revisionAfterRead, StringComparison.Ordinal))
                    continue;

                revision = revisionAfterRead;
                return ParseConfigValues(lines);
            }

            throw new IOException("server.properties 在读取期间持续发生变化，请稍后刷新重试。");
        }

        public void Dispose()
        {
            _isDisposed = true;
            _configPresetStateGeneration++;
            CancelConfigPresetConfirmation();
            ChangeServerProperties.Children.Clear();
            ChangeServerProperties.RowDefinitions.Clear();
            configTextBoxes.Clear();
            configKeyOrder.Clear();
            _serverPropertiesRevision = null;
            ResetConfigPresetToolbar();
        }
        #endregion

        #region 服务器功能调整

        private void refreahServerConfig_Click(object sender, RoutedEventArgs e)
        {
            if (_configPresetOperationInProgress)
            {
                SetConfigPresetStatus("预设操作正在进行，请稍候。");
                return;
            }
            RefreshServerConfig();
            Growl.Success("刷新成功！");
        }

        public void RefreshServerConfig()
        {
            RefreshServerConfig(false);
        }

        private void RefreshServerConfig(bool allowDuringPresetOperation)
        {
            if (_configPresetOperationInProgress && !allowDuringPresetOperation)
            {
                SetConfigPresetStatus("预设操作正在进行，请稍候。");
                return;
            }

            Dictionary<string, string> serverConfigCache = new Dictionary<string, string>();
            string configPresetNameBeforeRefresh = ConfigPresetNameTextBox.Text;
            Dictionary<string, bool> configPresetSelectionsBeforeRefresh = ConfigPresetItems
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().IsSelected,
                    StringComparer.OrdinalIgnoreCase);
            bool preserveConfigPresetDraft = _selectedConfigPreset == null &&
                ConfigPresetPanel.Visibility == Visibility.Visible &&
                SaveConfigPresetButton.Visibility == Visibility.Visible;
            Dictionary<string, string> configPresetDraftValuesBeforeRefresh = preserveConfigPresetDraft
                ? ConfigPresetItems
                    .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Last().Value ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                bool propertiesFileExists = File.Exists(propertiesPath);
                serverConfigCache = GetAllConfigs(out string serverPropertiesRevision);
                _serverPropertiesRevision = serverPropertiesRevision;

                if (serverConfigCache.Count == 0)
                {
                    _hasEditableServerProperties = false;
                    changeServerPropertiesLab.Text = propertiesFileExists
                        ? "服务器配置（未找到可解析的配置项，无法更改基础配置）"
                        : "服务器配置（未找到文件，无法更改基础配置，运行一下服务器再试）";
                    saveServerConfig.IsEnabled = false;
                    LoadSelectedConfigPresetButton.IsEnabled = false;
                    configPresetButton.IsEnabled = false;
                    ChangeServerProperties.Visibility = Visibility.Collapsed;
                    return;
                }

                changeServerPropertiesLab.Text = "服务器配置信息";
                _hasEditableServerProperties = true;
                saveServerConfig.IsEnabled = !_configPresetOperationInProgress;
                LoadSelectedConfigPresetButton.IsEnabled = !_configPresetOperationInProgress;
                configPresetButton.IsEnabled = !_configPresetOperationInProgress;
                ChangeServerProperties.Visibility = Visibility.Visible;

                // 清理现有内容
                ChangeServerProperties.Children.Clear();
                ChangeServerProperties.RowDefinitions.Clear();
                configTextBoxes.Clear();
                configKeyOrder.Clear();

                // 定义常用配置项的显示顺序、中文名称和描述
                var commonConfigs = new Dictionary<string, string>
                {
                    { "online-mode", "注：正版验证，若开启（true），盗版/离线用户将无法进入该服务器，关闭请输入false" },
                    { "gamemode", "注：游戏模式，不同版本改法不一致，具体可参照上面的表格" },
                    { "difficulty", "注：游戏难度，不同版本改法不一致，具体可参照上面的表格" },
                    { "max-players", "注：最大玩家数，在此输入数字来改变服务器最大人数" },
                    { "server-port", "注：服务器端口，非必要无需更改" },
                    { "server-ip", "注：绑定服务器ip，如果你不知道这是什么，请不要随意在这里填写东西！这里并不能自定义您的服务器地址！" },
                    { "enable-command-block", "注：启用命令方块，若开启(true)，服务器可使用命令方块，关闭请输入false" },
                    { "view-distance", "注：视距，和游戏内的渲染距离意思相近，设置过大会影响服务器性能" },
                    { "pvp", "注：PVP模式，若开启（true），玩家间可互相伤害，关闭请输入false" },
                    { "level-name", "注：世界名称，默认为world，非必要无需更改" },
                    { "motd", "注：服务器MOTD，服务器列表中显示的服务器简介" },
                    { "allow-flight", "注：允许飞行（若使用喷气背包/鞘翅飞行时被踢出服务器，请将这里设置为true）" },
                };

                int rowIndex = 0;

                // 先添加常用配置项
                foreach (var kvp in commonConfigs)
                {
                    if (serverConfigCache.ContainsKey(kvp.Key))
                    {
                        AddConfigRow(kvp.Key, serverConfigCache[kvp.Key], kvp.Value, rowIndex);
                        rowIndex++;
                    }
                }

                // 添加其他配置项
                foreach (var config in serverConfigCache.OrderBy(x => x.Key))
                {
                    if (!commonConfigs.ContainsKey(config.Key))
                    {
                        AddConfigRow(config.Key, config.Value, null, rowIndex);
                        rowIndex++;
                    }
                }

                if (ConfigPresetPanel.Visibility == Visibility.Visible)
                {
                    if (_selectedConfigPreset != null)
                        InitializeConfigPresetItems(_selectedConfigPreset.Values, _selectedConfigPreset.Values.Keys);
                    else if (preserveConfigPresetDraft)
                    {
                        Dictionary<string, string> draftValues = GetCurrentConfigValues();
                        foreach (var draftValue in configPresetDraftValuesBeforeRefresh)
                            draftValues[draftValue.Key] = draftValue.Value ?? string.Empty;
                        InitializeConfigPresetItems(draftValues);
                    }
                    else
                        InitializeConfigPresetItems(GetCurrentConfigValues());
                    if (_selectedConfigPreset != null)
                        LoadConfigPresetIntoEditor(_selectedConfigPreset);

                    foreach (var item in ConfigPresetItems)
                    {
                        if (configPresetSelectionsBeforeRefresh.TryGetValue(item.Key, out bool isSelected))
                            item.IsSelected = isSelected;
                    }
                    if (!string.IsNullOrEmpty(configPresetNameBeforeRefresh))
                        ConfigPresetNameTextBox.Text = configPresetNameBeforeRefresh;
                }
                else if (!_configPresetsLoaded && ConfigPresets.Count == 0)
                    ResetConfigPresetToolbar();
            }
            catch (Exception ex)
            {
                _serverPropertiesRevision = null;
                _hasEditableServerProperties = false;
                changeServerPropertiesLab.Text = "读取服务器配置失败：" + ex.Message;
                configPresetButton.IsEnabled = false;
                LoadSelectedConfigPresetButton.IsEnabled = false;
                saveServerConfig.IsEnabled = false;
                ChangeServerProperties.Visibility = Visibility.Collapsed;
            }
            finally
            {
                serverConfigCache.Clear();
            }
        }

        /// <summary>
        /// 添加配置项行
        /// </summary>
        private void AddConfigRow(string key, string value, string description, int rowIndex)
        {
            ChangeServerProperties.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 标签
            Label label = new Label
            {
                Content = key + ": ",
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };
            label.SetResourceReference(Label.StyleProperty, "MagicLabel14");

            // 文本框
            TextBox textBox = new TextBox
            {
                Text = value,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 200,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap
            };

            // 容器

            Grid firstPanel = new Grid();
            firstPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            firstPanel.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(label, 0);
            firstPanel.Children.Add(label);
            Grid.SetColumn(textBox, 1);
            firstPanel.Children.Add(textBox);

            // 如果有描述，添加描述TextBlock
            if (!string.IsNullOrEmpty(description))
            {
                Grid panel = new Grid();
                panel.RowDefinitions.Add(new RowDefinition());
                panel.RowDefinitions.Add(new RowDefinition());

                Grid.SetRow(firstPanel, 0);
                panel.Children.Add(firstPanel);
                TextBlock descriptionBlock = new TextBlock
                {
                    Text = description,
                    Margin = new Thickness(10, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                descriptionBlock.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextBrush");

                Grid.SetRow(descriptionBlock, 1);
                panel.Children.Add(descriptionBlock);
                Grid.SetRow(panel, rowIndex);
                ChangeServerProperties.Children.Add(panel);
            }
            else
            {
                Grid.SetRow(firstPanel, rowIndex);
                ChangeServerProperties.Children.Add(firstPanel);
            }

            // 保存引用
            textBox.Tag = key;
            textBox.TextChanged += ConfigTextBox_TextChanged;
            configTextBoxes[key] = textBox;
            configKeyOrder.Add(key);
        }

        private void ConfigTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedConfigPreset != null ||
                ConfigPresetPanel.Visibility != Visibility.Visible ||
                SaveConfigPresetButton.Visibility != Visibility.Visible ||
                !(sender is TextBox textBox) || !(textBox.Tag is string key))
                return;

            ConfigPresetItem item = ConfigPresetItems.FirstOrDefault(candidate =>
                string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase));
            if (item != null)
                item.Value = textBox.Text ?? string.Empty;
        }

        private async void saveServerConfig_Click(object sender, RoutedEventArgs e)
        {
            if (_configPresetOperationInProgress)
            {
                SetConfigPresetStatus("预设操作正在进行，请稍候。");
                return;
            }
            if (FatherService.CheckServerRunning())
            {
                MagicShow.ShowMsgDialog(FatherControl, "服务器运行时无法调整服务器功能！", "错误");
                return;
            }
            string propertiesPath = Path.Combine(Rserverbase, "server.properties");
            if (!File.Exists(propertiesPath))
            {
                MagicShow.ShowMsgDialog(FatherControl, "配置文件不存在！", "错误");
                return;
            }
            if (string.IsNullOrEmpty(_serverPropertiesRevision))
            {
                MagicShow.ShowMsgDialog(FatherControl, "配置文件状态尚未加载，请先刷新后重试。", "提示");
                return;
            }

            Dictionary<string, string> values = configTextBoxes.ToDictionary(
                item => item.Key,
                item => item.Value.Text?.Trim() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
            string expectedRevision = _serverPropertiesRevision;
            if (!BeginConfigPresetOperation())
                return;
            int operationGeneration = _configPresetStateGeneration;

            try
            {
                bool hasChanges = await Task.Run(() => WithServerPropertiesFileLock(
                    propertiesPath,
                    () => SaveServerConfigValuesLocked(
                        propertiesPath, values, expectedRevision)));
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                if (hasChanges)
                {
                    MagicShow.ShowMsgDialog(FatherControl, "保存成功！", "信息");
                    RefreshServerConfig(true);
                }
                else
                {
                    MagicShow.ShowMsgDialog(FatherControl, "没有需要保存的更改！", "信息");
                }
            }
            catch (ServerPropertiesStorageBusyException)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                {
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        "server.properties 正在被其他操作使用，请稍后重试。",
                        "提示");
                }
            }
            catch (ServerPropertiesConflictException)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                {
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        "server.properties 已被其他窗口或程序修改，本次保存已取消。请刷新后重试。",
                        "提示");
                }
            }
            catch (Exception ex)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                {
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        "保存过程中发生错误！\n错误代码：" + ex.Message,
                        "错误");
                }
            }
            finally
            {
                EndConfigPresetOperation();
            }
        }

        private bool SaveServerConfigValuesLocked(
            string propertiesPath,
            IReadOnlyDictionary<string, string> values,
            string expectedRevision)
        {
            if (FatherService.CheckServerRunning())
                throw new InvalidOperationException("服务器已经启动，无法保存配置。");
            if (!File.Exists(propertiesPath))
                throw new FileNotFoundException("配置文件不存在。", propertiesPath);
            if (!string.Equals(expectedRevision, ComputeFileFingerprint(propertiesPath), StringComparison.Ordinal))
                throw new ServerPropertiesConflictException();

            Encoding encoding = Functions.GetTextFileEncodingType(propertiesPath);
            string[] lines = File.ReadAllLines(propertiesPath, encoding);
            bool hasChanges = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string originalLine = lines[i];
                string trimmedLine = originalLine.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                int separatorIndex = originalLine.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                string key = originalLine.Substring(0, separatorIndex).Trim();
                if (!values.TryGetValue(key, out string configuredValue))
                    continue;

                string newValue = configuredValue ?? string.Empty;
                string oldValue = originalLine.Substring(separatorIndex + 1).Trim();
                if (newValue == oldValue)
                    continue;

                lines[i] = ReplaceConfigValueInLine(originalLine, separatorIndex, newValue);
                hasChanges = true;
            }

            if (!string.Equals(expectedRevision, ComputeFileFingerprint(propertiesPath), StringComparison.Ordinal))
                throw new ServerPropertiesConflictException();

            if (hasChanges)
            {
                WriteLinesAtomically(
                    propertiesPath,
                    lines,
                    encoding == Encoding.UTF8 ? new UTF8Encoding(false) : encoding);
            }
            return hasChanges;
        }

        private async void changeServerIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FatherService.CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(FatherControl, "服务器运行时无法更换图标！", "错误");
                    return;
                }
                if (File.Exists(Rserverbase + "\\server-icon.png"))
                {
                    bool dialogret = await MagicShow.ShowMsgDialogAsync(FatherControl, "检测到服务器已设置有图标，是否删除该图标？", "警告", true, "取消");
                    if (dialogret)
                    {
                        try
                        {
                            File.Delete(Rserverbase + "\\server-icon.png");
                        }
                        catch (Exception ex)
                        {
                            MagicShow.ShowMsgDialog(FatherControl, "图标删除失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
                            return;
                        }
                        bool _dialogret = await MagicShow.ShowMsgDialogAsync(FatherControl, "原图标已删除，是否继续操作？", "提示", true, "取消");
                        if (!_dialogret)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                await MagicShow.ShowMsgDialogAsync(FatherControl, "请先准备一张64*64像素的图片（格式为png），准备完成后点击确定以继续", "如何操作？");
                OpenFileDialog openfile = new OpenFileDialog
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Title = "请选择文件",
                    Filter = "PNG图像|*.png"
                };
                var res = openfile.ShowDialog();
                if (res == true)
                {
                    try
                    {
                        File.Copy(openfile.FileName, Rserverbase + "\\server-icon.png", true);
                        MagicShow.ShowMsgDialog(FatherControl, "图标更换完成！", "信息");
                    }
                    catch (Exception ex)
                    {
                        MagicShow.ShowMsgDialog(FatherControl, "图标更换失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
                    }
                }
            }
            catch
            {
                return;
            }
        }

        private async void changeWorldMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FatherService.CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(FatherControl, "服务器运行时无法更换地图！", "错误");
                    return;
                }
                string levelName = GetConfigValue("level-name") ?? "world";

                if (Directory.Exists(Rserverbase + @"\" + levelName))
                {
                    if (await MagicShow.ShowMsgDialogAsync(FatherControl, "点击确定后，MSL将删除原先主世界地图（删除后，地图将从电脑上彻底消失，如有必要请提前备份！）\n点击取消以中止操作", "警告", true, "取消"))
                    {
                        MagicDialog dialog = new MagicDialog();
                        dialog.ShowTextDialog(FatherControl, "删除中，请稍候");
                        await Task.Run(() =>
                        {
                            DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + levelName);
                            di.Delete(true);
                        });
                        dialog.CloseTextDialog();
                    }
                    else
                    {
                        return;
                    }

                    if (Directory.Exists(Rserverbase + @"\" + levelName + "_nether"))
                    {
                        if (await MagicShow.ShowMsgDialogAsync(FatherControl, "MSL同时检测到了下界地图，是否一并删除？\n删除后，地图将从电脑上彻底消失！", "警告", true, "取消"))
                        {
                            MagicDialog dialog = new MagicDialog();
                            dialog.ShowTextDialog(FatherControl, "删除中，请稍候");
                            await Task.Run(() =>
                            {
                                DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + levelName + "_nether");
                                di.Delete(true);
                            });
                            dialog.CloseTextDialog();
                        }
                    }

                    if (Directory.Exists(Rserverbase + @"\" + levelName + "_the_end"))
                    {
                        if (await MagicShow.ShowMsgDialogAsync(FatherControl, "MSL同时检测到了末地地图，是否一并删除？\n删除后，地图将从电脑上彻底消失！", "警告", true, "取消"))
                        {
                            MagicDialog dialog = new MagicDialog();
                            dialog.ShowTextDialog(FatherControl, "删除中，请稍候");
                            await Task.Run(() =>
                            {
                                DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + levelName + "_the_end");
                                di.Delete(true);
                            });
                            dialog.CloseTextDialog();
                        }
                    }

                    if (await MagicShow.ShowMsgDialogAsync(FatherControl, "相关地图已经成功删除！是否选择新存档进行导入？（如果不导入而直接开服，服务器将会重新创建一个新世界）", "提示", true, "取消"))
                    {
                        System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
                        {
                            Description = "请选择地图文件夹(或解压后的文件夹)"
                        };
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            try
                            {
                                MagicDialog _dialog = new MagicDialog();
                                _dialog.ShowTextDialog(FatherControl, "导入中，请稍候");
                                await Functions.MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + levelName, false);
                                _dialog.CloseTextDialog();
                                MagicShow.ShowMsgDialog(FatherControl, "导入世界成功！源存档目录您可手动进行删除！", "信息");
                            }
                            catch (Exception ex)
                            {
                                MagicShow.ShowMsgDialog(FatherControl, "导入世界失败！\n错误代码：" + ex.Message, "错误");
                            }
                        }
                    }
                }
                else
                {
                    System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = "请选择地图文件夹(或解压后的文件夹)"
                    };
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        try
                        {
                            MagicDialog _dialog = new MagicDialog();
                            _dialog.ShowTextDialog(FatherControl, "导入中，请稍候");
                            await Functions.MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + levelName, false);
                            _dialog.CloseTextDialog();
                            MagicShow.ShowMsgDialog(FatherControl, "导入世界成功！源存档目录您可手动进行删除！", "信息");
                        }
                        catch (Exception ex)
                        {
                            MagicShow.ShowMsgDialog(FatherControl, "导入世界失败！\n错误代码：" + ex.Message, "错误");
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }

        private void setServerconfig_Click(object sender, RoutedEventArgs e)
        {
            Window window = new SetServerconfig(Rserverbase)
            {
                Owner = FatherControl
            };
            window.ShowDialog();
            RefreshServerConfig();
        }

        private void configPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanUseConfigPresets())
                return;
            if (!BeginConfigPresetOperation())
                return;
            try
            {
                Dictionary<string, string> currentValues = GetCurrentConfigValues();
                if (currentValues.Count == 0)
                {
                    MagicShow.ShowMsgDialog(FatherControl, "未找到 server.properties，无法创建配置预设！", "提示");
                    return;
                }

                _selectedConfigPreset = null;
                RenderConfigPresetButtons();
                configPresetButton.SetResourceReference(Button.StyleProperty, "ButtonPrimary");
                InitializeConfigPresetItems(currentValues);
                ConfigPresetNameTextBox.Clear();
                ConfigPresetNameTextBox.IsReadOnly = false;
                SaveConfigPresetButton.Visibility = Visibility.Visible;
                SetExistingConfigPresetActionsVisibility(Visibility.Collapsed);
                ConfigPresetPanel.Visibility = Visibility.Visible;
                SetConfigPresetStatus("正在添加新预设，默认选择全部配置值。");
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(FatherControl, "加载配置预设失败：\n" + ex.Message, "错误");
            }
            finally
            {
                EndConfigPresetOperation();
            }
        }

        private void CollapseConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            _selectedConfigPreset = null;
            ConfigPresetItems.Clear();
            ConfigPresetNameTextBox.Clear();
            ConfigPresetNameTextBox.IsReadOnly = false;
            SetConfigPresetStatus(null);
            SaveConfigPresetButton.Visibility = Visibility.Collapsed;
            configPresetButton.ClearValue(Button.StyleProperty);
            HideConfigPresetPanel();
            RenderConfigPresetButtons();
        }

        private void HideConfigPresetPanel()
        {
            ConfigPresetPanel.Visibility = Visibility.Collapsed;
            SetExistingConfigPresetActionsVisibility(Visibility.Collapsed);
        }

        private void SetExistingConfigPresetActionsVisibility(Visibility visibility)
        {
            if (FindName("ApplyConfigPresetButton") is Button applyButton)
                applyButton.Visibility = visibility;
            if (FindName("DeleteConfigPresetButton") is Button deleteButton)
                deleteButton.Visibility = visibility;
        }

        private bool CanUseConfigPresets()
        {
            string propertiesPath = Path.Combine(Rserverbase, "server.properties");
            if (_hasEditableServerProperties && File.Exists(propertiesPath))
                return true;

            _hasEditableServerProperties = false;
            SetConfigPresetOperationState(false);
            SetConfigPresetStatus("未找到可用的 server.properties，无法加载或添加预设。");
            return false;
        }

        private void ResetConfigPresetToolbar()
        {
            _selectedConfigPreset = null;
            _configPresetsLoaded = false;
            _configPresetRevision = null;
            ConfigPresets.Clear();
            ConfigPresetItems.Clear();
            ConfigPresetButtonsPanel.Children.Clear();
            ConfigPresetNameTextBox.Clear();
            ConfigPresetNameTextBox.IsReadOnly = false;
            SetConfigPresetStatus(null);
            SaveConfigPresetButton.Visibility = Visibility.Collapsed;
            configPresetButton.ClearValue(Button.StyleProperty);
            HideConfigPresetPanel();
        }

        private bool BeginConfigPresetOperation()
        {
            if (_configPresetOperationInProgress)
            {
                SetConfigPresetStatus("预设操作正在进行，请稍候。");
                return false;
            }

            _configPresetOperationInProgress = true;
            SetConfigPresetOperationState(true);
            return true;
        }

        private void EndConfigPresetOperation()
        {
            _configPresetOperationInProgress = false;
            SetConfigPresetOperationState(false);
        }

        private void SetConfigPresetOperationState(bool busy)
        {
            if (_isDisposed)
                return;
            configPresetButton.IsEnabled = !busy && _hasEditableServerProperties;
            LoadSelectedConfigPresetButton.IsEnabled = !busy && _hasEditableServerProperties;
            SaveConfigPresetButton.IsEnabled = !busy;
            ConfigPresetPanel.IsEnabled = !busy;
            ChangeServerProperties.IsEnabled = !busy;
            refreahServerConfig.IsEnabled = !busy;
            saveServerConfig.IsEnabled = !busy && _hasEditableServerProperties;
            setServerconfig.IsEnabled = !busy;
            foreach (UIElement child in ConfigPresetButtonsPanel.Children)
                child.IsEnabled = !busy;
        }

        private bool IsConfigPresetOperationStale(int generation)
        {
            return _isDisposed || generation != _configPresetStateGeneration;
        }

        private CancellationTokenSource CreateConfigPresetConfirmationCancellation()
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            _configPresetConfirmationCancellation = cancellation;
            return cancellation;
        }

        private void ReleaseConfigPresetConfirmationCancellation(CancellationTokenSource cancellation)
        {
            if (ReferenceEquals(_configPresetConfirmationCancellation, cancellation))
                _configPresetConfirmationCancellation = null;
            cancellation.Dispose();
        }

        private void CancelConfigPresetConfirmation()
        {
            CancellationTokenSource cancellation = _configPresetConfirmationCancellation;
            _configPresetConfirmationCancellation = null;
            if (cancellation == null)
                return;

            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void InitializeConfigPresetItems(IDictionary<string, string> currentValues, IEnumerable<string> keysToInclude = null)
        {
            ConfigPresetItems.Clear();
            HashSet<string> includedKeys = keysToInclude == null
                ? null
                : new HashSet<string>(keysToInclude, StringComparer.OrdinalIgnoreCase);
            HashSet<string> addedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in configKeyOrder)
            {
                if (!currentValues.ContainsKey(key) || (includedKeys != null && !includedKeys.Contains(key)))
                    continue;

                ConfigPresetItems.Add(new ConfigPresetItem
                {
                    Key = key,
                    Value = currentValues[key] ?? string.Empty,
                    IsSelected = true
                });
                addedKeys.Add(key);
            }

            foreach (var config in currentValues)
            {
                if (addedKeys.Contains(config.Key) || (includedKeys != null && !includedKeys.Contains(config.Key)))
                    continue;

                ConfigPresetItems.Add(new ConfigPresetItem
                {
                    Key = config.Key,
                    Value = config.Value ?? string.Empty,
                    IsSelected = true
                });
            }
        }

        private async Task<bool> LoadConfigPresetsAsync(int operationGeneration, bool renderButtons = true)
        {
            string selectedName = _selectedConfigPreset?.Name;
            try
            {
                IReadOnlyList<string> legacyPaths = GetLegacyConfigPresetPaths();
                ConfigPresetStorageSnapshot snapshot = await Task.Run(
                    () => LoadConfigPresetsFromStorage(legacyPaths));
                if (IsConfigPresetOperationStale(operationGeneration))
                    return false;
                ConfigPresets.Clear();
                foreach (var preset in snapshot.Presets)
                    ConfigPresets.Add(preset);

                _selectedConfigPreset = string.IsNullOrEmpty(selectedName)
                    ? null
                    : ConfigPresets.FirstOrDefault(preset =>
                        string.Equals(preset.Name, selectedName, StringComparison.OrdinalIgnoreCase));
                _configPresetRevision = snapshot.Revision;
                _configPresetsLoaded = true;

                if (snapshot.MigrationConflictCount > 0 ||
                    snapshot.NormalizationConflictCount > 0 ||
                    snapshot.CanonicalQuarantineCount > 0 ||
                    snapshot.LegacyQuarantineCount > 0 ||
                    snapshot.MarkerWarningCount > 0)
                {
                    SetConfigPresetStatus(
                        $"预设已加载：迁移冲突 {snapshot.MigrationConflictCount} 项，" +
                        $"重复数据冲突 {snapshot.NormalizationConflictCount} 项，" +
                        $"隔离损坏主文件 {snapshot.CanonicalQuarantineCount} 个，" +
                        $"隔离损坏旧文件 {snapshot.LegacyQuarantineCount} 个，" +
                        $"迁移标记警告 {snapshot.MarkerWarningCount} 个；" +
                        "重复数据保留首个值，迁移冲突保留全局值。");
                }
                else
                {
                    SetConfigPresetStatus(snapshot.Presets.Count == 0
                        ? "未找到已保存的配置预设。"
                        : $"已加载 {snapshot.Presets.Count} 个配置预设。");
                }
            }
            catch (ConfigPresetStorageBusyException)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设文件正在被其他进程使用，请稍后重试。");
                return false;
            }
            catch (Exception ex)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设文件读取失败：" + ex.Message);
                return false;
            }

            if (renderButtons)
                RenderConfigPresetButtons();
            return true;
        }

        private void RenderConfigPresetButtons()
        {
            ConfigPresetButtonsPanel.Children.Clear();
            foreach (var preset in ConfigPresets)
            {
                Button button = new Button
                {
                    Content = new TextBlock
                    {
                        Text = preset.Name,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = 180
                    },
                    Tag = preset,
                    ToolTip = preset.Name,
                    MinWidth = 50,
                    Padding = new Thickness(10, 0, 10, 0),
                    Margin = new Thickness(0, 0, 5, 3),
                    IsEnabled = !_configPresetOperationInProgress
                };
                if (ReferenceEquals(preset, _selectedConfigPreset))
                    button.SetResourceReference(Button.StyleProperty, "ButtonPrimary");
                button.Click += ExistingConfigPresetButton_Click;
                ConfigPresetButtonsPanel.Children.Add(button);
            }

        }

        private void ReplaceVisibleConfigPresets(
            ConfigPresetStorageSnapshot snapshot,
            bool includeAll,
            IEnumerable<string> additionalNames = null)
        {
            HashSet<string> visibleNames = includeAll
                ? null
                : new HashSet<string>(ConfigPresets.Select(item => item.Name),
                    StringComparer.OrdinalIgnoreCase);
            if (visibleNames != null && additionalNames != null)
            {
                foreach (string name in additionalNames.Where(name => !string.IsNullOrWhiteSpace(name)))
                    visibleNames.Add(name);
            }

            ConfigPresets.Clear();
            foreach (ConfigPresetDefinition preset in snapshot.Presets)
            {
                if (includeAll || visibleNames.Contains(preset.Name))
                    ConfigPresets.Add(preset);
            }
        }

        private void ExistingConfigPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_configPresetOperationInProgress)
                return;
            if (!(sender is Button button) || !(button.Tag is ConfigPresetDefinition preset))
                return;

            _selectedConfigPreset = preset;
            Dictionary<string, string> presetValues = preset.Values ??
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            InitializeConfigPresetItems(presetValues, presetValues.Keys);
            LoadConfigPresetIntoEditor(preset);
            ConfigPresetNameTextBox.IsReadOnly = true;
            SaveConfigPresetButton.Visibility = Visibility.Collapsed;
            SetExistingConfigPresetActionsVisibility(Visibility.Visible);
            configPresetButton.ClearValue(Button.StyleProperty);
            ConfigPresetPanel.Visibility = Visibility.Visible;
            RenderConfigPresetButtons();
        }

        private void ConfigPresetList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is CheckBox checkBox)
                {
                    if (checkBox.DataContext is ConfigPresetItem checkBoxItem)
                        checkBoxItem.IsSelected = !checkBoxItem.IsSelected;
                    e.Handled = true;
                    return;
                }
                source = VisualTreeHelper.GetParent(source);
            }

            ListViewItem listViewItem = ItemsControl.ContainerFromElement(
                ConfigPresetList,
                e.OriginalSource as DependencyObject) as ListViewItem;
            if (listViewItem?.Content is ConfigPresetItem item)
            {
                item.IsSelected = !item.IsSelected;
                e.Handled = true;
            }
        }

        private void LoadConfigPresetIntoEditor(ConfigPresetDefinition preset)
        {
            foreach (var item in ConfigPresetItems)
            {
                if ((preset.Values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
                    .TryGetValue(item.Key, out string presetValue))
                {
                    item.Value = presetValue ?? string.Empty;
                    item.IsSelected = true;
                }
                else
                {
                    item.Value = string.Empty;
                    item.IsSelected = false;
                }
            }

            ConfigPresetNameTextBox.Text = preset.Name;
            SetConfigPresetStatus($"已加载预设“{preset.Name}”，选中 {preset.Values?.Count ?? 0} 项。");
        }

        private ConfigPresetStorageSnapshot UpsertConfigPreset(
            string name,
            IDictionary<string, string> values,
            IDictionary<string, string> valueFormats,
            ISet<string> visibleKeys,
            string expectedRevision)
        {
            return WithConfigPresetStorageLock(() =>
            {
                ConfigPresetDocumentReadResult readResult = ReadCanonicalDocumentLocked(false);
                EnsureExpectedRevision(expectedRevision, readResult.Revision);
                ConfigPresetStorageDocument document = readResult.Document;
                document.LegacyBlockedPresets.Remove(name);
                ConfigPresetDefinition preset = document.Presets.FirstOrDefault(item =>
                    string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                if (preset == null)
                {
                    preset = new ConfigPresetDefinition { Name = name };
                    document.Presets.Add(preset);
                }

                if (preset.Values == null)
                    preset.Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (preset.ValueFormats == null)
                    preset.ValueFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                HashSet<string> blockedKeys = GetOrCreateBlockedValueKeys(document, name);
                if (visibleKeys != null)
                {
                    foreach (string key in visibleKeys)
                    {
                        preset.Values.Remove(key);
                        preset.ValueFormats.Remove(key);
                        if (values.ContainsKey(key))
                            blockedKeys.Remove(key);
                        else
                            blockedKeys.Add(key);
                    }
                }
                foreach (var value in values)
                {
                    preset.Values[value.Key] = value.Value ?? string.Empty;
                    blockedKeys.Remove(value.Key);
                }
                foreach (var format in valueFormats ?? new Dictionary<string, string>())
                    preset.ValueFormats[format.Key] = format.Value ?? string.Empty;

                if (blockedKeys.Count == 0)
                    document.LegacyBlockedValues.Remove(name);
                string revision = WriteConfigPresetDocumentLocked(document);
                return CreateStorageSnapshot(document, revision, 0,
                    readResult.NormalizationConflictCount, 0, 0, 0);
            });
        }

        private ConfigPresetStorageSnapshot DeleteConfigPresetFromStorage(string name, string expectedRevision)
        {
            return WithConfigPresetStorageLock(() =>
            {
                ConfigPresetDocumentReadResult readResult = ReadCanonicalDocumentLocked(false);
                EnsureExpectedRevision(expectedRevision, readResult.Revision);
                ConfigPresetStorageDocument document = readResult.Document;
                document.Presets.RemoveAll(item =>
                    string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                document.LegacyBlockedPresets.Add(name);
                document.LegacyBlockedValues.Remove(name);
                string revision = WriteConfigPresetDocumentLocked(document);
                return CreateStorageSnapshot(document, revision, 0,
                    readResult.NormalizationConflictCount, 0, 0, 0);
            });
        }

        private ConfigPresetStorageSnapshot ReadConfigPresetsFromStorage()
        {
            return WithConfigPresetStorageLock(() =>
            {
                ConfigPresetDocumentReadResult readResult = ReadCanonicalDocumentLocked(false);
                return CreateStorageSnapshot(
                    readResult.Document,
                    readResult.Revision,
                    0,
                    readResult.NormalizationConflictCount,
                    0,
                    0,
                    0);
            });
        }

        private ConfigPresetStorageSnapshot LoadConfigPresetsFromStorage(
            IReadOnlyList<string> legacyPaths)
        {
            return WithConfigPresetStorageLock(() => LoadStorageLocked(legacyPaths));
        }

        private static void EnsureExpectedRevision(string expectedRevision, string actualRevision)
        {
            if (!string.Equals(expectedRevision, actualRevision, StringComparison.Ordinal))
                throw new ConfigPresetConflictException();
        }

        private static T WithConfigPresetStorageLock<T>(Func<T> action)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(5);
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool lockTaken = false;
            try
            {
                if (!Monitor.TryEnter(ConfigPresetFileLock, timeout))
                    throw new ConfigPresetStorageBusyException();
                lockTaken = true;

                bool ownsProcessMutex = false;
                Mutex processMutex = null;
                try
                {
                    try
                    {
                        processMutex = new Mutex(false, ConfigPresetMutexName);
                        TimeSpan remaining = timeout - stopwatch.Elapsed;
                        if (remaining <= TimeSpan.Zero || !processMutex.WaitOne(remaining))
                            throw new ConfigPresetStorageBusyException();
                        ownsProcessMutex = true;
                    }
                    catch (AbandonedMutexException)
                    {
                        ownsProcessMutex = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw new ConfigPresetStorageBusyException();
                    }
                    catch (IOException)
                    {
                        throw new ConfigPresetStorageBusyException();
                    }
                    catch (WaitHandleCannotBeOpenedException)
                    {
                        throw new ConfigPresetStorageBusyException();
                    }
                    catch (ArgumentException)
                    {
                        throw new ConfigPresetStorageBusyException();
                    }

                    return action();
                }
                finally
                {
                    if (ownsProcessMutex)
                        processMutex.ReleaseMutex();
                    processMutex?.Dispose();
                }
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(ConfigPresetFileLock);
            }
        }

        private static string BuildConfigPresetMutexName()
        {
            return BuildGlobalMutexName("ConfigPresets", ConfigPresetPath);
        }

        private static string BuildGlobalMutexName(string scope, string path)
        {
            string normalizedPath;
            try
            {
                normalizedPath = Path.GetFullPath(path).ToUpperInvariant();
            }
            catch
            {
                normalizedPath = (path ?? string.Empty).ToUpperInvariant();
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                string hash = BitConverter.ToString(
                    sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedPath))).Replace("-", string.Empty);
                return @"Global\MSL." + scope + "." + hash;
            }
        }

        private static T WithServerPropertiesFileLock<T>(string path, Func<T> action)
        {
            string mutexName = BuildGlobalMutexName("ServerProperties", path);
            bool ownsMutex = false;
            Mutex mutex = null;
            try
            {
                try
                {
                    mutex = new Mutex(false, mutexName);
                    if (!mutex.WaitOne(TimeSpan.FromSeconds(5)))
                        throw new ServerPropertiesStorageBusyException();
                    ownsMutex = true;
                }
                catch (AbandonedMutexException)
                {
                    ownsMutex = true;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException ||
                    ex is IOException || ex is WaitHandleCannotBeOpenedException ||
                    ex is ArgumentException)
                {
                    throw new ServerPropertiesStorageBusyException();
                }

                return action();
            }
            finally
            {
                if (ownsMutex)
                    mutex.ReleaseMutex();
                mutex?.Dispose();
            }
        }

        private ConfigPresetStorageSnapshot LoadStorageLocked(
            IReadOnlyList<string> legacyPaths)
        {
            ConfigPresetDocumentReadResult readResult = ReadCanonicalDocumentLocked(true);
            ConfigPresetStorageDocument document = readResult.Document;
            int normalizationConflicts = readResult.NormalizationConflictCount;
            int migrationConflicts = 0;
            int legacyQuarantineCount = 0;
            int markerWarningCount = 0;
            bool shouldWriteCanonical = readResult.WasLegacyArray ||
                readResult.WasRecovered || normalizationConflicts > 0;
            Dictionary<string, string> legacyMarkersToWrite =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string legacyPath in legacyPaths ?? Array.Empty<string>())
            {
                if (!File.Exists(legacyPath) || AreSamePath(legacyPath, ConfigPresetPath))
                    continue;

                ConfigPresetDocumentReadResult legacyReadResult = null;
                try
                {
                    legacyReadResult = ReadConfigPresetDocument(legacyPath);
                }
                catch (ConfigPresetJsonException)
                {
                    string quarantinePath = QuarantineCorruptPresetFileLocked(legacyPath);
                    if (quarantinePath == null)
                        throw;
                    legacyQuarantineCount++;
                }

                if (legacyReadResult != null)
                {
                    string legacyHash = legacyReadResult.Revision;
                    normalizationConflicts += legacyReadResult.NormalizationConflictCount;
                    string legacyKey = NormalizeLegacyPathKey(legacyPath);
                    string markerPath = legacyPath + ".migrated";
                    bool markerWasQuarantined = false;
                    LegacyConfigPresetMigrationMarker migrationMarker;
                    try
                    {
                        migrationMarker = ReadMigrationMarkerLocked(
                            markerPath, out markerWasQuarantined);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        migrationMarker = new LegacyConfigPresetMigrationMarker();
                        markerWarningCount++;
                    }
                    if (markerWasQuarantined)
                        markerWarningCount++;
                    bool markerIsCurrent = string.Equals(
                            legacyHash,
                            migrationMarker.LegacyHash,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            document.Generation,
                            migrationMarker.Generation,
                            StringComparison.Ordinal);

                    bool alreadyMigrated = !readResult.WasMissing && !readResult.WasRecovered &&
                        document.LegacyMigrations.TryGetValue(legacyKey, out string migratedHash) &&
                        string.Equals(migratedHash, legacyHash, StringComparison.Ordinal);

                    if (!alreadyMigrated && !readResult.WasMissing && !readResult.WasRecovered &&
                        !document.LegacyMigrations.ContainsKey(legacyKey))
                    {
                        alreadyMigrated = markerIsCurrent;
                    }

                    if (!alreadyMigrated)
                    {
                        migrationConflicts += MergeLegacyPresets(
                            document, legacyReadResult.Document.Presets);
                    }

                    if (!document.LegacyMigrations.TryGetValue(legacyKey, out string currentHash) ||
                        !string.Equals(currentHash, legacyHash, StringComparison.Ordinal))
                    {
                        document.LegacyMigrations[legacyKey] = legacyHash;
                        shouldWriteCanonical = true;
                    }
                    if (!markerIsCurrent)
                        legacyMarkersToWrite[markerPath] = legacyHash;
                }
            }

            string revision = readResult.Revision;
            if (shouldWriteCanonical)
                revision = WriteConfigPresetDocumentLocked(document);

            foreach (var marker in legacyMarkersToWrite)
            {
                try
                {
                    WriteLegacyMigrationMarkerLocked(
                        marker.Key,
                        document.Generation,
                        marker.Value);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    markerWarningCount++;
                }
            }

            return CreateStorageSnapshot(
                document,
                revision,
                migrationConflicts,
                normalizationConflicts,
                readResult.WasRecovered ? 1 : 0,
                legacyQuarantineCount,
                markerWarningCount);
        }

        private IReadOnlyList<string> GetLegacyConfigPresetPaths()
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NormalizeLegacyPathKey(LegacyConfigPresetPath)
            };
            foreach (ServerConfig.ServerInstance server in ServerConfig.Current.All.Values)
            {
                if (string.IsNullOrWhiteSpace(server?.Base))
                    continue;
                paths.Add(NormalizeLegacyPathKey(
                    Path.Combine(server.Base, "config-presets.json")));
            }
            return paths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static int MergeLegacyPresets(
            ConfigPresetStorageDocument document,
            IEnumerable<ConfigPresetDefinition> legacyPresets)
        {
            int conflictCount = 0;
            foreach (ConfigPresetDefinition legacyPreset in legacyPresets ??
                Enumerable.Empty<ConfigPresetDefinition>())
            {
                if (document.LegacyBlockedPresets.Contains(legacyPreset.Name))
                    continue;

                HashSet<string> blockedKeys = document.LegacyBlockedValues.TryGetValue(
                    legacyPreset.Name, out HashSet<string> blocked)
                    ? blocked
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ConfigPresetDefinition existing = document.Presets.FirstOrDefault(item =>
                    string.Equals(item.Name, legacyPreset.Name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    ConfigPresetDefinition migratedPreset = CloneConfigPresets(
                        new[] { legacyPreset }).First();
                    foreach (string blockedKey in blockedKeys)
                    {
                        migratedPreset.Values.Remove(blockedKey);
                        migratedPreset.ValueFormats.Remove(blockedKey);
                    }
                    if (migratedPreset.Values.Count > 0)
                        document.Presets.Add(migratedPreset);
                    continue;
                }

                foreach (var value in legacyPreset.Values ?? new Dictionary<string, string>())
                {
                    if (blockedKeys.Contains(value.Key))
                        continue;

                    bool valueConflict = false;
                    if (!existing.Values.ContainsKey(value.Key))
                    {
                        existing.Values[value.Key] = value.Value ?? string.Empty;
                        if (legacyPreset.ValueFormats != null &&
                            legacyPreset.ValueFormats.TryGetValue(value.Key, out string newFormat))
                            existing.ValueFormats[value.Key] = newFormat ?? string.Empty;
                        continue;
                    }

                    if (!string.Equals(
                        existing.Values[value.Key],
                        value.Value ?? string.Empty,
                        StringComparison.Ordinal))
                    {
                        conflictCount++;
                        valueConflict = true;
                    }

                    if (legacyPreset.ValueFormats == null ||
                        !legacyPreset.ValueFormats.TryGetValue(value.Key, out string legacyFormat))
                        continue;
                    if (!existing.ValueFormats.TryGetValue(value.Key, out string existingFormat))
                    {
                        existing.ValueFormats[value.Key] = legacyFormat ?? string.Empty;
                    }
                    else if (!valueConflict && !string.Equals(
                        existingFormat ?? string.Empty,
                        legacyFormat ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        conflictCount++;
                    }
                }
            }
            return conflictCount;
        }

        private static HashSet<string> GetOrCreateBlockedValueKeys(
            ConfigPresetStorageDocument document,
            string presetName)
        {
            if (!document.LegacyBlockedValues.TryGetValue(presetName, out HashSet<string> blockedKeys))
            {
                blockedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                document.LegacyBlockedValues[presetName] = blockedKeys;
            }
            return blockedKeys;
        }

        private static ConfigPresetDocumentReadResult ReadCanonicalDocumentLocked(bool recoverCorrupt)
        {
            if (!File.Exists(ConfigPresetPath))
            {
                return new ConfigPresetDocumentReadResult
                {
                    Document = CreateEmptyConfigPresetDocument(),
                    WasMissing = true
                };
            }

            try
            {
                return ReadConfigPresetDocument(ConfigPresetPath);
            }
            catch (ConfigPresetJsonException)
            {
                if (!recoverCorrupt)
                    throw;
                string quarantinePath = QuarantineCorruptPresetFileLocked(ConfigPresetPath);
                if (quarantinePath == null)
                    throw;
                return new ConfigPresetDocumentReadResult
                {
                    Document = CreateEmptyConfigPresetDocument(),
                    WasRecovered = true,
                    WasMissing = true
                };
            }
        }

        private static string NormalizeLegacyPathKey(string path)
        {
            try
            {
                return Path.GetFullPath(path).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path ?? string.Empty;
            }
        }

        private static ConfigPresetStorageDocument CreateEmptyConfigPresetDocument()
        {
            return new ConfigPresetStorageDocument
            {
                Generation = Guid.NewGuid().ToString("N")
            };
        }

        private static void WriteLegacyMigrationMarkerLocked(
            string markerPath,
            string generation,
            string legacyHash)
        {
            string markerJson = JsonConvert.SerializeObject(
                new LegacyConfigPresetMigrationMarker
                {
                    Generation = generation,
                    LegacyHash = legacyHash
                },
                Formatting.Indented);
            WriteTextFileAtomically(
                markerPath,
                markerJson,
                new UTF8Encoding(false));
        }

        private static LegacyConfigPresetMigrationMarker ReadMigrationMarkerLocked(
            string markerPath,
            out bool quarantined)
        {
            quarantined = false;
            if (!File.Exists(markerPath))
                return new LegacyConfigPresetMigrationMarker();

            string markerText = File.ReadAllText(markerPath, Encoding.UTF8).Trim();
            if (!markerText.StartsWith("{", StringComparison.Ordinal))
                return new LegacyConfigPresetMigrationMarker { LegacyHash = markerText };

            try
            {
                LegacyConfigPresetMigrationMarker marker =
                    JsonConvert.DeserializeObject<LegacyConfigPresetMigrationMarker>(markerText);
                return marker ?? new LegacyConfigPresetMigrationMarker();
            }
            catch (JsonException)
            {
                string quarantinePath = QuarantineCorruptPresetFileLocked(markerPath);
                if (quarantinePath == null)
                    throw;
                quarantined = true;
                return new LegacyConfigPresetMigrationMarker();
            }
        }

        private static string QuarantineCorruptPresetFileLocked(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                string quarantinePath = path + ".corrupt." + DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                    "." + Guid.NewGuid().ToString("N") + ".json";
                File.Move(path, quarantinePath);
                return quarantinePath;
            }
            catch
            {
                return null;
            }
        }

        private static ConfigPresetDocumentReadResult ReadConfigPresetDocument(string path)
        {
            try
            {
                byte[] fileBytes;
                using (FileStream stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (MemoryStream buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    fileBytes = buffer.ToArray();
                }

                string json;
                using (MemoryStream buffer = new MemoryStream(fileBytes))
                using (StreamReader reader = new StreamReader(
                    buffer, new UTF8Encoding(false, true), true))
                    json = reader.ReadToEnd();

                int normalizationConflicts = CountDuplicateJsonProperties(json);
                JsonLoadSettings loadSettings = new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Ignore
                };
                JToken root = JToken.Parse(json, loadSettings);
                ConfigPresetStorageDocument document = CreateEmptyConfigPresetDocument();
                bool wasLegacyArray = root.Type == JTokenType.Array;
                if (wasLegacyArray)
                {
                    document.Presets = ParseConfigPresetDefinitions(root, ref normalizationConflicts);
                    if (AreSamePath(path, ConfigPresetPath))
                    {
                        string legacyGeneration = ReadLegacyConfigPresetGenerationLocked();
                        if (!string.IsNullOrWhiteSpace(legacyGeneration))
                            document.Generation = legacyGeneration;
                    }
                }
                else if (root is JObject documentObject)
                {
                    ParseConfigPresetDocumentMetadata(
                        documentObject,
                        document,
                        ref normalizationConflicts);
                }
                else
                {
                    throw new JsonSerializationException("预设文件根节点必须是数组或对象。");
                }

                NormalizeConfigPresetDocument(document, ref normalizationConflicts);
                return new ConfigPresetDocumentReadResult
                {
                    Document = document,
                    Revision = ComputeBytesFingerprint(fileBytes),
                    NormalizationConflictCount = normalizationConflicts,
                    WasLegacyArray = wasLegacyArray
                };
            }
            catch (JsonException ex)
            {
                throw new ConfigPresetJsonException(path, ex);
            }
            catch (DecoderFallbackException ex)
            {
                throw new ConfigPresetJsonException(path, ex);
            }
        }

        private static List<ConfigPresetDefinition> ParseConfigPresetDefinitions(
            JToken root,
            ref int normalizationConflicts)
        {
            if (root == null || root.Type != JTokenType.Array)
                throw new JsonSerializationException("Presets 必须是数组。");

            List<ConfigPresetDefinition> presets = new List<ConfigPresetDefinition>();
            foreach (JToken token in root.Children())
            {
                JObject presetObject = token as JObject;
                if (presetObject == null)
                    throw new JsonSerializationException("预设项必须是对象。");

                string name = null;
                bool nameSeen = false;
                bool valuesSeen = false;
                bool valueFormatsSeen = false;
                Dictionary<string, string> values =
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> valueFormats =
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (JProperty property in presetObject.Properties())
                {
                    if (property.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        if (nameSeen)
                        {
                            normalizationConflicts++;
                            continue;
                        }
                        nameSeen = true;
                        name = TokenToConfigString(property.Value);
                        continue;
                    }

                    if (property.Name.Equals("Values", StringComparison.OrdinalIgnoreCase))
                    {
                        if (valuesSeen)
                        {
                            normalizationConflicts++;
                            continue;
                        }
                        valuesSeen = true;
                        JObject valuesObject = property.Value as JObject;
                        if (valuesObject == null && property.Value.Type != JTokenType.Null)
                            throw new JsonSerializationException("Values 必须是对象。");
                        if (valuesObject == null)
                            continue;

                        foreach (JProperty valueProperty in valuesObject.Properties())
                        {
                            if (!values.ContainsKey(valueProperty.Name))
                                values[valueProperty.Name] = TokenToConfigString(valueProperty.Value);
                            else
                                normalizationConflicts++;
                        }
                        continue;
                    }

                    if (property.Name.Equals("ValueFormats", StringComparison.OrdinalIgnoreCase))
                    {
                        if (valueFormatsSeen)
                        {
                            normalizationConflicts++;
                            continue;
                        }
                        valueFormatsSeen = true;
                        JObject formatsObject = property.Value as JObject;
                        if (formatsObject == null && property.Value.Type != JTokenType.Null)
                            throw new JsonSerializationException("ValueFormats 必须是对象。");
                        if (formatsObject == null)
                            continue;

                        foreach (JProperty formatProperty in formatsObject.Properties())
                        {
                            if (!valueFormats.ContainsKey(formatProperty.Name))
                                valueFormats[formatProperty.Name] =
                                    TokenToConfigString(formatProperty.Value);
                            else
                                normalizationConflicts++;
                        }
                    }
                }

                presets.Add(new ConfigPresetDefinition
                {
                    Name = name,
                    Values = values,
                    ValueFormats = valueFormats
                });
            }

            int normalizedConflicts;
            List<ConfigPresetDefinition> normalized = NormalizeConfigPresets(
                presets, out normalizedConflicts);
            normalizationConflicts += normalizedConflicts;
            return normalized;
        }

        private static void ParseConfigPresetDocumentMetadata(
            JObject documentObject,
            ConfigPresetStorageDocument document,
            ref int normalizationConflicts)
        {
            HashSet<string> seenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool generationSeen = false;
            bool presetsSeen = false;
            foreach (JProperty property in documentObject.Properties())
            {
                if (!seenProperties.Add(property.Name))
                {
                    normalizationConflicts++;
                    continue;
                }

                if (property.Name.Equals("Generation", StringComparison.OrdinalIgnoreCase))
                {
                    generationSeen = true;
                    document.Generation = TokenToConfigString(property.Value);
                }
                else if (property.Name.Equals("Presets", StringComparison.OrdinalIgnoreCase))
                {
                    presetsSeen = true;
                    document.Presets = ParseConfigPresetDefinitions(
                        property.Value, ref normalizationConflicts);
                }
                else if (property.Name.Equals("LegacyMigrations", StringComparison.OrdinalIgnoreCase))
                {
                    JObject migrations = property.Value as JObject;
                    if (migrations == null && property.Value.Type != JTokenType.Null)
                        throw new JsonSerializationException("LegacyMigrations 必须是对象。");
                    if (migrations == null)
                        continue;
                    foreach (JProperty migration in migrations.Properties())
                    {
                        string key = NormalizeLegacyPathKey(migration.Name);
                        if (!document.LegacyMigrations.ContainsKey(key))
                            document.LegacyMigrations[key] = TokenToConfigString(migration.Value);
                        else
                            normalizationConflicts++;
                    }
                }
                else if (property.Name.Equals("LegacyBlockedPresets", StringComparison.OrdinalIgnoreCase))
                {
                    JArray blockedPresets = property.Value as JArray;
                    if (blockedPresets == null && property.Value.Type != JTokenType.Null)
                        throw new JsonSerializationException("LegacyBlockedPresets 必须是数组。");
                    if (blockedPresets == null)
                        continue;
                    foreach (JToken token in blockedPresets)
                    {
                        if (!document.LegacyBlockedPresets.Add(TokenToConfigString(token)))
                            normalizationConflicts++;
                    }
                }
                else if (property.Name.Equals("LegacyBlockedValues", StringComparison.OrdinalIgnoreCase))
                {
                    JObject blockedValues = property.Value as JObject;
                    if (blockedValues == null && property.Value.Type != JTokenType.Null)
                        throw new JsonSerializationException("LegacyBlockedValues 必须是对象。");
                    if (blockedValues == null)
                        continue;
                    foreach (JProperty blockedPreset in blockedValues.Properties())
                    {
                        JArray keys = blockedPreset.Value as JArray;
                        if (keys == null && blockedPreset.Value.Type != JTokenType.Null)
                            throw new JsonSerializationException("LegacyBlockedValues 的值必须是数组。");
                        if (document.LegacyBlockedValues.ContainsKey(blockedPreset.Name))
                        {
                            normalizationConflicts++;
                            continue;
                        }
                        HashSet<string> blockedKeys =
                            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        document.LegacyBlockedValues[blockedPreset.Name] = blockedKeys;
                        if (keys == null)
                            continue;
                        foreach (JToken key in keys)
                        {
                            if (!blockedKeys.Add(TokenToConfigString(key)))
                                normalizationConflicts++;
                        }
                    }
                }
            }

            if (!presetsSeen)
                throw new JsonSerializationException("配置预设文档缺少 Presets 数组。");
            if (!generationSeen)
                normalizationConflicts++;
        }

        private static void NormalizeConfigPresetDocument(
            ConfigPresetStorageDocument document,
            ref int normalizationConflicts)
        {
            if (!Guid.TryParseExact(document.Generation, "N", out Guid _))
            {
                document.Generation = Guid.NewGuid().ToString("N");
                normalizationConflicts++;
            }

            Dictionary<string, string> migrations =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var migration in document.LegacyMigrations ??
                new Dictionary<string, string>())
            {
                string path = NormalizeLegacyPathKey(migration.Key);
                string hash = migration.Value?.Trim();
                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(hash) ||
                    migrations.ContainsKey(path))
                {
                    normalizationConflicts++;
                    continue;
                }
                migrations[path] = hash;
            }
            document.LegacyMigrations = migrations;

            HashSet<string> blockedPresets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string blockedName in document.LegacyBlockedPresets ??
                new HashSet<string>())
            {
                string name = blockedName?.Trim();
                if (!IsValidPresetName(name) || !blockedPresets.Add(name))
                    normalizationConflicts++;
            }
            document.LegacyBlockedPresets = blockedPresets;

            Dictionary<string, HashSet<string>> blockedValues =
                new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var blockedPreset in document.LegacyBlockedValues ??
                new Dictionary<string, HashSet<string>>())
            {
                string name = blockedPreset.Key?.Trim();
                if (!IsValidPresetName(name) || blockedValues.ContainsKey(name))
                {
                    normalizationConflicts++;
                    continue;
                }
                HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string blockedKey in blockedPreset.Value ?? new HashSet<string>())
                {
                    string key = blockedKey?.Trim();
                    if (!IsValidConfigPresetKey(key) || !keys.Add(key))
                        normalizationConflicts++;
                }
                if (keys.Count > 0)
                    blockedValues[name] = keys;
            }
            document.LegacyBlockedValues = blockedValues;
        }

        private static string ReadLegacyConfigPresetGenerationLocked()
        {
            if (!File.Exists(LegacyConfigPresetGenerationPath))
                return null;
            return File.ReadAllText(LegacyConfigPresetGenerationPath, Encoding.UTF8).Trim();
        }

        private static string ComputeBytesFingerprint(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
                return Convert.ToBase64String(sha256.ComputeHash(bytes ?? Array.Empty<byte>()));
        }

        private static string TokenToConfigString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return string.Empty;
            if (token.Type == JTokenType.String)
                return token.Value<string>() ?? string.Empty;
            return token.ToString(Formatting.None);
        }

        private static int CountDuplicateJsonProperties(string json)
        {
            int duplicateCount = 0;
            Stack<HashSet<string>> objectProperties = new Stack<HashSet<string>>();
            using (JsonTextReader reader = new JsonTextReader(new StringReader(json)))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        objectProperties.Push(new HashSet<string>(StringComparer.Ordinal));
                    }
                    else if (reader.TokenType == JsonToken.PropertyName && objectProperties.Count > 0)
                    {
                        string propertyName = Convert.ToString(reader.Value) ?? string.Empty;
                        if (!objectProperties.Peek().Add(propertyName))
                            duplicateCount++;
                    }
                    else if (reader.TokenType == JsonToken.EndObject && objectProperties.Count > 0)
                    {
                        objectProperties.Pop();
                    }
                }
            }
            return duplicateCount;
        }

        private static List<ConfigPresetDefinition> NormalizeConfigPresets(IEnumerable<ConfigPresetDefinition> presets)
        {
            int ignoredConflicts;
            return NormalizeConfigPresets(presets, out ignoredConflicts);
        }

        private static List<ConfigPresetDefinition> NormalizeConfigPresets(
            IEnumerable<ConfigPresetDefinition> presets, out int conflictCount)
        {
            conflictCount = 0;
            List<ConfigPresetDefinition> normalized = new List<ConfigPresetDefinition>();
            Dictionary<string, ConfigPresetDefinition> byName =
                new Dictionary<string, ConfigPresetDefinition>(StringComparer.OrdinalIgnoreCase);

            if (presets == null)
                return normalized;

            foreach (ConfigPresetDefinition preset in presets)
            {
                if (preset == null || !IsValidPresetName(preset.Name?.Trim()))
                {
                    conflictCount++;
                    continue;
                }

                string name = preset.Name.Trim();
                if (!string.Equals(name, preset.Name, StringComparison.Ordinal))
                    conflictCount++;
                if (name.Length > 80)
                {
                    conflictCount++;
                    continue;
                }
                if (!byName.TryGetValue(name, out ConfigPresetDefinition target))
                {
                    target = new ConfigPresetDefinition
                    {
                        Name = name,
                        Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                        ValueFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    };
                    byName[name] = target;
                    normalized.Add(target);
                }
                else
                {
                    conflictCount++;
                    continue;
                }

                foreach (var value in preset.Values ?? new Dictionary<string, string>())
                {
                    string key = value.Key?.Trim();
                    string configValue = value.Value ?? string.Empty;
                    if (!IsValidConfigPresetKey(key) ||
                        configValue.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                    {
                        conflictCount++;
                        continue;
                    }
                    if (!string.Equals(key, value.Key, StringComparison.Ordinal))
                        conflictCount++;
                    if (!target.Values.ContainsKey(key))
                    {
                        target.Values[key] = configValue;
                        if (preset.ValueFormats != null &&
                            preset.ValueFormats.TryGetValue(value.Key, out string format))
                            target.ValueFormats[key] = format ?? string.Empty;
                    }
                    else
                    {
                        conflictCount++;
                    }
                }
            }

            return normalized;
        }

        private static bool IsValidPresetName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length <= 80 &&
                name.IndexOfAny(new[] { '\r', '\n' }) < 0;
        }

        private static bool IsValidConfigPresetKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key) &&
                !key.StartsWith("#", StringComparison.Ordinal) &&
                key.IndexOfAny(new[] { '=', '\r', '\n' }) < 0;
        }

        private static string WriteConfigPresetDocumentLocked(ConfigPresetStorageDocument document)
        {
            int ignoredConflicts = 0;
            document.Presets = NormalizeConfigPresets(document.Presets, out ignoredConflicts);
            NormalizeConfigPresetDocument(document, ref ignoredConflicts);

            ConfigPresetStorageFile storageFile = new ConfigPresetStorageFile
            {
                Version = 2,
                Generation = document.Generation,
                Presets = CloneConfigPresets(document.Presets),
                LegacyMigrations = document.LegacyMigrations
                    .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(item => item.Key, item => item.Value,
                        StringComparer.OrdinalIgnoreCase),
                LegacyBlockedPresets = document.LegacyBlockedPresets
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                LegacyBlockedValues = document.LegacyBlockedValues
                    .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value.OrderBy(
                            key => key, StringComparer.OrdinalIgnoreCase).ToList(),
                        StringComparer.OrdinalIgnoreCase)
            };
            string json = JsonConvert.SerializeObject(storageFile, Formatting.Indented);
            UTF8Encoding encoding = new UTF8Encoding(false);
            byte[] bytes = encoding.GetBytes(json);
            WriteTextFileAtomically(ConfigPresetPath, json, encoding);
            return ComputeBytesFingerprint(bytes);
        }

        private static string ComputeFileFingerprint(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Convert.ToBase64String(sha256.ComputeHash(stream));
            }
        }

        private static bool AreSamePath(string firstPath, string secondPath)
        {
            if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
                return false;
            try
            {
                return string.Equals(
                    Path.GetFullPath(firstPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetFullPath(secondPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(firstPath, secondPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static ConfigPresetStorageSnapshot CreateStorageSnapshot(
            ConfigPresetStorageDocument document,
            string revision,
            int migrationConflicts,
            int normalizationConflicts,
            int canonicalQuarantineCount,
            int legacyQuarantineCount,
            int markerWarningCount)
        {
            return new ConfigPresetStorageSnapshot
            {
                Presets = CloneConfigPresets(document.Presets),
                Revision = revision,
                MigrationConflictCount = migrationConflicts,
                NormalizationConflictCount = normalizationConflicts,
                CanonicalQuarantineCount = canonicalQuarantineCount,
                LegacyQuarantineCount = legacyQuarantineCount,
                MarkerWarningCount = markerWarningCount
            };
        }

        private static List<ConfigPresetDefinition> CloneConfigPresets(IEnumerable<ConfigPresetDefinition> presets)
        {
            return NormalizeConfigPresets(presets);
        }

        private Dictionary<string, string> GetSelectedConfigPresetValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in ConfigPresetItems.Where(item => item.IsSelected))
                values[item.Key] = item.Value ?? string.Empty;
            return values;
        }

        private Dictionary<string, string> GetSelectedConfigPresetValueFormats(
            IReadOnlyDictionary<string, string> values)
        {
            Dictionary<string, string> formats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                if (value.Key.Equals("gamemode", StringComparison.OrdinalIgnoreCase))
                    formats[value.Key] = GetConfigValueFormat(value.Value, true);
                else if (value.Key.Equals("difficulty", StringComparison.OrdinalIgnoreCase))
                    formats[value.Key] = GetConfigValueFormat(value.Value, false);
            }
            return formats;
        }

        private string GetConfigValueFormat(string value, bool gameMode)
        {
            string normalizedValue = value ?? string.Empty;
            if (gameMode)
            {
                int ignoredNumber;
                if (TryGetGameModeNumber(normalizedValue, out ignoredNumber) &&
                    int.TryParse(normalizedValue.Trim(), out ignoredNumber))
                    return "numeric";
                string ignoredName;
                if (TryGetGameModeName(normalizedValue, out ignoredName))
                    return "named";
            }
            else
            {
                int ignoredNumber;
                if (TryGetDifficultyNumber(normalizedValue, out ignoredNumber) &&
                    int.TryParse(normalizedValue.Trim(), out ignoredNumber))
                    return "numeric";
                string ignoredName;
                if (TryGetDifficultyName(normalizedValue, out ignoredName))
                    return "named";
            }
            return "unknown";
        }

        private string GetConfigPresetInputSignature()
        {
            StringBuilder builder = new StringBuilder();
            foreach (ConfigPresetItem item in ConfigPresetItems.OrderBy(item => item.Key,
                StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(item.Key).Append('\u001f')
                    .Append(item.IsSelected ? '1' : '0').Append('\u001f')
                    .Append(item.Value ?? string.Empty).Append('\u001e');
            }
            return builder.ToString();
        }

        private async void SaveConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            string name = ConfigPresetNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            {
                SetConfigPresetStatus("请输入预设名称。");
                ConfigPresetNameTextBox.Focus();
                return;
            }
            if (name.Length > 80)
            {
                SetConfigPresetStatus("预设名称不能超过 80 个字符。");
                ConfigPresetNameTextBox.Focus();
                return;
            }

            Dictionary<string, string> values = GetSelectedConfigPresetValues();
            if (values.Count == 0)
            {
                SetConfigPresetStatus("请至少选择一个配置值。");
                return;
            }
            if (values.Any(value => !IsValidConfigPresetKey(value.Key) ||
                (value.Value ?? string.Empty).IndexOfAny(new[] { '\r', '\n' }) >= 0))
            {
                SetConfigPresetStatus("配置项名称或配置值包含非法换行符，无法保存预设。");
                return;
            }

            string inputSignature = GetConfigPresetInputSignature();
            ConfigPresetDefinition selectedBeforeSave = _selectedConfigPreset;
            bool presetsWereExplicitlyLoaded = _configPresetsLoaded;
            if (!BeginConfigPresetOperation())
                return;
            int operationGeneration = _configPresetStateGeneration;
            try
            {
                Dictionary<string, string> valueFormats = GetSelectedConfigPresetValueFormats(values);
                ConfigPresetStorageSnapshot storage = await Task.Run(() => ReadConfigPresetsFromStorage());
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                ConfigPresetDefinition preset = storage.Presets.FirstOrDefault(
                    item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                if (preset != null)
                {
                    CancellationTokenSource confirmationCancellation =
                        CreateConfigPresetConfirmationCancellation();
                    bool overwrite;
                    try
                    {
                        overwrite = await MagicShow.ShowMsgDialogAsync(
                            FatherControl,
                            $"预设“{name}”已经存在，是否覆盖原预设？",
                            "覆盖预设",
                            true,
                            "取消",
                            "覆盖",
                            null,
                            true,
                            cancellationToken: confirmationCancellation.Token);
                    }
                    finally
                    {
                        ReleaseConfigPresetConfirmationCancellation(confirmationCancellation);
                    }
                    if (IsConfigPresetOperationStale(operationGeneration))
                        return;
                    if (!overwrite)
                    {
                        SetConfigPresetStatus("已取消覆盖预设。");
                        return;
                    }
                }
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                if (!string.Equals(name, ConfigPresetNameTextBox.Text?.Trim(), StringComparison.Ordinal) ||
                    !ReferenceEquals(selectedBeforeSave, _selectedConfigPreset) ||
                    !string.Equals(inputSignature, GetConfigPresetInputSignature(), StringComparison.Ordinal))
                {
                    SetConfigPresetStatus("预设名称、选择或内容在确认期间发生变化，请重新执行保存。");
                    return;
                }
                HashSet<string> visibleKeys = new HashSet<string>(
                    ConfigPresetItems.Select(item => item.Key),
                    StringComparer.OrdinalIgnoreCase);
                ConfigPresetStorageSnapshot latest = await Task.Run(() => UpsertConfigPreset(
                    name, values, valueFormats, visibleKeys, storage.Revision));
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                ReplaceVisibleConfigPresets(latest, presetsWereExplicitlyLoaded, new[] { name });
                _configPresetRevision = latest.Revision;
                _configPresetsLoaded = presetsWereExplicitlyLoaded;
                _selectedConfigPreset = ConfigPresets.FirstOrDefault(
                    item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                ConfigPresetNameTextBox.IsReadOnly = true;
                SaveConfigPresetButton.Visibility = Visibility.Collapsed;
                SetExistingConfigPresetActionsVisibility(_selectedConfigPreset == null
                    ? Visibility.Collapsed
                    : Visibility.Visible);
                configPresetButton.ClearValue(Button.StyleProperty);
                if (_selectedConfigPreset != null)
                {
                    Dictionary<string, string> savedValues = _selectedConfigPreset.Values ??
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    InitializeConfigPresetItems(savedValues, savedValues.Keys);
                    LoadConfigPresetIntoEditor(_selectedConfigPreset);
                }
                RenderConfigPresetButtons();
                SetConfigPresetStatus($"预设“{name}”已保存，共 {_selectedConfigPreset?.Values.Count ?? values.Count} 项。");
            }
            catch (ConfigPresetStorageBusyException)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设文件正在被其他进程使用，请稍后重试。");
            }
            catch (ConfigPresetConflictException)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设已被其他窗口修改，请重新加载后再保存。");
            }
            catch (Exception ex)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设保存失败：" + ex.Message);
            }
            finally
            {
                EndConfigPresetOperation();
            }
        }

        private async void LoadConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!CanUseConfigPresets())
                return;
            if (!BeginConfigPresetOperation())
                return;
            int operationGeneration = _configPresetStateGeneration;
            HideConfigPresetPanel();
            ConfigPresetItems.Clear();
            ConfigPresetNameTextBox.Clear();
            ConfigPresetNameTextBox.IsReadOnly = false;
            _selectedConfigPreset = null;
            configPresetButton.ClearValue(Button.StyleProperty);
            RenderConfigPresetButtons();
            try
            {
                await LoadConfigPresetsAsync(operationGeneration);
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
            }
            catch (Exception ex)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设界面更新失败：" + ex.Message);
            }
            finally
            {
                EndConfigPresetOperation();
            }
        }

        private async void ApplyConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!BeginConfigPresetOperation())
                return;
            int operationGeneration = _configPresetStateGeneration;
            try
            {
                if (_selectedConfigPreset == null)
                {
                    SetConfigPresetStatus("请先选择并加载一个已有预设。");
                    return;
                }

                string loadedPresetRevision = _configPresetRevision;
                ConfigPresetStorageSnapshot currentStorage = await Task.Run(
                    () => ReadConfigPresetsFromStorage());
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                if (!string.Equals(loadedPresetRevision, currentStorage.Revision,
                    StringComparison.Ordinal))
                {
                    SetConfigPresetStatus("全局预设已被其他窗口修改，请重新加载后再应用。");
                    return;
                }

                Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in ConfigPresetItems.Where(item => item.IsSelected))
                {
                    if (_selectedConfigPreset.Values.TryGetValue(item.Key, out string value))
                        values[item.Key] = value ?? string.Empty;
                }
                if (values.Count == 0)
                {
                    SetConfigPresetStatus("请至少选择一个配置值。");
                    return;
                }

                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                string fileRevision = _serverPropertiesRevision;
                if (string.IsNullOrEmpty(fileRevision))
                {
                    SetConfigPresetStatus("server.properties 状态尚未加载，请先刷新后重试。");
                    return;
                }
                Dictionary<string, string> valueFormats = new Dictionary<string, string>(
                    _selectedConfigPreset.ValueFormats ?? new Dictionary<string, string>(),
                    StringComparer.OrdinalIgnoreCase);
                string error = await Task.Run(() => ApplyPresetValues(values, valueFormats, fileRevision));
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                if (string.IsNullOrWhiteSpace(error))
                {
                    RefreshServerConfig(true);
                    foreach (var item in ConfigPresetItems)
                        item.IsSelected = true;
                }
                SetConfigPresetStatus(string.IsNullOrWhiteSpace(error)
                    ? $"已应用 {values.Count} 项配置。"
                    : error);
            }
            catch (ConfigPresetStorageBusyException)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设文件正在被其他进程使用，请稍后重试。");
            }
            catch (Exception ex)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("配置应用失败：" + ex.Message);
            }
            finally
            {
                EndConfigPresetOperation();
            }
        }

        private async void DeleteConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            if (!BeginConfigPresetOperation())
                return;
            int operationGeneration = _configPresetStateGeneration;
            if (_selectedConfigPreset == null)
            {
                SetConfigPresetStatus("请先选择一个预设。");
                EndConfigPresetOperation();
                return;
            }

            try
            {
                string presetName = _selectedConfigPreset.Name;
                ConfigPresetDefinition selectedBeforeDelete = _selectedConfigPreset;
                bool presetsWereExplicitlyLoaded = _configPresetsLoaded;
                ConfigPresetStorageSnapshot storage = await Task.Run(() => ReadConfigPresetsFromStorage());
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                if (!ReferenceEquals(selectedBeforeDelete, _selectedConfigPreset) ||
                    !string.Equals(presetName, _selectedConfigPreset?.Name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    SetConfigPresetStatus("当前选择在确认期间发生变化，请重新加载后再删除。");
                    return;
                }
                ConfigPresetDefinition preset = storage.Presets.FirstOrDefault(item =>
                    string.Equals(item.Name, presetName, StringComparison.OrdinalIgnoreCase));
                if (preset == null)
                {
                    SetConfigPresetStatus("预设已不存在，请重新加载预设列表。");
                    return;
                }
                CancellationTokenSource confirmationCancellation =
                    CreateConfigPresetConfirmationCancellation();
                bool confirmed;
                try
                {
                    confirmed = await MagicShow.ShowMsgDialogAsync(
                        FatherControl,
                        $"预设“{preset.Name}”是全局预设，删除后所有服务器都将无法使用它，是否继续？",
                        "删除预设",
                        true,
                        "取消",
                        "删除",
                        null,
                        true,
                        cancellationToken: confirmationCancellation.Token);
                }
                finally
                {
                    ReleaseConfigPresetConfirmationCancellation(confirmationCancellation);
                }
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                if (!confirmed)
                {
                    SetConfigPresetStatus("已取消删除预设。");
                    return;
                }
                if (!ReferenceEquals(selectedBeforeDelete, _selectedConfigPreset) ||
                    !string.Equals(presetName, _selectedConfigPreset?.Name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    SetConfigPresetStatus("当前选择在确认期间发生变化，请重新加载后再删除。");
                    return;
                }

                ConfigPresetStorageSnapshot latest = await Task.Run(() => DeleteConfigPresetFromStorage(
                    preset.Name, storage.Revision));
                if (IsConfigPresetOperationStale(operationGeneration))
                    return;
                ReplaceVisibleConfigPresets(latest, presetsWereExplicitlyLoaded);
                _configPresetRevision = latest.Revision;
                _selectedConfigPreset = null;
                ConfigPresetNameTextBox.Clear();
                ConfigPresetNameTextBox.IsReadOnly = false;
                RenderConfigPresetButtons();
                ConfigPresetItems.Clear();
                SetConfigPresetStatus($"预设“{preset.Name}”已删除。");
                HideConfigPresetPanel();
            }
            catch (ConfigPresetStorageBusyException)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设文件正在被其他进程使用，请稍后重试。");
            }
            catch (ConfigPresetConflictException)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设已被其他窗口修改，请重新加载后再删除。");
            }
            catch (Exception ex)
            {
                if (!IsConfigPresetOperationStale(operationGeneration))
                    SetConfigPresetStatus("预设删除失败：" + ex.Message);
            }
            finally
            {
                EndConfigPresetOperation();
            }
        }

        private void SelectAllConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ConfigPresetItems)
                item.IsSelected = true;
            SetConfigPresetStatus("已选择全部配置值。");
        }

        private void SelectNoneConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ConfigPresetItems)
                item.IsSelected = false;
            SetConfigPresetStatus("已取消选择全部配置值。");
        }

        private void SetConfigPresetStatus(string message)
        {
            ConfigPresetStatusText.Text = message ?? string.Empty;
            ConfigPresetStatusText.Visibility = string.IsNullOrWhiteSpace(message)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private Dictionary<string, string> GetCurrentConfigValues()
        {
            if (configTextBoxes.Count == 0)
                return GetAllConfigs();

            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in configTextBoxes)
                values[item.Key] = item.Value.Text ?? string.Empty;
            return values;
        }

        /// <summary>
        /// 将预设中选中的值写回 server.properties；返回 null 表示应用成功。
        /// </summary>
        private string ApplyPresetValues(
            IReadOnlyDictionary<string, string> values,
            IReadOnlyDictionary<string, string> valueFormats,
            string expectedRevision)
        {
            string propertiesPath = Path.Combine(Rserverbase, "server.properties");
            try
            {
                return WithServerPropertiesFileLock(
                    propertiesPath,
                    () => ApplyPresetValuesLocked(
                        values, valueFormats, expectedRevision, propertiesPath));
            }
            catch (ServerPropertiesStorageBusyException)
            {
                return "server.properties 正在被其他操作使用，请稍后重试。";
            }
        }

        private string ApplyPresetValuesLocked(
            IReadOnlyDictionary<string, string> values,
            IReadOnlyDictionary<string, string> valueFormats,
            string expectedRevision,
            string propertiesPath)
        {
            try
            {
                if (FatherService.CheckServerRunning())
                    return "服务器运行时无法应用配置预设！";

                if (!File.Exists(propertiesPath))
                    return "配置文件不存在！";
                if (!string.Equals(expectedRevision, ComputeFileFingerprint(propertiesPath), StringComparison.Ordinal))
                    return "server.properties 已被其他操作修改，请刷新后重试。";

                foreach (var value in values)
                {
                    if (string.IsNullOrWhiteSpace(value.Key) ||
                        value.Key.Trim() != value.Key ||
                        value.Key.StartsWith("#", StringComparison.Ordinal) ||
                        value.Key.IndexOfAny(new[] { '=', '\r', '\n' }) >= 0 ||
                        (value.Value ?? string.Empty).IndexOfAny(new[] { '\r', '\n' }) >= 0)
                        return "预设包含非法配置项或换行符，无法应用。";
                }

                Encoding encoding = Functions.GetTextFileEncodingType(propertiesPath);
                string originalText = File.ReadAllText(propertiesPath, encoding);
                string lineEnding;
                bool endsWithLineEnding;
                List<string> lines = SplitTextLines(
                    originalText, out lineEnding, out endsWithLineEnding);
                Dictionary<string, string> currentValues = ParseConfigValues(lines);
                HashSet<string> foundKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool hasChanges = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    string originalLine = lines[i];
                    string trimmedLine = originalLine.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    int separatorIndex = originalLine.IndexOf('=');
                    if (separatorIndex <= 0)
                        continue;

                    string key = originalLine.Substring(0, separatorIndex).Trim();
                    if (!values.TryGetValue(key, out string newValue))
                        continue;

                    foundKeys.Add(key);
                    newValue = newValue ?? string.Empty;
                    string rawValue = originalLine.Substring(separatorIndex + 1);
                    string oldValue = rawValue.Trim();
                    string conversionError;
                    newValue = ConvertLegacyConfigValue(key, newValue, oldValue, out conversionError);
                    if (!string.IsNullOrEmpty(conversionError))
                        return conversionError;
                    if (newValue == oldValue)
                        continue;

                    lines[i] = ReplaceConfigValueInLine(originalLine, separatorIndex, newValue);
                    hasChanges = true;
                }

                foreach (var value in values)
                {
                    if (foundKeys.Contains(value.Key))
                        continue;

                    string conversionError;
                    string convertedValue = ConvertMissingConfigValue(
                        value.Key,
                        value.Value ?? string.Empty,
                        valueFormats,
                        currentValues,
                        out conversionError);
                    if (!string.IsNullOrEmpty(conversionError))
                        return conversionError;
                    lines.Add(value.Key + "=" + convertedValue);
                    hasChanges = true;
                }

                if (!hasChanges)
                    return null;

                if (!string.Equals(expectedRevision, ComputeFileFingerprint(propertiesPath), StringComparison.Ordinal))
                    return "server.properties 已被其他操作修改，请刷新后重试。";

                string updatedText = string.Join(lineEnding, lines);
                if (endsWithLineEnding)
                    updatedText += lineEnding;
                WriteTextFileAtomically(
                    propertiesPath,
                    updatedText,
                    encoding == Encoding.UTF8 ? new UTF8Encoding(false) : encoding);

                return null;
            }
            catch (Exception ex)
            {
                return "配置应用失败：" + ex.Message;
            }
        }

        private static string ReplaceConfigValueInLine(
            string originalLine,
            int separatorIndex,
            string newValue)
        {
            string prefix = originalLine.Substring(0, separatorIndex + 1);
            string rawValue = originalLine.Substring(separatorIndex + 1);
            string valuePart = rawValue;
            int leadingWhitespaceLength = valuePart.Length - valuePart.TrimStart().Length;
            int trailingWhitespaceLength = valuePart.Length - valuePart.TrimEnd().Length;
            string leadingWhitespace = valuePart.Substring(0, leadingWhitespaceLength);
            if (leadingWhitespaceLength + trailingWhitespaceLength > valuePart.Length)
                trailingWhitespaceLength = 0;
            string trailingWhitespace = trailingWhitespaceLength == 0
                ? string.Empty
                : valuePart.Substring(valuePart.Length - trailingWhitespaceLength);
            return prefix + leadingWhitespace + newValue + trailingWhitespace;
        }

        private static List<string> SplitTextLines(
            string text,
            out string lineEnding,
            out bool endsWithLineEnding)
        {
            text = text ?? string.Empty;
            Match firstLineEnding = Regex.Match(text, "\\r\\n|\\n|\\r");
            lineEnding = firstLineEnding.Success ? firstLineEnding.Value : Environment.NewLine;
            endsWithLineEnding = text.EndsWith("\r\n", StringComparison.Ordinal) ||
                text.EndsWith("\n", StringComparison.Ordinal) ||
                text.EndsWith("\r", StringComparison.Ordinal);
            if (text.Length == 0)
                return new List<string>();
            List<string> lines = Regex.Split(text, "\\r\\n|\\n|\\r").ToList();
            if (endsWithLineEnding && lines.Count > 0 && lines[lines.Count - 1].Length == 0)
                lines.RemoveAt(lines.Count - 1);
            return lines;
        }

        private static Dictionary<string, string> ParseConfigValues(IEnumerable<string> lines)
        {
            Dictionary<string, string> values =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in lines ?? Enumerable.Empty<string>())
            {
                string trimmedLine = (line ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;
                int separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;
                string key = trimmedLine.Substring(0, separatorIndex).Trim();
                if (!values.ContainsKey(key))
                    values[key] = trimmedLine.Substring(separatorIndex + 1).Trim();
            }
            return values;
        }

        private string ConvertMissingConfigValue(
            string key,
            string presetValue,
            IReadOnlyDictionary<string, string> valueFormats,
            IReadOnlyDictionary<string, string> currentValues,
            out string error)
        {
            error = null;
            string targetFormat = DetectServerConfigValueFormat(currentValues);
            if (valueFormats != null && valueFormats.TryGetValue(key, out string presetFormat) &&
                !string.Equals(presetFormat, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                if (targetFormat == null)
                    targetFormat = presetFormat;
            }
            if (key.Equals("gamemode", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(targetFormat, "numeric", StringComparison.OrdinalIgnoreCase) &&
                    TryGetGameModeNumber(presetValue, out int gameModeNumber))
                    return gameModeNumber.ToString();
                if (string.Equals(targetFormat, "named", StringComparison.OrdinalIgnoreCase) &&
                    TryGetGameModeName(presetValue, out string gameModeName))
                    return gameModeName;
                error = "无法确定 gamemode 的数字/文本格式，未应用该配置项。";
                return null;
            }
            else if (key.Equals("difficulty", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(targetFormat, "numeric", StringComparison.OrdinalIgnoreCase) &&
                    TryGetDifficultyNumber(presetValue, out int difficultyNumber))
                    return difficultyNumber.ToString();
                if (string.Equals(targetFormat, "named", StringComparison.OrdinalIgnoreCase) &&
                    TryGetDifficultyName(presetValue, out string difficultyName))
                    return difficultyName;
                error = "无法确定 difficulty 的数字/文本格式，未应用该配置项。";
                return null;
            }

            return presetValue;
        }

        private string DetectServerConfigValueFormat(IReadOnlyDictionary<string, string> currentValues)
        {
            string detectedFormat = null;
            if (currentValues != null && currentValues.TryGetValue("gamemode", out string gameMode))
                detectedFormat = GetConfigValueFormat(gameMode, true);
            if (currentValues != null && currentValues.TryGetValue("difficulty", out string difficulty))
            {
                string difficultyFormat = GetConfigValueFormat(difficulty, false);
                if (string.IsNullOrEmpty(detectedFormat) ||
                    string.Equals(detectedFormat, "unknown", StringComparison.OrdinalIgnoreCase))
                    detectedFormat = difficultyFormat;
                else if (!string.Equals(difficultyFormat, "unknown", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(detectedFormat, difficultyFormat, StringComparison.OrdinalIgnoreCase))
                    detectedFormat = null;
            }
            if (!string.IsNullOrEmpty(detectedFormat) &&
                !string.Equals(detectedFormat, "unknown", StringComparison.OrdinalIgnoreCase))
                return detectedFormat;

            string core = FatherService?.ServerCore;
            if (string.IsNullOrWhiteSpace(core))
                return null;

            Match match = Regex.Match(
                Path.GetFileNameWithoutExtension(core),
                @"(?<!\d)(1\.\d+(?:\.\d+){0,2})(?!\d)",
                RegexOptions.IgnoreCase);
            if (!match.Success || !Version.TryParse(match.Groups[1].Value, out Version version))
                return null;
            return version < new Version("1.13") ? "numeric" : "named";
        }

        private static void WriteLinesAtomically(string path, IEnumerable<string> lines, Encoding encoding)
        {
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllLines(tempPath, lines, encoding);
                ReplaceFileAtomically(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private static void WriteTextFileAtomically(string path, string content, Encoding encoding)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(tempPath, content, encoding);
                ReplaceFileAtomically(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private static void ReplaceFileAtomically(string tempPath, string targetPath)
        {
            if (File.Exists(targetPath))
                File.Replace(tempPath, targetPath, null);
            else
                File.Move(tempPath, targetPath);
        }

        private string ConvertLegacyConfigValue(
            string key,
            string presetValue,
            string targetValue,
            out string error)
        {
            error = null;
            presetValue = presetValue ?? string.Empty;
            targetValue = targetValue ?? string.Empty;

            if (key.Equals("gamemode", StringComparison.OrdinalIgnoreCase))
            {
                bool targetUsesNumber = int.TryParse(targetValue.Trim(), out _) &&
                    TryGetGameModeNumber(targetValue, out _);
                if (targetUsesNumber && TryGetGameModeNumber(presetValue, out int gameModeNumber))
                    return gameModeNumber.ToString();
                bool targetUsesName = !targetUsesNumber &&
                    TryGetGameModeName(targetValue, out _);
                if (targetUsesName && TryGetGameModeName(presetValue, out string gameModeName))
                    return gameModeName;
                error = targetUsesNumber || targetUsesName
                    ? "gamemode 的预设值无法转换为目标服务器使用的格式。"
                    : "无法确定 server.properties 中 gamemode 当前值的数字/文本格式，未应用该配置项。";
                return null;
            }
            else if (key.Equals("difficulty", StringComparison.OrdinalIgnoreCase))
            {
                bool targetUsesNumber = int.TryParse(targetValue.Trim(), out _) &&
                    TryGetDifficultyNumber(targetValue, out _);
                if (targetUsesNumber && TryGetDifficultyNumber(presetValue, out int difficultyNumber))
                    return difficultyNumber.ToString();
                bool targetUsesName = !targetUsesNumber &&
                    TryGetDifficultyName(targetValue, out _);
                if (targetUsesName && TryGetDifficultyName(presetValue, out string difficultyName))
                    return difficultyName;
                error = targetUsesNumber || targetUsesName
                    ? "difficulty 的预设值无法转换为目标服务器使用的格式。"
                    : "无法确定 server.properties 中 difficulty 当前值的数字/文本格式，未应用该配置项。";
                return null;
            }

            return presetValue;
        }

        private bool TryGetGameModeNumber(string value, out int number)
        {
            if (int.TryParse(value.Trim(), out number))
                return number >= 0 && number <= 3;

            switch (value.Trim().ToLowerInvariant())
            {
                case "survival": number = 0; return true;
                case "creative": number = 1; return true;
                case "adventure": number = 2; return true;
                case "spectator": number = 3; return true;
                default: number = 0; return false;
            }
        }

        private bool TryGetGameModeName(string value, out string name)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "0": case "survival": name = "survival"; return true;
                case "1": case "creative": name = "creative"; return true;
                case "2": case "adventure": name = "adventure"; return true;
                case "3": case "spectator": name = "spectator"; return true;
                default: name = value; return false;
            }
        }

        private bool TryGetDifficultyNumber(string value, out int number)
        {
            if (int.TryParse(value.Trim(), out number))
                return number >= 0 && number <= 3;

            switch (value.Trim().ToLowerInvariant())
            {
                case "peaceful": number = 0; return true;
                case "easy": number = 1; return true;
                case "normal": number = 2; return true;
                case "hard": number = 3; return true;
                default: number = 0; return false;
            }
        }

        private bool TryGetDifficultyName(string value, out string name)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "0": case "peaceful": name = "peaceful"; return true;
                case "1": case "easy": name = "easy"; return true;
                case "2": case "normal": name = "normal"; return true;
                case "3": case "hard": name = "hard"; return true;
                default: name = value; return false;
            }
        }

        #endregion
    }

    internal sealed class ConfigPresetStorageSnapshot
    {
        public List<ConfigPresetDefinition> Presets { get; set; } = new List<ConfigPresetDefinition>();
        public string Revision { get; set; }
        public int MigrationConflictCount { get; set; }
        public int NormalizationConflictCount { get; set; }
        public int CanonicalQuarantineCount { get; set; }
        public int LegacyQuarantineCount { get; set; }
        public int MarkerWarningCount { get; set; }
    }

    internal sealed class ConfigPresetDocumentReadResult
    {
        public ConfigPresetStorageDocument Document { get; set; } = new ConfigPresetStorageDocument();
        public string Revision { get; set; }
        public int NormalizationConflictCount { get; set; }
        public bool WasLegacyArray { get; set; }
        public bool WasMissing { get; set; }
        public bool WasRecovered { get; set; }
    }

    internal sealed class ConfigPresetStorageDocument
    {
        public string Generation { get; set; }
        public List<ConfigPresetDefinition> Presets { get; set; } = new List<ConfigPresetDefinition>();
        public Dictionary<string, string> LegacyMigrations { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> LegacyBlockedPresets { get; set; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, HashSet<string>> LegacyBlockedValues { get; set; } =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class ConfigPresetStorageFile
    {
        public int Version { get; set; }
        public string Generation { get; set; }
        public List<ConfigPresetDefinition> Presets { get; set; } = new List<ConfigPresetDefinition>();
        public Dictionary<string, string> LegacyMigrations { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public List<string> LegacyBlockedPresets { get; set; } = new List<string>();
        public Dictionary<string, List<string>> LegacyBlockedValues { get; set; } =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class LegacyConfigPresetMigrationMarker
    {
        public string Generation { get; set; }
        public string LegacyHash { get; set; }
    }

    internal sealed class ConfigPresetStorageBusyException : IOException
    {
        public ConfigPresetStorageBusyException()
            : base("配置预设文件正在被其他进程使用。")
        {
        }
    }

    internal sealed class ConfigPresetConflictException : IOException
    {
        public ConfigPresetConflictException()
            : base("配置预设文件已被其他操作修改。")
        {
        }
    }

    internal sealed class ServerPropertiesStorageBusyException : IOException
    {
        public ServerPropertiesStorageBusyException()
            : base("server.properties 正在被其他操作使用。")
        {
        }
    }

    internal sealed class ServerPropertiesConflictException : IOException
    {
        public ServerPropertiesConflictException()
            : base("server.properties 已被其他操作修改。")
        {
        }
    }

    public sealed class ConfigPresetDefinition
    {
        public string Name { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ValueFormats { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class ConfigPresetJsonException : Exception
    {
        public ConfigPresetJsonException(string filePath, Exception innerException)
            : base("配置预设文件格式无效。", innerException)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }

    public sealed class ConfigPresetItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _value;

        public string Key { get; set; }

        public string Value
        {
            get => _value;
            set
            {
                if (string.Equals(_value, value, StringComparison.Ordinal))
                    return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
