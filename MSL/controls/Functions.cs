using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MSL.controls
{
    internal class Functions
    {
        public static string Get(string path)
        {
            string url;
            if (MainWindow.serverLink != "https://msl.waheal.top")
            {
                url = MainWindow.serverLink + ":5000";
            }
            else
            {
                url = "https://api.waheal.top";
            }
            WebClient webClient = new WebClient();
            webClient.Credentials = CredentialCache.DefaultCredentials;
            byte[] pageData = webClient.DownloadData(url + "/" + path);
            return Encoding.UTF8.GetString(pageData);
        }

        public static string Post(string path,int contentType=0, string parameterData="",string customUrl="")
        {
            string url;
            if (customUrl == "")
            {
                if (MainWindow.serverLink != "https://msl.waheal.top")
                {
                    url = MainWindow.serverLink + ":5000";
                }
                else
                {
                    url = "https://api.waheal.top";
                }
            }
            else
            {
                url = customUrl;
            }
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url+"/"+path);
            byte[] buf = Encoding.GetEncoding("UTF-8").GetBytes(parameterData);

            if (contentType == 0)
            {
                myRequest.Method = "POST";
                myRequest.Accept = "application/json";
                myRequest.ContentType = "application/json; charset=UTF-8";
                myRequest.ContentLength = buf.Length;
                myRequest.MaximumAutomaticRedirections = 1;
                myRequest.AllowAutoRedirect = true;
            }
            else if (contentType == 1)
            {
                myRequest.Method = "POST";
                myRequest.Accept = "text/plain";
                myRequest.ContentType = "text/plain; charset=UTF-8";
                myRequest.ContentLength = buf.Length;
                myRequest.MaximumAutomaticRedirections = 1;
                myRequest.AllowAutoRedirect = true;
            }

            // 发送请求
            using (Stream stream = myRequest.GetRequestStream())
            {
                stream.Write(buf, 0, buf.Length);
            }

            // 获取响应
            using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
            using (StreamReader reader = new StreamReader(myResponse.GetResponseStream(), Encoding.UTF8))
            {
                //string returnData = Regex.Unescape(reader.ReadToEnd());
                string returnData = reader.ReadToEnd();
                return returnData;
            }
        }

        public static Tuple<int, int, int, int> VersionCompare(string version)
        {
            if (version.StartsWith("*"))
            {
                return Tuple.Create(100, 100, 100, 100);
            }

            // 使用正则表达式从版本号中提取主要版本号
            Regex regex = new Regex(@"(\d+(\.\d+)+)");
            Match match = regex.Match(version);
            if (match.Success)
            {
                version = match.Groups[1].Value;
            }

            // 将版本号中的每个部分转换为整数，并进行比较
            string[] versionParts = version.Split('.');
            List<int> versionIntParts = new List<int>();
            foreach (string part in versionParts)
            {
                if (int.TryParse(part, out int parsedPart))
                {
                    versionIntParts.Add(parsedPart);
                }
            }

            // 添加0，以便对不完整的版本号进行比较（如1.7）
            while (versionIntParts.Count < 4)
            {
                versionIntParts.Add(0);
            }

            return Tuple.Create(versionIntParts[0], versionIntParts[1], versionIntParts[2], versionIntParts[3]);
        }

        public static void MoveFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("创建目标目录失败：" + ex.Message);
                    }
                }
                //获得源文件下所有文件
                List<string> files = new List<string>(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //覆盖模式
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(c, destFile);
                });
                //获得源文件下所有目录文件
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //Directory.Move必须要在同一个根目录下移动才有效，不能在不同卷中移动。
                    //Directory.Move(c, destDir);

                    //采用递归的方法实现
                    MoveFolder(c, destDir);
                });
                Directory.Delete(sourcePath);
            }
            else
            {
                throw new DirectoryNotFoundException("源目录不存在！");
            }
        }
    }
}
