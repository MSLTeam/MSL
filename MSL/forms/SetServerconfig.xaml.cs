using ICSharpCode.AvalonEdit.Highlighting;
using MSL.utils;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MSL
{
    /// <summary>
    /// SetServerconfig.xaml 的交互逻辑
    /// </summary>
    public partial class SetServerconfig : HandyControl.Controls.Window
    {
        private readonly string serverbase;
        private Encoding encoding;
        private string path;

        public SetServerconfig(string _serverbase)
        {
            InitializeComponent();
            serverbase = _serverbase;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetConfigFiles();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            FileTreeView.Items.Clear();
            GetConfigFiles();
        }

        private void GetConfigFiles()
        {
            GetConfigFiles(serverbase, FileTreeView);
        }

        private void GetConfigFiles(string folderPath, TreeViewItem parentNode)
        {
            try
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    if (file.EndsWith(".json") || file.EndsWith(".yml") || file.EndsWith(".toml") || file.EndsWith(".properties"))
                    {
                        TreeViewItem fileNode = new TreeViewItem();
                        fileNode.Header = Path.GetFileName(file);
                        parentNode.Items.Add(fileNode);
                    }
                }

                string[] subdirectories = Directory.GetDirectories(folderPath);
                foreach (string subdirectory in subdirectories)
                {
                    TreeViewItem subdirectoryNode = new TreeViewItem
                    {
                        Header = Path.GetFileName(subdirectory)
                    };
                    parentNode.Items.Add(subdirectoryNode);
                    GetConfigFiles(subdirectory, subdirectoryNode); // 递归调用，处理子文件夹
                }
            }
            catch
            {
                return;
            }
        }

        private string GetSelectTreePath(TreeViewItem item)
        {
            StringBuilder pathBuilder = new StringBuilder();

            while (item != null && item.Header != null)
            {
                // 跳过根节点（FileTreeView）
                if (item == FileTreeView)
                {
                    break;
                }

                string header = item.Header.ToString();
                pathBuilder.Insert(0, header);
                pathBuilder.Insert(0, "\\");

                item = item.Parent as TreeViewItem;
            }

            if (pathBuilder.Length > 0)
            {
                pathBuilder.Remove(0, 1);
                return pathBuilder.ToString();
            }
            return string.Empty;
        }

        private void FileTreeView_Selected(object sender, RoutedEventArgs e)
        {
            if (e.Source is TreeViewItem selectedNode)
            {
                ChangeEncoding.IsEnabled = false;
                SaveChange.IsEnabled = false;
                path = GetSelectTreePath(selectedNode);
                if (path.EndsWith(".json") || path.EndsWith(".yml") ||
                    path.EndsWith(".toml") || path.EndsWith(".properties"))
                {
                    try
                    {
                        encoding = Functions.GetTextFileEncodingType(serverbase + "\\" + path);
                        FileEncoding.Content = encoding.EncodingName;

                        string content = File.ReadAllText(serverbase + "\\" + path, encoding);
                        EditorBox.Text = content;
                        ChangeEncoding.IsEnabled = true;
                        SaveChange.IsEnabled = true;
                        // 根据文件类型设置语法高亮
                        SetSyntaxHighlighting(path);
                    }
                    catch (FileNotFoundException)
                    {
                        FileTreeView.Items.Clear();
                        GetConfigFiles();
                    }
                    catch (Exception ex)
                    {
                        MagicShow.ShowMsgDialog(this, ex.Message, "Err");
                    }
                }
            }
        }

        private void SetSyntaxHighlighting(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            string highlightName = ext switch
            {
                ".json" => "JavaScript",
                ".yml" => "YAML",
                ".toml" => null,
                ".properties" => null,
                _ => null
            };

            EditorBox.SyntaxHighlighting = highlightName != null
                ? HighlightingManager.Instance.GetDefinition(highlightName)
                : null;
        }

        private void ChangeEncoding_Click(object sender, RoutedEventArgs e)
        {
            if (encoding == Encoding.UTF8)
            {
                encoding = Encoding.Default;
            }
            else if (encoding == Encoding.Default)
            {
                encoding = Encoding.UTF8;
            }
            FileEncoding.Content = encoding.EncodingName;
        }

        private void SaveChange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var writeEncoding = encoding == Encoding.UTF8
                    ? new UTF8Encoding(false)
                    : (Encoding)Encoding.Default;

                File.WriteAllText(serverbase + "\\" + path, EditorBox.Text, writeEncoding);
                MagicShow.ShowMsgDialog(this, "保存成功！重启服务器生效！", "提示");
            }
            catch (Exception ex)
            {
                MagicShow.ShowMsgDialog(this, ex.Message, "Err");
            }
        }

        // 复制
        private void AEMenu_Copy(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(EditorBox.SelectedText))
                Clipboard.SetText(EditorBox.SelectedText);
            else if (!string.IsNullOrEmpty(EditorBox.Document.Text))
                Clipboard.SetText(EditorBox.Document.Text);
        }

        // 全选
        private void AEMenu_SelectAll(object sender, RoutedEventArgs e)
        {
            EditorBox.SelectAll();
        }

        // 粘贴
        private void AEMenu_Paste(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
                EditorBox.Document.Insert(EditorBox.CaretOffset, Clipboard.GetText());
        }

        // 剪切
        private void AEMenu_Cut(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(EditorBox.SelectedText))
            {
                Clipboard.SetText(EditorBox.SelectedText);
                EditorBox.Document.Remove(EditorBox.SelectionStart, EditorBox.SelectionLength);
            }
        }
    }
}
