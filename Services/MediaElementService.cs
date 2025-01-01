using RPGGamer_Radio_Desktop.Models;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace RPGGamer_Radio_Desktop.Services;

public class MediaElementService
{
    private readonly DatabaseService _databaseService;
    private readonly NotificationService _notificationService;
    private readonly Dictionary<string, BitmapImage> _imageCache = [];



    public MediaElementService(DatabaseService databaseService, NotificationService notificationService)
    {
        _databaseService = databaseService;
        _notificationService = notificationService;

        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] _imageSources = assembly.GetManifestResourceNames();
        HashSet<string> imageSourceSet = new(_imageSources);

        SongImages = _databaseService.Read()
                .ConvertAll(song => new SongImage(song, GetCachedImage(song, imageSourceSet, assembly, _imageSources)));
    }

    public MediaElement? MediaElement { get; set; }

    public List<SongImage> SongImages { get; } = [];

    private bool isPlaying;
    public bool IsPlaying
    {
        get => isPlaying; set
        {
            isPlaying = value;
            PlayStatusChange?.Invoke(value, EventArgs.Empty);
        }
    }
    public SongImage CurrentlyPlaying
    {
        get { return currentlyPlaying; }
        set
        {
            currentlyPlaying = value;
            SongChange?.Invoke(value, EventArgs.Empty);
        }
    }

    private SongImage currentlyPlaying = new() { Song = new() { Game = "None", Title = "None" } };

    public EventHandler? SongChange;

    public EventHandler? PlayStatusChange;

    private bool _subscribed;


    public void PlayMedia(SongImage songImage)
    {
        if (MediaElement is not MediaElement mediaElement) return;
        if (!_subscribed)
        {
            mediaElement.MediaEnded += Element_MediaEnded;
            _subscribed = true;
        }

        Song song = songImage.Song;

        new Thread(async () => await _notificationService.ShowNotificationAsync(song.Game, song.Title)).Start();

        mediaElement.Source = new(song.Url);
        mediaElement.Play();

        CurrentlyPlaying = songImage;

        IsPlaying = true;
    }

    private void Element_MediaEnded(object sender, RoutedEventArgs e) => PlayRandomSong();

    public void PlayRandomSong() => PlayMedia(SongImages[Random.Shared.Next(SongImages.Count - 1)]);

    public void Pause()
    {
        if (MediaElement is not MediaElement mediaElement) return;
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


    private ImageSource GetCachedImage(Song song, HashSet<string> imageSourceSet, Assembly assembly, string[] fallbackSources)
    {
        string resourceName = imageSourceSet.FirstOrDefault(name => name.Contains($"{song.Game}.jpg"))
                              ?? fallbackSources[1];

        // Return from cache if already loaded
        if (_imageCache.TryGetValue(resourceName, out BitmapImage? cachedImage))
            return cachedImage;

        // Load the image if not in cache
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly.");

        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.StreamSource = stream;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();

        // Cache the loaded image
        _imageCache[resourceName] = bitmap;

        return bitmap;
    }
}
