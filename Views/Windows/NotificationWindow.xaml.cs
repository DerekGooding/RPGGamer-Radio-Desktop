namespace RPGGamer_Radio_Desktop.Views.Windows;

/// <summary>
/// Interaction logic for NotificationWindow.xaml
/// </summary>
public partial class NotificationWindow : Window
{
    public NotificationWindow(string title, string message)
    {
        InitializeComponent();
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
    }

    public async Task ShowNotificationAsync(int duration = 3000)
    {
        Top = SystemParameters.WorkArea.Top + 10;
        Left = SystemParameters.WorkArea.Right - Width - 10;

        Show();

        await Task.Delay(duration);
        Close();
    }
}
