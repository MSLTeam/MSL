using HandyControl.Controls;
using Microsoft.Win32;
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
            Growl.Success("刷新成功！");
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

                changeServerPropertiesLab.Text = "服务器配置信息";
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
                    MagicShow.ShowMsgDialog(FatherControl, "服务器运行时无法调整服务器功能！", "错误");
                    return;
                }
                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                if (!File.Exists(propertiesPath))
                {
                    MagicShow.ShowMsgDialog(FatherControl, "配置文件不存在！", "错误");
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

                        MagicShow.ShowMsgDialog(FatherControl, "保存成功！", "信息");
                        RefreshServerConfig(); // 重新加载配置
                    }
                    catch (Exception ex)
                    {
                        MagicShow.ShowMsgDialog(FatherControl, "保存失败！请检查服务器是否关闭！\n错误代码：" + ex.Message, "错误");
                    }
                }
                else
                {
                    MagicShow.ShowMsgDialog(FatherControl, "没有需要保存的更改！", "信息");
                }
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(FatherControl, "保存过程中发生错误！\n错误代码：" + ex.Message, "错误");
            }
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
            try
            {
                Dictionary<string, string> currentValues = GetCurrentConfigValues();
                if (currentValues.Count == 0)
                {
                    MagicShow.ShowMsgDialog(FatherControl, "未找到 server.properties，无法创建配置预设！", "提示");
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
                SetConfigPresetStatus("正在添加新预设，默认选择全部配置值。");
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(FatherControl, "加载配置预设失败：\n" + ex.Message, "错误");
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
                SetConfigPresetStatus("预设文件读取失败：" + ex.Message);
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
            SetConfigPresetStatus($"已加载预设“{preset.Name}”，选中 {preset.Values.Count} 项。");
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
                SetConfigPresetStatus("请输入预设名称。");
                ConfigPresetNameTextBox.Focus();
                return;
            }

            Dictionary<string, string> values = GetSelectedConfigPresetValues();
            if (values.Count == 0)
            {
                SetConfigPresetStatus("请至少选择一个配置值。");
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
                        $"预设“{name}”已经存在，是否覆盖原预设？",
                        "覆盖预设",
                        true,
                        "取消",
                        "覆盖",
                        null,
                        true);
                    if (!overwrite)
                    {
                        SetConfigPresetStatus("已取消覆盖预设。");
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
                SetConfigPresetStatus($"预设“{name}”已保存，共 {_selectedConfigPreset?.Values.Count ?? values.Count} 项。");
            }
            catch (Exception ex)
            {
                SetConfigPresetStatus("预设保存失败：" + ex.Message);
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
                SetConfigPresetStatus("请先选择并加载一个已有预设。");
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

            string error = ApplyPresetValues(values);
            if (string.IsNullOrWhiteSpace(error))
            {
                foreach (var item in ConfigPresetItems)
                    item.IsSelected = true;
            }
            SetConfigPresetStatus(string.IsNullOrWhiteSpace(error)
                ? $"已应用 {values.Count} 项配置。"
                : error);
        }

        private void DeleteConfigPreset_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedConfigPreset == null)
            {
                SetConfigPresetStatus("请先选择一个预设。");
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
                SetConfigPresetStatus($"预设“{preset.Name}”已删除。");
                ConfigPresetPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                SetConfigPresetStatus("预设删除失败：" + ex.Message);
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
                    return "服务器运行时无法应用配置预设！";

                string propertiesPath = Path.Combine(Rserverbase, "server.properties");
                if (!File.Exists(propertiesPath))
                    return "配置文件不存在！";

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
                    return "没有需要应用的更改。";

                if (encoding == Encoding.UTF8)
                    File.WriteAllLines(propertiesPath, lines, new UTF8Encoding(false));
                else
                    File.WriteAllLines(propertiesPath, lines, encoding);

                RefreshServerConfig();
                return null;
            }
            catch (Exception ex)
            {
                return "配置应用失败：" + ex.Message;
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
