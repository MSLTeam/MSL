namespace MSL.controls
{
    /// <summary>
    /// TextDialog.xaml 的交互逻辑
    /// </summary>
    public partial class TextDialog
    {
        public TextDialog(string text = "Plase Wait")
        {
            InitializeComponent();
            DialogText.Text = text;
        }
    }
}
