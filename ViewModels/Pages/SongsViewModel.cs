using RPGGamer_Radio_Desktop.Helpers;
using RPGGamer_Radio_Desktop.Models;
using RPGGamer_Radio_Desktop.Services;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.ViewModels.Pages
{
    public partial class SongsViewModel(
        WebhookService webhookService,
        DatabaseService databaseService,
        MediaElementService mediaElementService,
        NotificationService notificationService) : ObservableObject, INavigationAware
    {
        private readonly WebhookService _webhookService = webhookService;
        private readonly DatabaseService _databaseService = databaseService;
        private readonly MediaElementService _mediaElementService = mediaElementService;
        private readonly NotificationService _notificationService = notificationService;

        private bool _initialized;

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            //Assembly assembly = Assembly.GetExecutingAssembly();
            //string[] _imageSources = assembly.GetManifestResourceNames();

            var list = _databaseService.Read();
            //ImageSource first = GetImage(list[0], _imageSources, assembly);

            //List<SongImage> collection = [.. _databaseService.Read().Select(x => new SongImage(x, GetImage(x, _imageSources, assembly)))];
            //List<SongImage> collection = list.ConvertAll(x => new SongImage(x, first));
            SongImages = list.ToList();
        }

        private ImageSource GetImage(Song song, string[] imageSources, Assembly assembly)
        {
            string? find = imageSources.FirstOrDefault(x => x.Contains(song.Game+".jpg"));


            using Stream? stream = assembly.GetManifestResourceStream(find ?? imageSources[1])
                ?? throw new FileNotFoundException($"Resource '{find}' not found in assembly.");

            // Create a BitmapImage and load the stream
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            return bitmap;
        }

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
        private List<Song> _songImages = [];

        public void OnNavigatedTo() { Initialize(); }
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
