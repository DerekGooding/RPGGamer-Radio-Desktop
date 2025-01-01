using RPGGamer_Radio_Desktop.Models;
using RPGGamer_Radio_Desktop.Services;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.ViewModels.Pages
{
    public partial class SongsViewModel(
        WebhookService webhookService,
        MediaElementService mediaElementService,
        NotificationService notificationService) : ObservableObject, INavigationAware
    {
        private readonly WebhookService _webhookService = webhookService;
        private readonly MediaElementService _mediaElementService = mediaElementService;
        private readonly NotificationService _notificationService = notificationService;



        [ObservableProperty]
        private string _status = "Ready";
        partial void OnStatusChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                _status = "Ready";
        }

        [ObservableProperty]
        private string _duration = string.Empty;

        [ObservableProperty]
        private double _volume = 0.5;
        partial void OnVolumeChanged(double value)
        {
            if (_mediaElementService.MediaElement is not MediaElement mediaElement) return;
            mediaElement.Volume = Volume;
        }

        [ObservableProperty]
        private string _search = string.Empty;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _isRequesting = true;

        [ObservableProperty]
        private List<SongImage> _songImages = [];

        public void OnNavigatedTo() { SongImages = _mediaElementService.SongImages; }
        public void OnNavigatedFrom() { }

        private bool _subscribed;

        private void PlayMedia(SongImage songImage)
        {
            if (_mediaElementService.MediaElement is not MediaElement mediaElement) return;
            if(!_subscribed)
            {
                mediaElement.MediaEnded += Element_MediaEnded;
                _subscribed = true;
            }

            Song song = songImage.Song;

            new Thread(async()=> await _notificationService.ShowNotificationAsync(song.Game, song.Title)).Start();

            mediaElement.Source = new(song.Url);
            mediaElement.Play();

            Status = $"{song.Game} | {song.Title}";
            _mediaElementService.CurrentlyPlaying = songImage;

            IsPlaying = true;
        }

        [RelayCommand]
        public void PlayByButton(SongImage? songImage)
        {
            if (songImage is not SongImage s) return;
            PlayMedia(s);
        }

        [RelayCommand]
        public void PlayByID()
        {
            if (!int.TryParse(Search, out int id)) return;
            Search = string.Empty;
            PlayMedia(SongImages[id]);
        }

        private void Element_MediaEnded(object sender, RoutedEventArgs e) => PlayRandomSong();

        [RelayCommand]
        public void PlayRandomSong() => PlayMedia(SongImages[Random.Shared.Next(SongImages.Count - 1)]);

        [RelayCommand]
        public void Pause()
        {
            if (_mediaElementService.MediaElement is not MediaElement mediaElement) return;
            if (IsPlaying)
            {
                mediaElement.Pause();
                IsPlaying = false;
            }
            else
            {
                mediaElement.Play();
                IsPlaying = true;
            }
        }
    }
}
