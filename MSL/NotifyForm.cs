using MSL.controls;
using MSL.pages;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace MSL
{
    public partial class NotifyForm : Form
    {
        public static bool isNotifyOpen;
        public NotifyForm()
        {
            MainWindow.CloseNotify += CtrlNotify;
            InitializeComponent();
        }

        private void Form_Load(object sender, EventArgs e)
        {
            isNotifyOpen = true;
            this.Hide();
        }
        public event Action NotifyFormShowEvent;
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            NotifyFormShowEvent();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (ServerList.RunningServerIDs != "" || FrpcPage.FRPCMD.HasExited == false || OnlinePage.FRPCMD.HasExited == false)
                {
                    if (MessageBox.Show("您的服务器、内网映射或联机功能正在运行中，关闭软件可能会让服务器进程在后台一直运行并占用资源！确定要继续关闭吗？", "警告", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Close();
                        Process.GetCurrentProcess().Kill();
                    }
                }
                else
                {
                    Close();
                    Process.GetCurrentProcess().Kill();
                }
            }
            catch
            {
                try
                {
                    if (FrpcPage.FRPCMD.HasExited == false || OnlinePage.FRPCMD.HasExited == false)
                    {
                        if (MessageBox.Show("内网映射或联机功能正在运行中，关闭软件可能会让内网映射进程在后台一直运行并占用资源！确定要继续关闭吗？", "警告", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            Close();
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    else
                    {
                        Close();
                        Process.GetCurrentProcess().Kill();
                    }
                }
                catch
                {
                    try
                    {
                        if (OnlinePage.FRPCMD.HasExited == false)
                        {
                            if (MessageBox.Show("联机功能正在运行中，关闭软件可能会让内网映射进程在后台一直运行并占用资源！确定要继续关闭吗？", "警告", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning) == DialogResult.Yes)
                            {
                                Close();
                                Process.GetCurrentProcess().Kill();
                            }
                        }
                        else
                        {
                            Close();
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    catch
                    {
                        Close();
                        Process.GetCurrentProcess().Kill();
                    }
                }
            }
        }
        void CtrlNotify()
        {
            isNotifyOpen = false;
            notifyIcon1.Visible = false;
            this.Close();
        }
    }
}
