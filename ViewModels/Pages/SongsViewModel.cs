using RPGGamer_Radio_Desktop.Helpers;
using RPGGamer_Radio_Desktop.Models;
using RPGGamer_Radio_Desktop.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.ViewModels.Pages
{
    public partial class SongsViewModel : ObservableObject, INavigationAware
    {
        public SongsViewModel(
            WebhookService webhookService,
            DatabaseService databaseService,
            MediaElementService mediaElementService,
            NotificationService notificationService)
        {
            _webhookService = webhookService;
            _databaseService = databaseService;
            _mediaElementService = mediaElementService;
            _notificationService = notificationService;
            object foundLinksLock = new();
            BindingOperations.EnableCollectionSynchronization(AllSongs, foundLinksLock);

            LoadData();
        }

        private bool _suppressNotifications;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotifications)
            {
                base.OnPropertyChanged(e);
            }
        }

        private void LoadData()
        {
            _suppressNotifications = true;
            new Thread(() =>
            {
                foreach (Song item in _databaseService.Read().Take(20))
                    AllSongs.Add(item);
                _suppressNotifications = false;
            }).Start();
        }

        private readonly WebhookService _webhookService;
        private readonly DatabaseService _databaseService;
        private readonly MediaElementService _mediaElementService;
        private readonly NotificationService _notificationService;

        public double FillWidth { get; set; }

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
        private ObservableCollection<Song> _allSongs = [];

        public void OnNavigatedTo() { }
        public void OnNavigatedFrom() { }

        public async Task LookForLinks()
        {
            string dataUrl = Path.Combine(_webhookService.ROOT, "data_");
            var tasks = new List<Task>();
            const int range = 4585;

            for (int i = 1; i <= range; i++)
            {
                string digit = i.ToString();
                while (digit.Length < 4)
                    digit = "0" + digit;
                char dataNumber = digit[0];
                string url = $"{dataUrl}{dataNumber}/{digit}.dat";

                await ReadSongInfoAsync(url, i);
            }

            Application.Current.Dispatcher.Invoke(() => Status = "");
        }

        private async Task ReadSongInfoAsync(string url, int id)
        {
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(url);
            Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
            var fileAbstraction = new StreamFileAbstraction(streamToReadFrom, "audio.mp3");
            var file = TagLib.File.Create(fileAbstraction);
            Song song = new()
            {
                Id = id,
                Url = url,
                Title = file.Tag.Title ?? "Unknown",
                Game = file.Tag.Album ?? "Unknown"
            };
            _databaseService.Insert(song);
        }

        private bool _subscribed;

        private void PlayMedia(Song song)
        {
            if (_mediaElementService.MediaElement is not MediaElement mediaElement) return;
            if(!_subscribed)
            {
                mediaElement.MediaEnded += Element_MediaEnded;
                _subscribed = true;
            }

            new Thread(async()=> await _notificationService.ShowNotificationAsync(song.Game, song.Title)).Start();

            mediaElement.Source = new(song.Url);
            mediaElement.Play();

            Status = $"{song.Game} | {song.Title}";

            IsPlaying = true;
        }

        [RelayCommand]
        public void PlayByButton(Song? song)
        {
            if (song is not Song s) return;
            PlayMedia(s);
        }

        [RelayCommand]
        public void PlayByID()
        {
            if (!int.TryParse(Search, out int id)) return;
            Search = string.Empty;
            PlayMedia(AllSongs[id]);
        }

        private void Element_MediaEnded(object sender, RoutedEventArgs e) => PlayRandomSong();

        [RelayCommand]
        public void PlayRandomSong()
        {
            Song song = AllSongs[Random.Shared.Next(AllSongs.Count - 1)];

            PlayMedia(song);
        }

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
