using MSL.utils;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MSL.controls.dialogs
{
    /// <summary>
    /// SelectConfigPresetDialog.xaml 的交互逻辑
    /// 独立窗口：用于选择要保存到预设的配置项（默认全选，可手动勾选）
    /// 预设名称由调用方在第一步输入，本窗口只负责选择配置项
    /// </summary>
    public partial class SelectConfigPresetDialog : HandyControl.Controls.Window
    {
        /// <summary>确认后返回的选中配置键值对，取消时为 null</summary>
        public Dictionary<string, string> SelectedValues = null;

        private readonly Dictionary<string, string> allValues;
        private readonly List<CheckBox> checkBoxes = new List<CheckBox>();

        /// <summary>
        /// 构造选择窗口
        /// </summary>
        /// <param name="allValues">全部配置键值对</param>
        /// <param name="descriptions">配置项说明（key -> 说明文字，可选，显示在每项下方）</param>
        public SelectConfigPresetDialog(Dictionary<string, string> allValues, Dictionary<string, string> descriptions = null)
        {
            InitializeComponent();
            this.allValues = allValues;

            // 默认全选
            foreach (var kvp in allValues)
            {
                Grid itemGrid = new Grid();
                itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                itemGrid.Margin = new Thickness(0, 0, 0, 2);

                CheckBox checkBox = new CheckBox
                {
                    Content = kvp.Key + " = " + kvp.Value,
                    Tag = kvp.Key,
                    IsChecked = true,
                    Margin = new Thickness(4, 3, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                checkBox.SetResourceReference(Control.ForegroundProperty, "PrimaryTextBrush");
                checkBox.Checked += UpdateSelectedCount;
                checkBox.Unchecked += UpdateSelectedCount;
                Grid.SetRow(checkBox, 0);
                itemGrid.Children.Add(checkBox);
                checkBoxes.Add(checkBox);

                // 配置项说明（注：...）
                if (descriptions != null && descriptions.TryGetValue(kvp.Key, out string description) && !string.IsNullOrEmpty(description))
                {
                    TextBlock descriptionBlock = new TextBlock
                    {
                        Text = description,
                        Margin = new Thickness(28, 0, 4, 5),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    descriptionBlock.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryTextBrush");
                    Grid.SetRow(descriptionBlock, 1);
                    itemGrid.Children.Add(descriptionBlock);
                }

                itemsPanel.Children.Add(itemGrid);
            }
            UpdateSelectedCount(null, null);
        }

        /// <summary>更新已选择数量提示</summary>
        private void UpdateSelectedCount(object sender, RoutedEventArgs e)
        {
            int selected = 0;
            foreach (CheckBox checkBox in checkBoxes)
            {
                if (checkBox.IsChecked == true)
                    selected++;
            }
            selectedCountLab.Text = $"已选择 {selected} / {allValues.Count} 项";
        }

        private void selectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox checkBox in checkBoxes)
            {
                checkBox.IsChecked = true;
            }
            UpdateSelectedCount(null, null);
        }

        private void deselectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox checkBox in checkBoxes)
            {
                checkBox.IsChecked = false;
            }
            UpdateSelectedCount(null, null);
        }

        private void PrimaryBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = new Dictionary<string, string>();
            foreach (CheckBox checkBox in checkBoxes)
            {
                if (checkBox.IsChecked == true && checkBox.Tag is string key && allValues.TryGetValue(key, out string value))
                {
                    selected[key] = value;
                }
            }
            if (selected.Count == 0)
            {
                MagicFlowMsg.ShowMessage("请至少选择一个配置项！", 2, panel: MainGrid);
                return;
            }

            SelectedValues = selected;
            Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectedValues = null;
            Close();
        }
    }
}
