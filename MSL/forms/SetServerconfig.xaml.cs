using System;
using System.IO;
using System.Text;
using System.Windows;

namespace MSL
{
    /// <summary>
    /// SetServerconfig.xaml 的交互逻辑
    /// </summary>
    public partial class SetServerconfig : HandyControl.Controls.Window
    {
        private readonly string serverbase;
        public SetServerconfig(string _serverbase)
        {
            InitializeComponent();
            serverbase = _serverbase;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetConfigFiles();
        }

        private void GetConfigFiles()
        {
            try
            {
                string[] files = Directory.GetFiles(serverbase);
                foreach (string file in files)
                {
                    if (file.EndsWith(".json") || file.EndsWith(".yml") || file.EndsWith(".properties"))
                    {
                        FileTreeView.Items.Add(Path.GetFileName(file));
                    }
                }
            }
            catch
            {
                return;
            }
        }

        private Encoding encoding;
        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            encoding = GetTextFileEncodingType(serverbase + "\\" + FileTreeView.SelectedItem.ToString());
            //MessageBox.Show(encoding.EncodingName);
            FileEncoding.Content = encoding.EncodingName;
            EditorBox.Text = File.ReadAllText(serverbase + "\\" + FileTreeView.SelectedItem.ToString(),encoding);
        }

        private void SaveChange_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(serverbase + "\\" + FileTreeView.SelectedItem.ToString(), EditorBox.Text, encoding);
        }

        /// <summary>
        /// 获取文本文件的字符编码类型
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static Encoding GetTextFileEncodingType(string fileName)
        {
            Encoding encoding = Encoding.Default;
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream, encoding);
            byte[] buffer = binaryReader.ReadBytes((int)fileStream.Length);
            binaryReader.Close();
            fileStream.Close();
            if (buffer.Length >= 3 && buffer[0] == 239 && buffer[1] == 187 && buffer[2] == 191)
            {
                encoding = Encoding.UTF8;
            }
            else if (buffer.Length >= 3 && buffer[0] == 254 && buffer[1] == 255 && buffer[2] == 0)
            {
                encoding = Encoding.BigEndianUnicode;
            }
            else if (buffer.Length >= 3 && buffer[0] == 255 && buffer[1] == 254 && buffer[2] == 65)
            {
                encoding = Encoding.Unicode;
            }
            else if (IsUTF8Bytes(buffer))
            {
                encoding = Encoding.UTF8;
            }
            return encoding;
        }

        /// <summary>
        /// 判断是否是不带 BOM 的 UTF8 格式
        /// BOM（Byte Order Mark），字节顺序标记，出现在文本文件头部，Unicode编码标准中用于标识文件是采用哪种格式的编码。
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数 
            byte curByte; //当前分析的字节. 
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前 
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1 
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }
    }
}
