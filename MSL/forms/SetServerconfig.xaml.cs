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
                    if (file.EndsWith(".json") || file.EndsWith(".yml") || file.EndsWith(".properties"))
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

        private void FileTreeView_Selected(object sender, RoutedEventArgs e)
        {
            if (e.Source is TreeViewItem selectedNode)
            {
                path = GetSelectTreePath(selectedNode);
                //MessageBox.Show("选中的路径：" + path);
                if (path.EndsWith(".json") || path.EndsWith(".yml") || path.EndsWith(".properties"))
                {
                    try
                    {
                        encoding = Functions.GetTextFileEncodingType(serverbase + "\\" + path);
                        FileEncoding.Content = encoding.EncodingName;
                        EditorBox.Text = File.ReadAllText(serverbase + "\\" + path, encoding);
                    }
                    catch (FileNotFoundException)
                    {
                        FileTreeView.Items.Clear();
                        GetConfigFiles();
                    }
                    catch (Exception ex)
                    {
                        Shows.ShowMsgDialog(this, ex.Message, "Err");
                    }
                }
            }
        }

        private string GetSelectTreePath(TreeViewItem item)
        {
            // 初始化一个 StringBuilder 用于拼接路径
            StringBuilder pathBuilder = new StringBuilder();

            // 从当前选中的 TreeViewItem 开始，逐级向上遍历父节点
            while (item.Header != null)
            {
                // 获取当前节点的标题（Header）
                string header = item.Header.ToString();

                // 将标题添加到路径中
                pathBuilder.Insert(0, header);

                // 添加路径分隔符（例如斜杠或反斜杠）
                pathBuilder.Insert(0, "\\");

                // 获取当前节点的父节点
                item = item.Parent as TreeViewItem;
            }
            if (pathBuilder.Length > 0)
            {
                // 移除路径开头的分隔符
                pathBuilder.Remove(0, 1);

                // 返回拼接好的路径
                return pathBuilder.ToString();
            }
            return string.Empty;
        }

        private void ChangeEncoding_Click(object sender, RoutedEventArgs e)
        {
            string content = EditorBox.Text;
            if (encoding == Encoding.UTF8)
            {
                byte[] ansiBytes = Encoding.Convert(Encoding.UTF8, Encoding.Default, Encoding.UTF8.GetBytes(content));
                string ansiContent = Encoding.Default.GetString(ansiBytes);
                encoding = Encoding.Default;
                FileEncoding.Content = encoding.EncodingName;
                EditorBox.Text = ansiContent;
            }
            else if (encoding == Encoding.Default)
            {
                byte[] utf8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(content));
                string utf8Content = Encoding.UTF8.GetString(utf8Bytes);
                encoding = Encoding.UTF8;
                FileEncoding.Content = encoding.EncodingName;
                EditorBox.Text = utf8Content;
            }
        }

        private void SaveChange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (encoding == Encoding.UTF8)
                {
                    File.WriteAllText(serverbase + "\\" + path, EditorBox.Text, new UTF8Encoding(false));
                }
                else if (encoding == Encoding.Default)
                {
                    File.WriteAllText(serverbase + "\\" + path, EditorBox.Text, Encoding.Default);
                }
                Shows.ShowMsgDialog(this, "保存成功！重启服务器生效！", "提示");
            }
            catch (Exception ex)
            {
                Shows.ShowMsgDialog(this, ex.Message, "Err");
            }
        }
    }
}
