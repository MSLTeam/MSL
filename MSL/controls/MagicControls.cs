using System.Windows;
using System.Windows.Controls;


namespace MSL.controls
{
    /// <summary>
    /// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
    ///
    /// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MSL.controls"
    ///
    ///
    /// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MSL.controls;assembly=MSL.controls"
    ///
    /// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
    /// 并重新生成以避免编译错误:
    ///
    ///     在解决方案资源管理器中右击目标项目，然后依次单击
    ///     “添加引用”->“项目”->[浏览查找并选择此项目]
    ///
    ///
    /// 步骤 2)
    /// 继续操作并在 XAML 文件中使用控件。
    ///
    ///     <MyNamespace:MagicControls/>
    ///
    /// </summary>

    public class MagicCard : ContentControl
    {
        static MagicCard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MagicCard), new FrameworkPropertyMetadata(typeof(MagicCard)));
        }

        // 定义 Title 依赖属性
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(MagicCard), new PropertyMetadata(string.Empty));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static new readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register("Padding", typeof(Thickness), typeof(MagicCard), new PropertyMetadata(new Thickness(10)));

        public new Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }
    }

    public class MagicScrollViewer : ItemsControl
    {
        static MagicScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MagicScrollViewer), new FrameworkPropertyMetadata(typeof(MagicScrollViewer)));
        }
    }
    
    public class MagicListBox : ListBox
    {
        static MagicListBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MagicListBox), new FrameworkPropertyMetadata(typeof(MagicListBox)));
        }
    }

    public class MagicListBox1 : ListBox
    {
        static MagicListBox1()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MagicListBox1), new FrameworkPropertyMetadata(typeof(MagicListBox1)));
        }
    }

    /* MagicGrowlPanel 用处不多不大，暂时弃用
    public class MagicGrowlPanel : Control
    {
        static MagicGrowlPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MagicGrowlPanel), new FrameworkPropertyMetadata(typeof(MagicGrowlPanel)));
        }

        public MagicGrowlPanel()
        {
            this.Loaded += GrowlPanelControl_Loaded;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // 获取模板中的 GrowlPanel 和 ScrollViewer
            GrowlPanel = GetTemplateChild("GrowlPanel") as StackPanel;
            GrowlScrollViewer = GetTemplateChild("GrowlScrollViewer") as ScrollViewer;
        }

        private void GrowlPanelControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.TemplatedParent is Window window)
            {
                window.Activated += (s, args) => HandyControl.Controls.Growl.SetGrowlParent(this.GrowlPanel, true);
                window.Deactivated += (s, args) => HandyControl.Controls.Growl.SetGrowlParent(this.GrowlPanel, false);
            }
        }

        public StackPanel GrowlPanel { get; private set; }
        public ScrollViewer GrowlScrollViewer { get; private set; }
    }
    */
}
