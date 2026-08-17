using HandyControl.Data;
using HandyControl.Themes;
using HandyControl.Tools;
using MSL.utils.Config;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSL.utils
{
    /// <summary>
    /// 共享皮肤管理 — MainWindow 和 ServerRunner 共用
    /// </summary>
    public static class SkinHelper
    {
        public static ImageBrush BackImageBrush;

        /// <summary>
        /// 应用皮肤样式到指定窗口
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="sideMenuPanel">侧边栏面板（可为 null）</param>
        /// <param name="contentBorder">内容区域边框控件（可为 null，用于背景图时去除边框）</param>
        public static void ApplySkin(Window window, Panel sideMenuPanel = null, Control contentBorder = null)
        {
            try
            {
                var cfg = AppConfig.Current;
                if (cfg.MicaEffect)
                {
                    if (File.Exists("MSL\\Background.png")) DisposeBackImage(window, contentBorder);
                    ChangeTitleStyle(window, true);
                    ThemeManager.Current.UsingSystemTheme = true;
                    window.SetValue(HandyControl.Controls.Window.SystemBackdropTypeProperty, BackdropType.Auto);
                    window.SetValue(HandyControl.Controls.Window.SystemBackdropTypeProperty, BackdropType.Mica);
                    if (sideMenuPanel != null)
                        sideMenuPanel.Background = Brushes.Transparent;
                }
                else
                {
                    window.SetValue(HandyControl.Controls.Window.SystemBackdropTypeProperty, BackdropType.Auto);
                    window.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush");
                    if (sideMenuPanel != null)
                        sideMenuPanel.SetResourceReference(Panel.BackgroundProperty, "SideMenuBrush");

                    if (cfg.DarkTheme != "Auto")
                        ThemeManager.Current.UsingSystemTheme = false;

                    ChangeTitleStyle(window, cfg.SemitransparentTitle);

                    if (File.Exists("MSL\\Background.png"))
                    {
                        if (BackImageBrush != null)
                        {
                            BackImageBrush = null;
                            GC.Collect();
                        }
                        BackImageBrush = new ImageBrush(GetImage("MSL\\Background.png"))
                        {
                            Stretch = Stretch.UniformToFill
                        };
                        window.Background = BackImageBrush;
                        if (contentBorder != null)
                            contentBorder.BorderThickness = new Thickness(0);
                    }
                    else
                    {
                        DisposeBackImage(window, contentBorder);
                    }
                }
            }
            catch { }
        }

        public static void ChangeTitleStyle(Window window, bool isOpen)
        {
            if (isOpen)
            {
                window.SetResourceReference(HandyControl.Controls.Window.NonClientAreaBackgroundProperty, "SideMenuBrush");
                window.SetResourceReference(HandyControl.Controls.Window.NonClientAreaForegroundProperty, "PrimaryTextBrush");
                window.SetResourceReference(HandyControl.Controls.Window.CloseButtonForegroundProperty, "PrimaryTextBrush");
                window.SetResourceReference(HandyControl.Controls.Window.OtherButtonForegroundProperty, "PrimaryTextBrush");
                window.SetResourceReference(HandyControl.Controls.Window.OtherButtonHoverForegroundProperty, "PrimaryTextBrush");
            }
            else
            {
                window.SetResourceReference(HandyControl.Controls.Window.NonClientAreaBackgroundProperty, "PrimaryBrush");
                window.SetValue(HandyControl.Controls.Window.NonClientAreaForegroundProperty, Brushes.White);
                window.SetValue(HandyControl.Controls.Window.CloseButtonForegroundProperty, Brushes.White);
                window.SetValue(HandyControl.Controls.Window.OtherButtonForegroundProperty, Brushes.White);
                window.SetValue(HandyControl.Controls.Window.OtherButtonHoverForegroundProperty, Brushes.White);
            }
        }

        private static void DisposeBackImage(Window window, Control contentBorder)
        {
            if (BackImageBrush == null) return;
            window.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush");
            if (contentBorder != null)
                contentBorder.BorderThickness = new Thickness(1, 0, 0, 0);
            _ = Task.Run(async () =>
            {
                await Task.Delay(400);
                BackImageBrush = null;
                await Task.Delay(100);
                GC.Collect();
            });
        }

        private static BitmapImage GetImage(string imagePath)
        {
            var bitmap = new BitmapImage();
            if (!File.Exists(imagePath)) return bitmap;
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            using (var ms = new MemoryStream(File.ReadAllBytes(imagePath)))
            {
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            return bitmap;
        }
    }
}
