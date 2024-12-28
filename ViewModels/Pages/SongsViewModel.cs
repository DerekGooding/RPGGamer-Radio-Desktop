using NAudio.Wave;
using NAudio.WaveFormRenderer;
using RPGGamer_Radio_Desktop.Models;
using RPGGamer_Radio_Desktop.Services;
using RPGGamer_Radio_Desktop.Views.Pages;
using RPGGamer_Radio_Desktop.Views.Windows;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.ViewModels.Pages
{
    public partial class SongsViewModel(WebhookService webhookService, SongsPage songsPage) : ObservableObject, INavigationAware
    {
        private WebhookService _webhookService = webhookService;
        private MediaElement _mediaElement = songsPage.MyPlayer;
        public double FillWidth { get; set; }

        [ObservableProperty]
        private string _status = "Ready";
        partial void OnStatusChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                _status = "Ready";
            //else
            //    _status = value;
        }

        [ObservableProperty]
        private string _songCount = "No Songs";

        partial void OnSongCountChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                _songCount = "No Songs";
            //else
            //    _songCount = value;
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
                PlaySong(song);
        }
        [ObservableProperty]
        private bool _isPlaying = false;
        [ObservableProperty]
        private bool _isRequesting = true;

        [ObservableProperty]
        private List<Song> _foundLinks = [];
        [ObservableProperty]
        private List<Song> _filteredSongs = [];
        [ObservableProperty]
        public List<Song> _allSongs = [];
        [ObservableProperty]
        public Stack<Song> _previousSongs = [];
        public void OnNavigatedTo() { }
        public void OnNavigatedFrom() { }


        private void ReadSongs()
        {
            FoundLinks.Clear();

            foreach (var item in AllSongs)
                FoundLinks.Add(item);

            Query();
        }

        public void Query()
        {
            FilteredSongs = [.. FoundLinks.Where(x => x.Game?.ToLower().Contains(Search.ToLower()) == true).OrderBy(s => s.Url)];
            SongCount = $"{FilteredSongs.Count} songs";
        }

        public Task LookForLinksAsync()
        {
            string dataUrl = Path.Combine(_webhookService.ROOT, "data_");
            for (int i = 0; i < 4000; i++)
            {
                string digit = i.ToString();
                char dataNumber = digit[0];
                while (digit.Length < 4)
                    digit = "0" + digit;
                string url = $"{dataUrl}{dataNumber}/{digit}.dat";
                _ = ReadSongInfoAsync(url, i);
            }
            return Task.CompletedTask;
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

            Song song = new()
            {
                Id = id,
                Url = url,
                Title = title,
                Game = game
            };

            ReadSongs();
        }

        private static string Decode(string input) => WebhookService.MyRegex().Replace(input, string.Empty);

        private bool subscribed = false;
        private void PlaySong(Song song) => PlaySong(song, false);
        private void PlaySong(Song song, bool isPrevious)
        {
            _mediaElement.Source = new Uri(song.Url);
            _mediaElement.Play();
            SetVolume();
            if (!isPrevious)
                PreviousSongs.Push(song);
            CheckHistory();
            Status = $"{song.Game} | {song.Title}";

            IsPlaying = true;
            if (!subscribed)
            {
                _mediaElement.MediaEnded += Element_MediaEnded;
                subscribed = true;
            }
            _webhookService.WaveFormSource = new BitmapImage();
            new Thread(() =>
            {
                using var waveStream = new WaveFileReader(song.Url);
                var image = _webhookService.WaveFormRenderer.Render(waveStream, new MaxPeakProvider(), new StandardWaveFormRendererSettings() { Width = 1650 });
                if (image == null) return;
                using var ms = new MemoryStream();
                image.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                Dispatcher.CurrentDispatcher.Invoke(() => _webhookService.WaveFormSource = bitmapImage);

            }).Start();
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

        private void SetVolume() => _mediaElement.Volume = Volume;

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
            if (_mediaElement.NaturalDuration.HasTimeSpan)
            {
                Duration = string.Format("{0} / {1}", _mediaElement.Position.ToString(@"mm\:ss"), _mediaElement.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
                FillWidth = _mediaElement.Position.TotalMilliseconds / _mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds * 800;
            }
        }

        private void Element_MediaEnded(object sender, RoutedEventArgs e) => PlayRandomSong();

        public void PlayRandomSong()
        {
            Random rand = new();
            SelectedSong = FilteredSongs[rand.Next(FilteredSongs.Count)];

            if (SelectedSong is Song song)
                PlaySong(song);
        }
        public void PlayPrevious()
        {
            if (PreviousSongs.Count == 0) return;
            PlaySong(PreviousSongs.Pop(), true);
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
            if (IsPlaying)
            {
                _mediaElement.Pause();
                IsPlaying = false;
            }
            else
            {
                if (SelectedSong == null) PlayRandomSong();
                _mediaElement.Play();
                IsPlaying = true;
            }
        }
    }
}
