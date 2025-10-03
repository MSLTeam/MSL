using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace MSL.controls.ctrls_serverrunner
{
    /// <summary>
    /// NoMods.xaml 的交互逻辑
    /// </summary>
    public partial class NoMods : UserControl
    {
        public static readonly DependencyProperty RefreshCommandProperty =
        DependencyProperty.Register(
            nameof(RefreshCommand),
            typeof(ICommand),
            typeof(NoMods),
            new PropertyMetadata(null));

        public ICommand RefreshCommand
        {
            get { return (ICommand)GetValue(RefreshCommandProperty); }
            set { SetValue(RefreshCommandProperty, value); }
        }

        public NoMods()
        {
            InitializeComponent();
        }
    }
}
