using HandyControl.Controls;
using Microsoft.Win32;
using MSL.langs;
using MSL.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
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
        private static readonly object ConfigPresetFileLock = new object();
        private readonly string LegacyConfigPresetPath;
        private Dictionary<string, TextBox> configTextBoxes = new Dictionary<string, TextBox>();
        private readonly List<string> configKeyOrder = new List<string>();
        private ConfigPresetDefinition _selectedConfigPreset;
        public ObservableCollection<ConfigPresetDefinition> ConfigPresets { get; } = new ObservableCollection<ConfigPresetDefinition>();
        public ObservableCollection<ConfigPresetItem> ConfigPresetItems { get; } = new ObservableCollection<ConfigPresetItem>();

        #region 核心函数

        /// <summary>
        /// 读取指定配置项的值
        /// </summary>
        /// <param name="key">配置项键名</param>
        /// <returns>配置项值，如果不存在返回null</returns>
        public string GetConfigValue(string key)
        {
            Dictionary<string, string> configs = GetAllConfigs();
            return configs.TryGetValue(key, out string value) ? value : null;
        }

        /// <summary>
        /// 读取所有配置项
        /// </summary>
        /// <returns>配置项字典</returns>
        private Dictionary<string, string> GetAllConfigs()
        {
            Dictionary<string, string> configs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                if (!File.Exists(propertiesPath))
                    return configs;

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
                        string key = trimmedLine.Substring(0, separatorIndex).Trim();
                        string value = trimmedLine.Substring(separatorIndex + 1).Trim();
                        if (!configs.ContainsKey(key))
                        {
                            configs[key] = value;
                        }
                    }
                }
            }
            catch
            {
            }
            return configs;
        }

        public void ClearConfigPresetState()
        {
            ResetConfigPresetToolbar();
        }
        #endregion

        #region 服务器功能调整

        private void refreahServerConfig_Click(object sender, RoutedEventArgs e)
        {
            RefreshServerConfig();
            Growl.Success(LanguageManager.Instance["Page_ServerList_RefreshSuccess"]);
        }

        public void RefreshServerConfig()
        {
            try
            {
                Dictionary<string, string> serverConfigCache = GetAllConfigs();

                if (serverConfigCache.Count == 0)
                {
                    changeServerPropertiesLab.Text = "服务器配置（未找到文件，无法更改基础配置，运行一下服务器再试）";
                    saveServerConfig.IsEnabled = false;
                    LoadSelectedConfigPresetButton.IsEnabled = false;
                    configPresetButton.IsEnabled = false;
                    ChangeServerProperties.Visibility = Visibility.Collapsed;
                    ResetConfigPresetToolbar();
                    return;
                }

                changeServerPropertiesLab.Text = LanguageManager.Instance["SR_ServerConfig"];
                saveServerConfig.IsEnabled = true;
                LoadSelectedConfigPresetButton.IsEnabled = true;
                configPresetButton.IsEnabled = true;
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
                    else
                        InitializeConfigPresetItems(GetCurrentConfigValues());
                    if (_selectedConfigPreset != null)
                        LoadConfigPresetIntoEditor(_selectedConfigPreset);
                }
            }
            catch
            {
                changeServerPropertiesLab.Text = "找不到配置文件，无法更改相关设置（请尝试开启一次服务器）";
                configPresetButton.IsEnabled = false;
                LoadSelectedConfigPresetButton.IsEnabled = false;
                ChangeServerProperties.Visibility = Visibility.Collapsed;
                ResetConfigPresetToolbar();
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
            configTextBoxes[key] = textBox;
            configKeyOrder.Add(key);
        }

        private void saveServerConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FatherService.CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        LanguageManager.Instance["SR_CantChangeWhileRunning"],
                        LanguageManager.Instance["Error"]);
                    return;
                }
                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                if (!File.Exists(propertiesPath))
                {
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        LanguageManager.Instance["SR_ServerConfigFileMissing"],
                        LanguageManager.Instance["Error"]);
                    return;
                }

                Encoding encoding = Functions.GetTextFileEncodingType(propertiesPath);
                string[] lines = File.ReadAllLines(propertiesPath, encoding);
                bool hasChanges = false;

                // 逐行检查并更新配置
                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmedLine = lines[i].Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string key = trimmedLine.Substring(0, separatorIndex).Trim();

                        // 检查是否有对应的配置
                        if (configTextBoxes.ContainsKey(key))
                        {
                            string newValue = configTextBoxes[key].Text.Trim();
                            string oldValue = trimmedLine.Substring(separatorIndex + 1).Trim();

                            if (newValue != oldValue)
                            {
                                lines[i] = key + "=" + newValue;
                                hasChanges = true;
                            }
                        }
                    }
                }

                if (hasChanges)
                {
                    try
                    {
                        if (encoding == Encoding.UTF8)
                        {
                            File.WriteAllLines(propertiesPath, lines, new UTF8Encoding(false));
                        }
                        else if (encoding == Encoding.Default)
                        {
                            File.WriteAllLines(propertiesPath, lines, Encoding.Default);
                        }
                        else
                        {
                            File.WriteAllLines(propertiesPath, lines, encoding);
                        }

                        MagicShow.ShowMsgDialog(
                            FatherControl,
                            LanguageManager.Instance["SR_SaveSuccess"],
                            LanguageManager.Instance["Information"]);
                        RefreshServerConfig(); // 重新加载配置
                    }
                    catch (Exception ex)
                    {
                        MagicShow.ShowMsgDialog(
                            FatherControl,
                            string.Format(LanguageManager.Instance["SR_ServerConfigSaveFailed"], ex.Message),
                            LanguageManager.Instance["Error"]);
                    }
                }
                else
                {
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        LanguageManager.Instance["SR_ServerConfigNoChanges"],
                        LanguageManager.Instance["Information"]);
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(
                    FatherControl,
                    string.Format(LanguageManager.Instance["SR_ServerConfigSaveError"], ex.Message),
                    LanguageManager.Instance["Error"]);
            }
        }

        private async void changeServerIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FatherService.CheckServerRunning())
                {
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        LanguageManager.Instance["SR_ServerIconRunning"],
                        LanguageManager.Instance["Error"]);
                    return;
                }
                if (File.Exists(Rserverbase + "\\server-icon.png"))
                {
                    bool dialogret = await MagicShow.ShowMsgDialogAsync(
                        FatherControl,
                        LanguageManager.Instance["SR_ServerIconDeleteConfirm"],
                        LanguageManager.Instance["Warning"],
                        true,
                        LanguageManager.Instance["Cancel"]);
                    if (dialogret)
                    {
                        try
                        {
                            File.Delete(Rserverbase + "\\server-icon.png");
                        }
                        catch (Exception ex)
                        {
                            MagicShow.ShowMsgDialog(
                                FatherControl,
                                string.Format(LanguageManager.Instance["SR_ServerIconDeleteFailed"], ex.Message),
                                LanguageManager.Instance["Error"]);
                            return;
                        }
                        bool _dialogret = await MagicShow.ShowMsgDialogAsync(
                            FatherControl,
                            LanguageManager.Instance["SR_ServerIconDeletedContinue"],
                            LanguageManager.Instance["Tip"],
                            true,
                            LanguageManager.Instance["Cancel"]);
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

                await MagicShow.ShowMsgDialogAsync(
                    FatherControl,
                    LanguageManager.Instance["SR_ServerIconPrepare"],
                    LanguageManager.Instance["SR_ServerIconHowTo"]);
                OpenFileDialog openfile = new OpenFileDialog
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Title = LanguageManager.Instance["SR_SelectFile"],
                    Filter = LanguageManager.Instance["SR_PngImageFilter"]
                };
                var res = openfile.ShowDialog();
                if (res == true)
                {
                    try
                    {
                        File.Copy(openfile.FileName, Rserverbase + "\\server-icon.png", true);
                        MagicShow.ShowMsgDialog(
                            FatherControl,
                            LanguageManager.Instance["SR_ServerIconChanged"],
                            LanguageManager.Instance["Information"]);
                    }
                    catch (Exception ex)
                    {
                        MagicShow.ShowMsgDialog(
                            FatherControl,
                            string.Format(LanguageManager.Instance["SR_ServerIconChangeFailed"], ex.Message),
                            LanguageManager.Instance["Error"]);
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
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        LanguageManager.Instance["SR_WorldMapRunning"],
                        LanguageManager.Instance["Error"]);
                    return;
                }
                string levelName = GetConfigValue("level-name") ?? "world";

                if (Directory.Exists(Rserverbase + @"\" + levelName))
                {
                    if (await MagicShow.ShowMsgDialogAsync(
                        FatherControl,
                        LanguageManager.Instance["SR_WorldMapDeleteOverworldConfirm"],
                        LanguageManager.Instance["Warning"],
                        true,
                        LanguageManager.Instance["Cancel"]))
                    {
                        MagicDialog dialog = new MagicDialog();
                        dialog.ShowTextDialog(FatherControl, LanguageManager.Instance["SR_WorldMapDeleting"]);
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
                        if (await MagicShow.ShowMsgDialogAsync(
                            FatherControl,
                            LanguageManager.Instance["SR_WorldMapDeleteNetherConfirm"],
                            LanguageManager.Instance["Warning"],
                            true,
                            LanguageManager.Instance["Cancel"]))
                        {
                            MagicDialog dialog = new MagicDialog();
                            dialog.ShowTextDialog(FatherControl, LanguageManager.Instance["SR_WorldMapDeleting"]);
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
                        if (await MagicShow.ShowMsgDialogAsync(
                            FatherControl,
                            LanguageManager.Instance["SR_WorldMapDeleteEndConfirm"],
                            LanguageManager.Instance["Warning"],
                            true,
                            LanguageManager.Instance["Cancel"]))
                        {
                            MagicDialog dialog = new MagicDialog();
                            dialog.ShowTextDialog(FatherControl, LanguageManager.Instance["SR_WorldMapDeleting"]);
                            await Task.Run(() =>
                            {
                                DirectoryInfo di = new DirectoryInfo(Rserverbase + @"\" + levelName + "_the_end");
                                di.Delete(true);
                            });
                            dialog.CloseTextDialog();
                        }
                    }

                    if (await MagicShow.ShowMsgDialogAsync(
                        FatherControl,
                        LanguageManager.Instance["SR_WorldMapDeletedImportPrompt"],
                        LanguageManager.Instance["Tip"],
                        true,
                        LanguageManager.Instance["Cancel"]))
                    {
                        System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
                        {
                            Description = LanguageManager.Instance["SR_WorldMapFolderDescription"]
                        };
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            try
                            {
                                MagicDialog _dialog = new MagicDialog();
                                _dialog.ShowTextDialog(FatherControl, LanguageManager.Instance["SR_WorldMapImporting"]);
                                await Functions.MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + levelName, false);
                                _dialog.CloseTextDialog();
                                MagicShow.ShowMsgDialog(
                                    FatherControl,
                                    LanguageManager.Instance["SR_WorldMapImportSuccess"],
                                    LanguageManager.Instance["Information"]);
                            }
                            catch (Exception ex)
                            {
                                MagicShow.ShowMsgDialog(
                                    FatherControl,
                                    string.Format(LanguageManager.Instance["SR_WorldMapImportFailed"], ex.Message),
                                    LanguageManager.Instance["Error"]);
                            }
                        }
                    }
                }
                else
                {
                    System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = LanguageManager.Instance["SR_WorldMapFolderDescription"]
                    };
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        try
                        {
                            MagicDialog _dialog = new MagicDialog();
                            _dialog.ShowTextDialog(FatherControl, LanguageManager.Instance["SR_WorldMapImporting"]);
                            await Functions.MoveFolder(dialog.SelectedPath, Rserverbase + @"\" + levelName, false);
                            _dialog.CloseTextDialog();
                            MagicShow.ShowMsgDialog(
                                FatherControl,
                                LanguageManager.Instance["SR_WorldMapImportSuccess"],
                                LanguageManager.Instance["Information"]);
                        }
                        catch (Exception ex)
                        {
                            MagicShow.ShowMsgDialog(
                                FatherControl,
                                string.Format(LanguageManager.Instance["SR_WorldMapImportFailed"], ex.Message),
                                LanguageManager.Instance["Error"]);
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
            try
            {
                Dictionary<string, string> currentValues = GetCurrentConfigValues();
                if (currentValues.Count == 0)
                {
                    MagicShow.ShowMsgDialog(
                        FatherControl,
                        LanguageManager.Instance["SR_ConfigPresetNoFile"],
                        LanguageManager.Instance["Tip"]);
                    return;
                }

                _selectedConfigPreset = null;
                SetExistingConfigPresetActionsVisibility(Visibility.Collapsed);
                RenderConfigPresetButtons();
                configPresetButton.SetResourceReference(Button.StyleProperty, "ButtonPrimary");
                InitializeConfigPresetItems(currentValues);
                ConfigPresetNameTextBox.Clear();
                ConfigPresetNameTextBox.IsReadOnly = false;
                SaveConfigPresetButton.Visibility = Visibility.Visible;
                ConfigPresetPanel.Visibility = Visibility.Visible;
                SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetAddHint"]);
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(
                    FatherControl,
                    string.Format(LanguageManager.Instance["SR_ConfigPresetLoadFailed"], ex.Message),
                    LanguageManager.Instance["Error"]);
            }
        }

        private void CollapseConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            _selectedConfigPreset = null;
            ConfigPresetItems.Clear();
            ConfigPresetNameTextBox.Clear();
            SaveConfigPresetButton.Visibility = Visibility.Collapsed;
            SetExistingConfigPresetActionsVisibility(Visibility.Collapsed);
            ConfigPresetNameTextBox.IsReadOnly = false;
            SetConfigPresetStatus(string.Empty);
            configPresetButton.ClearValue(Button.StyleProperty);
            ConfigPresetPanel.Visibility = Visibility.Collapsed;
            RenderConfigPresetButtons();
        }

        private void ResetConfigPresetToolbar()
        {
            _selectedConfigPreset = null;
            ConfigPresets.Clear();
            ConfigPresetItems.Clear();
            ConfigPresetButtonsPanel.Children.Clear();
            ConfigPresetNameTextBox.Clear();
            SaveConfigPresetButton.Visibility = Visibility.Collapsed;
            SetExistingConfigPresetActionsVisibility(Visibility.Collapsed);
            ConfigPresetNameTextBox.IsReadOnly = false;
            SetConfigPresetStatus(string.Empty);
            configPresetButton.ClearValue(Button.StyleProperty);
            ConfigPresetPanel.Visibility = Visibility.Collapsed;
        }

        private void SetExistingConfigPresetActionsVisibility(Visibility visibility)
        {
            ApplyConfigPresetButton.Visibility = visibility;
            DeleteConfigPresetButton.Visibility = visibility;
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

        private void LoadConfigPresets()
        {
            string selectedName = _selectedConfigPreset?.Name;
            try
            {
                MergeLegacyPresetsIntoGlobal();
                List<ConfigPresetDefinition> presets = ReadConfigPresetDefinitions(ConfigPresetPath);
                ConfigPresets.Clear();
                _selectedConfigPreset = null;
                foreach (var preset in presets)
                {
                    ConfigPresets.Add(preset);
                }

                if (!string.IsNullOrEmpty(selectedName))
                {
                    _selectedConfigPreset = ConfigPresets.FirstOrDefault(
                        preset => string.Equals(preset.Name, selectedName, StringComparison.OrdinalIgnoreCase));
                }

            }
            catch (Exception ex)
            {
                SetConfigPresetStatus(string.Format(LanguageManager.Instance["SR_ConfigPresetLoadFailed"], ex.Message));
            }

            RenderConfigPresetButtons();
        }

        private void RenderConfigPresetButtons()
        {
            ConfigPresetButtonsPanel.Children.Clear();
            foreach (var preset in ConfigPresets)
            {
                Button button = new Button
                {
                    Content = preset.Name,
                    Tag = preset,
                    MinWidth = 50,
                    Padding = new Thickness(10, 0, 10, 0),
                    Margin = new Thickness(0, 0, 5, 3)
                };
                if (ReferenceEquals(preset, _selectedConfigPreset))
                    button.SetResourceReference(Button.StyleProperty, "ButtonPrimary");
                button.Click += ExistingConfigPresetButton_Click;
                ConfigPresetButtonsPanel.Children.Add(button);
            }

        }

        private void ExistingConfigPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.Tag is ConfigPresetDefinition preset))
                return;

            if (preset.Values == null || preset.Values.Count == 0)
                return;

            _selectedConfigPreset = preset;
            InitializeConfigPresetItems(preset.Values, preset.Values.Keys);
            LoadConfigPresetIntoEditor(preset);
            SaveConfigPresetButton.Visibility = Visibility.Collapsed;
            ConfigPresetNameTextBox.IsReadOnly = true;
            configPresetButton.ClearValue(Button.StyleProperty);
            ConfigPresetPanel.Visibility = Visibility.Visible;
            RenderConfigPresetButtons();
        }

        private void ConfigPresetList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is CheckBox)
                    return;
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
                if (preset.Values.ContainsKey(item.Key))
                {
                    item.Value = preset.Values[item.Key] ?? string.Empty;
                    item.IsSelected = true;
                }
                else
                {
                    item.Value = string.Empty;
                    item.IsSelected = false;
                }
            }

            ConfigPresetNameTextBox.Text = preset.Name;
            SetConfigPresetStatus(string.Format(
                LanguageManager.Instance["SR_ConfigPresetLoaded"],
                preset.Name,
                preset.Values.Count));
            SetExistingConfigPresetActionsVisibility(Visibility.Visible);
        }

        private List<ConfigPresetDefinition> UpsertConfigPreset(string name, IDictionary<string, string> values)
        {
            lock (ConfigPresetFileLock)
            {
                MergeLegacyPresetsIntoGlobalLocked();
                List<ConfigPresetDefinition> latest = ReadConfigPresetDefinitions(ConfigPresetPath);
                ConfigPresetDefinition preset = latest.FirstOrDefault(item =>
                    string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                if (preset == null)
                {
                    preset = new ConfigPresetDefinition { Name = name };
                    latest.Add(preset);
                }

                if (preset.Values == null)
                    preset.Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var value in values)
                    preset.Values[value.Key] = value.Value ?? string.Empty;

                WriteConfigPresetDefinitionsLocked(latest);
                return NormalizeConfigPresets(latest);
            }
        }

        private List<ConfigPresetDefinition> DeleteConfigPresetFromStorage(string name)
        {
            lock (ConfigPresetFileLock)
            {
                MergeLegacyPresetsIntoGlobalLocked();
                List<ConfigPresetDefinition> latest = ReadConfigPresetDefinitions(ConfigPresetPath);
                latest.RemoveAll(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                WriteConfigPresetDefinitionsLocked(latest);
                return NormalizeConfigPresets(latest);
            }
        }

        private void MergeLegacyPresetsIntoGlobal()
        {
            lock (ConfigPresetFileLock)
                MergeLegacyPresetsIntoGlobalLocked();
        }

        private void MergeLegacyPresetsIntoGlobalLocked()
        {
            if (!File.Exists(LegacyConfigPresetPath))
                return;

            string globalPath = Path.GetFullPath(ConfigPresetPath);
            string legacyPath = Path.GetFullPath(LegacyConfigPresetPath);
            if (string.Equals(globalPath, legacyPath, StringComparison.OrdinalIgnoreCase))
                return;

            List<ConfigPresetDefinition> legacy = ReadConfigPresetDefinitions(LegacyConfigPresetPath);
            if (legacy.Count == 0)
                return;

            List<ConfigPresetDefinition> latest = ReadConfigPresetDefinitions(ConfigPresetPath);
            bool changed = false;
            foreach (var legacyPreset in legacy)
            {
                ConfigPresetDefinition existing = latest.FirstOrDefault(item =>
                    string.Equals(item.Name, legacyPreset.Name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    latest.Add(NormalizeConfigPresets(new[] { legacyPreset }).First());
                    changed = true;
                    continue;
                }

                if (existing.Values == null)
                    existing.Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var value in legacyPreset.Values ?? new Dictionary<string, string>())
                {
                    if (!existing.Values.ContainsKey(value.Key))
                    {
                        existing.Values[value.Key] = value.Value ?? string.Empty;
                        changed = true;
                    }
                }
            }

            if (changed)
                WriteConfigPresetDefinitionsLocked(latest);
        }

        private static List<ConfigPresetDefinition> ReadConfigPresetDefinitions(string path)
        {
            if (!File.Exists(path))
                return new List<ConfigPresetDefinition>();

            JToken json = JToken.Parse(File.ReadAllText(path, Encoding.UTF8));
            JToken presetToken = json as JArray;
            if (presetToken == null && json is JObject root)
                presetToken = root["Presets"];
            List<ConfigPresetDefinition> presets = presetToken?.ToObject<List<ConfigPresetDefinition>>();
            return NormalizeConfigPresets(presets);
        }

        private static List<ConfigPresetDefinition> NormalizeConfigPresets(IEnumerable<ConfigPresetDefinition> presets)
        {
            return presets == null
                ? new List<ConfigPresetDefinition>()
                : presets
                    .Where(preset => preset != null && !string.IsNullOrWhiteSpace(preset.Name))
                    .Select(preset => new ConfigPresetDefinition
                    {
                        Name = preset.Name.Trim(),
                        Values = new Dictionary<string, string>(
                            preset.Values ?? new Dictionary<string, string>(),
                            StringComparer.OrdinalIgnoreCase)
                    })
                    .ToList();
        }

        private static void WriteConfigPresetDefinitionsLocked(IEnumerable<ConfigPresetDefinition> presets)
        {
            string directory = Path.GetDirectoryName(ConfigPresetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string tempPath = ConfigPresetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                string json = JsonConvert.SerializeObject(presets, Formatting.Indented);
                File.WriteAllText(tempPath, json, new UTF8Encoding(false));
                if (File.Exists(ConfigPresetPath))
                    File.Replace(tempPath, ConfigPresetPath, null);
                else
                    File.Move(tempPath, ConfigPresetPath);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        private Dictionary<string, string> GetSelectedConfigPresetValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in ConfigPresetItems.Where(item => item.IsSelected))
            {
                if (configTextBoxes.TryGetValue(item.Key, out TextBox textBox))
                    values[item.Key] = textBox.Text ?? string.Empty;
            }
            return values;
        }

        private async void SaveConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            string name = ConfigPresetNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetNameRequired"]);
                ConfigPresetNameTextBox.Focus();
                return;
            }

            Dictionary<string, string> values = GetSelectedConfigPresetValues();
            if (values.Count == 0)
            {
                SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetSelectionRequired"]);
                return;
            }

            try
            {
                ConfigPresetDefinition preset = ConfigPresets.FirstOrDefault(
                    item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                if (preset == null)
                {
                    MergeLegacyPresetsIntoGlobal();
                    preset = ReadConfigPresetDefinitions(ConfigPresetPath).FirstOrDefault(
                        item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                }
                if (preset != null)
                {
                    bool overwrite = await MagicShow.ShowMsgDialogAsync(
                        FatherControl,
                        string.Format(LanguageManager.Instance["SR_ConfigPresetOverwriteConfirm"], name),
                        LanguageManager.Instance["SR_ConfigPresetOverwriteTitle"],
                        true,
                        LanguageManager.Instance["Cancel"],
                        LanguageManager.Instance["SR_ConfigPresetOverwrite"],
                        null,
                        true);
                    if (!overwrite)
                    {
                        SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetOverwriteCanceled"]);
                        return;
                    }
                }
                List<ConfigPresetDefinition> latest = UpsertConfigPreset(name, values);
                ConfigPresets.Clear();
                foreach (var latestPreset in NormalizeConfigPresets(latest))
                    ConfigPresets.Add(latestPreset);
                _selectedConfigPreset = ConfigPresets.FirstOrDefault(
                    item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
                SaveConfigPresetButton.Visibility = Visibility.Collapsed;
                ConfigPresetNameTextBox.IsReadOnly = true;
                SetExistingConfigPresetActionsVisibility(Visibility.Visible);
                RenderConfigPresetButtons();
                SetConfigPresetStatus(string.Format(
                    LanguageManager.Instance["SR_ConfigPresetSaved"],
                    name,
                    _selectedConfigPreset?.Values.Count ?? values.Count));
            }
            catch (Exception ex)
            {
                SetConfigPresetStatus(string.Format(LanguageManager.Instance["SR_ConfigPresetSaveFailed"], ex.Message));
            }
        }

        private void LoadConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            ConfigPresetPanel.Visibility = Visibility.Collapsed;
            ConfigPresetItems.Clear();
            ConfigPresetNameTextBox.Clear();
            _selectedConfigPreset = null;
            configPresetButton.ClearValue(Button.StyleProperty);
            LoadConfigPresets();
        }

        private void ApplyConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedConfigPreset == null)
            {
                SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetLoadRequired"]);
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
                SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetSelectionRequired"]);
                return;
            }

            string error = ApplyPresetValues(values);
            if (string.IsNullOrWhiteSpace(error))
            {
                foreach (var item in ConfigPresetItems)
                    item.IsSelected = true;
            }
            SetConfigPresetStatus(string.IsNullOrWhiteSpace(error)
                ? string.Format(LanguageManager.Instance["SR_ConfigPresetApplied"], values.Count)
                : error);
        }

        private void DeleteConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedConfigPreset == null)
            {
                SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetSelectRequired"]);
                return;
            }

            try
            {
                ConfigPresetDefinition preset = _selectedConfigPreset;
                List<ConfigPresetDefinition> latest = DeleteConfigPresetFromStorage(preset.Name);
                ConfigPresets.Clear();
                foreach (var latestPreset in NormalizeConfigPresets(latest))
                    ConfigPresets.Add(latestPreset);
                _selectedConfigPreset = null;
                ConfigPresetNameTextBox.Clear();
                ConfigPresetNameTextBox.IsReadOnly = false;
                SetExistingConfigPresetActionsVisibility(Visibility.Collapsed);
                RenderConfigPresetButtons();
                ConfigPresetItems.Clear();
                SetConfigPresetStatus(string.Format(LanguageManager.Instance["SR_ConfigPresetDeleted"], preset.Name));
                ConfigPresetPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                SetConfigPresetStatus(string.Format(LanguageManager.Instance["SR_ConfigPresetDeleteFailed"], ex.Message));
            }
        }

        private void SelectAllConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ConfigPresetItems)
                item.IsSelected = true;
            SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetAllSelected"]);
        }

        private void SelectNoneConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ConfigPresetItems)
                item.IsSelected = false;
            SetConfigPresetStatus(LanguageManager.Instance["SR_ConfigPresetAllUnselected"]);
        }

        private void SetConfigPresetStatus(string message)
        {
            ConfigPresetStatusText.Text = message;
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
        private string ApplyPresetValues(IReadOnlyDictionary<string, string> values)
        {
            try
            {
                if (FatherService.CheckServerRunning())
                    return LanguageManager.Instance["SR_ConfigPresetRunning"];

                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                if (!File.Exists(propertiesPath))
                    return LanguageManager.Instance["SR_ConfigPresetFileMissing"];

                Encoding encoding = Functions.GetTextFileEncodingType(propertiesPath);
                List<string> lines = File.ReadAllLines(propertiesPath, encoding).ToList();
                HashSet<string> foundKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool hasChanges = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    string trimmedLine = lines[i].Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex <= 0)
                        continue;

                    string key = trimmedLine.Substring(0, separatorIndex).Trim();
                    if (!values.TryGetValue(key, out string newValue))
                        continue;

                    foundKeys.Add(key);
                    newValue = newValue ?? string.Empty;
                    string oldValue = trimmedLine.Substring(separatorIndex + 1).Trim();
                    newValue = ConvertLegacyConfigValue(key, newValue, oldValue);
                    if (newValue == oldValue)
                        continue;

                    lines[i] = key + "=" + newValue;
                    hasChanges = true;
                }

                foreach (var value in values)
                {
                    if (foundKeys.Contains(value.Key))
                        continue;

                    lines.Add(value.Key + "=" + (value.Value ?? string.Empty));
                    hasChanges = true;
                }

                if (!hasChanges)
                    return LanguageManager.Instance["SR_ConfigPresetNoChanges"];

                if (encoding == Encoding.UTF8)
                    File.WriteAllLines(propertiesPath, lines, new UTF8Encoding(false));
                else
                    File.WriteAllLines(propertiesPath, lines, encoding);

                RefreshServerConfig();
                return null;
            }
            catch (Exception ex)
            {
                return string.Format(LanguageManager.Instance["SR_ConfigPresetApplyFailed"], ex.Message);
            }
        }

        private string ConvertLegacyConfigValue(string key, string presetValue, string targetValue)
        {
            if (key.Equals("gamemode", StringComparison.OrdinalIgnoreCase))
                return ConvertIndexedConfigValue(presetValue, targetValue,
                    new[] { "survival", "creative", "adventure", "spectator" });
            if (key.Equals("difficulty", StringComparison.OrdinalIgnoreCase))
                return ConvertIndexedConfigValue(presetValue, targetValue,
                    new[] { "peaceful", "easy", "normal", "hard" });
            return presetValue ?? string.Empty;
        }

        private static string ConvertIndexedConfigValue(string presetValue, string targetValue, string[] names)
        {
            if (string.IsNullOrWhiteSpace(presetValue) || string.IsNullOrWhiteSpace(targetValue))
                return presetValue ?? string.Empty;

            string value = presetValue.Trim();
            bool targetUsesNumber = int.TryParse(targetValue.Trim(), out _);
            if (int.TryParse(value, out int number) && number >= 0 && number < names.Length)
                return targetUsesNumber ? number.ToString() : names[number];

            int index = Array.FindIndex(names,
                name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && targetUsesNumber ? index.ToString() :
                index >= 0 ? names[index] : presetValue;
        }

        #endregion
    }

    public sealed class ConfigPresetDefinition
    {
        public string Name { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
