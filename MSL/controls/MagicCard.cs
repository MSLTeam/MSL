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
    ///     <MyNamespace:MagicCard/>
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

        public static readonly DependencyProperty MarginOverrideProperty =
            DependencyProperty.Register("MarginOverride", typeof(Thickness), typeof(MagicCard), new PropertyMetadata(new Thickness(10)));

        public Thickness MarginOverride
        {
            get { return (Thickness)GetValue(MarginOverrideProperty); }
            set { SetValue(MarginOverrideProperty, value); }
        }
    }
}
