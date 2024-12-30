namespace RPGGamer_Radio_Desktop.Services;

public class NotificationService
{
    public async Task ShowNotificationAsync(string title, string message, int duration = 3000)
        => await Application.Current.Dispatcher.Invoke(async () =>
            {
                var notification = new Views.Windows.NotificationWindow(title, message)
                {
                    Width = 300,
                    Height = 100
                };
                await notification.ShowNotificationAsync(duration);
            });
}