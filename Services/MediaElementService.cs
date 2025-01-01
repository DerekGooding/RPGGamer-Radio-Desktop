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
    private readonly Dictionary<string, BitmapImage> _imageCache = [];

    public MediaElementService(DatabaseService databaseService)
    {
        _databaseService = databaseService;

        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] _imageSources = assembly.GetManifestResourceNames();
        HashSet<string> imageSourceSet = new(_imageSources);

        SongImages = _databaseService.Read()
                .ConvertAll(song => new SongImage(song, GetCachedImage(song, imageSourceSet, assembly, _imageSources)));
    }

    public MediaElement? MediaElement { get; set; }

    public List<SongImage> SongImages { get; } = [];

    public SongImage CurrentlyPlaying
    {
        get { return currentlyPlaying; }
        set
        {
            currentlyPlaying = value;
            SongChange?.Invoke(value, EventArgs.Empty);
        }
    }

    public EventHandler? SongChange;
    private SongImage currentlyPlaying = new() { Song = new() { Game = "None", Title = "None" } };

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
