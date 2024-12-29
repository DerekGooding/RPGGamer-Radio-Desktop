using NAudio.Wave;
using RPGGamer_Radio_Desktop.Models;
using RPGGamer_Radio_Desktop.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.ViewModels.Pages
{
    public partial class SongsViewModel : ObservableObject, INavigationAware
    {
        public SongsViewModel(
            WebhookService webhookService,
            DatabaseService databaseService,
            MediaElementService mediaElementService)
        {
            _webhookService = webhookService;
            _databaseService = databaseService;
            _mediaElementService = mediaElementService;
            FoundLinks = _databaseService.Read().Take(100).ToList();
        }

        private readonly WebhookService _webhookService;
        private readonly DatabaseService _databaseService;
        private readonly MediaElementService _mediaElementService;

        public double FillWidth { get; set; }

        [ObservableProperty]
        private string _status = "Ready";
        partial void OnStatusChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                _status = "Ready";
        }

        [ObservableProperty]
        private string _songCount = "No Songs";

        partial void OnSongCountChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                _songCount = "No Songs";
        }
        [ObservableProperty]
        private string _duration = string.Empty;

        [ObservableProperty]
        private double _volume = 0.5;
        partial void OnVolumeChanged(double value) => SetVolume();

        [ObservableProperty]
        private string _search = string.Empty;
        partial void OnSearchChanged(string value) => Query();

        [ObservableProperty]
        private Song? _selectedSong = null;
        partial void OnSelectedSongChanged(Song? value)
        {
            if(value is Song song)
                PlayMedia(song);
        }
        [ObservableProperty]
        private bool _isPlaying = false;
        [ObservableProperty]
        private bool _isRequesting = true;

        [ObservableProperty]
        private List<Song> _foundLinks;

        [ObservableProperty]
        private ObservableCollection<Song> _filteredSongs = [];

        //[ObservableProperty]
        //public List<Song> _allSongs = [];

        [ObservableProperty]
        public Stack<Song> _previousSongs = [];


        public void OnNavigatedTo() { }

        public void OnNavigatedFrom() { }

        //private void ReadSongs()
        //{
        //    FoundLinks.Clear();
        //    List<Song> allSongs = DatabaseHelper.Read<Song>(DatabaseHelper.Target.Database);
        //    if (allSongs.Count == 0)
        //    {
        //        DatabaseHelper.ImportFromOnlineAsync();
        //        allSongs = DatabaseHelper.Read<Song>(DatabaseHelper.Target.Database);
        //    }

        //    if (allSongs.Count == 0)
        //        return;

        //    foreach (var item in allSongs)
        //        FoundLinks.Add(item);
        //    Query();
        //}

        public void Query()
        {
            FilteredSongs = [.. FoundLinks.Where(x => x.Game?.ToLower().Contains(Search, StringComparison.CurrentCultureIgnoreCase) == true).OrderBy(s => s.Url)];
            SongCount = $"{FilteredSongs.Count} songs";
        }

        public async Task LookForLinks()
        {
            string dataUrl = Path.Combine(_webhookService.ROOT, "data_");
            var tasks = new List<Task>();
            //const int range = 1000;
            const int range = 4585;

            for (int i = 0; i <= range; i++)
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
            using StreamReader sr = new(streamToReadFrom, Encoding.UTF8);
            string line = sr.ReadLine() ?? "";

            line = Decode(line);

            if (line.Contains("DOCTYPE"))
                return;

            var gameSplit = line.Split("TALB", StringSplitOptions.None);
            string game = string.Empty;
            if (gameSplit.Length < 2)
                game = gameSplit[0];
            else
                game = gameSplit[1].Split("TPE1", StringSplitOptions.None)[0];

            var titleSplit = line.Split("TIT2", StringSplitOptions.None);
            string title = string.Empty;
            if (titleSplit.Length < 2)
                title = titleSplit[0];
            else
                title = titleSplit[1].Split("TRCK", StringSplitOptions.None)[0];
            //string title = $"{id}";
            //string game = "Nothing";

            Song song = new()
            {
                Id = id,
                Url = url,
                Title = title,
                Game = game
            };

            _databaseService.Insert(song);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Status = $"{id}";
                FoundLinks.Add(song);
            });
        }

        private static string Decode(string input) => WebhookService.MyRegex().Replace(input, string.Empty);

        private bool subscribed = false;

        private void PlayMedia(Song song, bool isPrevious = false)
        {
            if (_mediaElementService.MediaElement is not MediaElement mediaElement) return;

            mediaElement.Source = new(song.Url);
            mediaElement.Play();
            SetVolume();
            if (!isPrevious)
                PreviousSongs.Push(song);
            CheckHistory();
            Status = $"{song.Game} | {song.Title}";

            IsPlaying = true;
            if (!subscribed)
            {
                mediaElement.MediaEnded += Element_MediaEnded;
                subscribed = true;
            }
            //_webhookService.WaveFormSource = new BitmapImage();
            //new Thread(() =>
            //{
            //    using WaveFileReader waveStream = new(song.Url);
            //    var image = _webhookService.WaveFormRenderer.Render(waveStream, new MaxPeakProvider(), new StandardWaveFormRendererSettings() { Width = 1650 });
            //    if (image == null) return;
            //    using var ms = new MemoryStream();
            //    image.Save(ms, ImageFormat.Bmp);
            //    ms.Seek(0, SeekOrigin.Begin);

            //    var bitmapImage = new BitmapImage();
            //    bitmapImage.BeginInit();
            //    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            //    bitmapImage.StreamSource = ms;
            //    bitmapImage.EndInit();
            //    bitmapImage.Freeze();

            //    Dispatcher.CurrentDispatcher.Invoke(() => _webhookService.WaveFormSource = bitmapImage);

            //}).Start();
        }

        private void CheckHistory()
        {
            if (PreviousSongs.Count < 50) return;
            Stack<Song> temp = new();
            for (int i = 0; i < 10; i++)
                temp.Push(PreviousSongs.Pop());
            PreviousSongs.Clear();
            for (int i = 0; i < 10; i++)
                PreviousSongs.Push(temp.Pop());
        }

        private void SetVolume()
        {
            if (_mediaElementService.MediaElement is not MediaElement mediaElement) return;
            mediaElement.Volume = Volume;
        }

        private void StartTimer()
        {
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromMilliseconds(20)
            };
            timer.Tick += TimerTick;
            timer.Start();
        }

        void TimerTick(object? sender, EventArgs e)
        {
            if (_mediaElementService.MediaElement is not MediaElement mediaElement || !mediaElement.NaturalDuration.HasTimeSpan) return;

            Duration = string.Format(
                "{0} / {1}",
                mediaElement.Position.ToString(@"mm\:ss"),
                mediaElement.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
            FillWidth = mediaElement.Position.TotalMilliseconds / mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds * 800;
        }

        private void Element_MediaEnded(object sender, RoutedEventArgs e) => PlayRandomSong();

        public void PlayRandomSong()
        {
            Random rand = new();
            SelectedSong = FilteredSongs[rand.Next(FilteredSongs.Count)];

            if (SelectedSong is Song song)
                PlayMedia(song);
        }
        public void PlayPrevious()
        {
            if (PreviousSongs.Count == 0) return;
            PlayMedia(PreviousSongs.Pop(), true);
        }

        public static async Task SaveSong(Song song)
        {
            string userRoot = Environment.GetEnvironmentVariable("USERPROFILE") ?? "C:\\";
            string downloadFolder = Path.Combine(userRoot, "Downloads", $"{song.Title}.mp3");

            //MessageBox.Show($"Downloading to:\n{downloadFolder}");
            HttpClient client = new();
            await using var stream = await client.GetStreamAsync(song.Url);
            await using var fileStream = new FileStream(downloadFolder, FileMode.CreateNew);
            await stream.CopyToAsync(fileStream);
        }

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
                if (SelectedSong == null) PlayRandomSong();
                mediaElement.Play();
                IsPlaying = true;
            }
        }
    }
}
