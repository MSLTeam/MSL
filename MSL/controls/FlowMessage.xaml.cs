using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MSL.controls
{
    /// <summary>
    /// FlowMessage.xaml 的交互逻辑
    /// </summary>
    public partial class FlowMessage : UserControl
    {
        private DispatcherTimer timer;
        public FlowMessage()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
        }

        // 定时器到时后触发隐藏消息的方法
        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            HideMessage();
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="message">要显示的消息文本</param>
        /// <param name="seconds">显示时长，单位：秒（默认 3 秒）</param>
        public void ShowMessage(string message, int seconds = 3)
        {
            MessageTextBlock.Text = message;

            // 先设置为可见，然后开始淡入动画
            MessageBorder.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            MessageBorder.BeginAnimation(OpacityProperty, fadeIn);

            // 设置定时器，在指定秒数后执行隐藏操作
            timer.Interval = TimeSpan.FromSeconds(seconds);
            timer.Start();
        }

        /// <summary>
        /// 隐藏消息（使用淡出动画）
        /// </summary>
        private void HideMessage()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, e) =>
            {
                MessageBorder.Visibility = Visibility.Collapsed;
            };
            MessageBorder.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
