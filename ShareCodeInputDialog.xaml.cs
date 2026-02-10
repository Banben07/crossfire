namespace CrossfireCrosshair;

public partial class ShareCodeInputDialog : System.Windows.Window
{
    public string ShareCode { get; private set; } = string.Empty;

    public ShareCodeInputDialog(string initialCode = "")
    {
        InitializeComponent();
        ShareCodeTextBox.Text = initialCode;
        ShareCodeTextBox.SelectAll();
    }

    private void Import_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        ShareCode = ShareCodeTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ShareCode))
        {
            System.Windows.MessageBox.Show(
                this,
                "请先粘贴分享码。",
                "导入分享码",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }
}
