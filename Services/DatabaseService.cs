using RPGGamer_Radio_Desktop.Models;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace RPGGamer_Radio_Desktop.Services;

public class DatabaseService
{
    private const char separator = '|';

    private readonly string _localAppData;
    private readonly string _userFilePath;
    private readonly string _database;
    private readonly string _imageCache;

    private readonly ConcurrentQueue<Action> _writeQueue = new();
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    public DatabaseService()
    {
        _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _userFilePath = Path.Combine(_localAppData, "RadioDesktop");
        _database = Path.Combine(_userFilePath, "database.csv");
        _imageCache = Path.Combine(_userFilePath, "ImageCache");

        if (!Directory.Exists(_userFilePath))
            Directory.CreateDirectory(_userFilePath);

        if (!Directory.Exists(_imageCache))
            Directory.CreateDirectory(_imageCache);

        InitializeCSV(_database);
        Task.Run(ProcessQueue);
    }

    /// <summary>
    /// Saves a BitmapImage to the cache folder with the specified key.
    /// </summary>
    /// <param name="key">Unique key for the image (e.g., resource name).</param>
    /// <param name="image">BitmapImage to save.</param>
    public void SaveImageToCache(string key, BitmapImage image)
    {
        string cacheFile = GetCacheFilePath(key);

        // Save image to disk if not already cached
        if (!File.Exists(cacheFile))
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using FileStream fileStream = new(cacheFile, FileMode.Create, FileAccess.Write);
            encoder.Save(fileStream);
        }
    }

    /// <summary>
    /// Loads a BitmapImage from the cache folder using the specified key.
    /// </summary>
    /// <param name="key">Unique key for the image (e.g., resource name).</param>
    /// <returns>The loaded BitmapImage, or null if the image is not found in the cache.</returns>
    public BitmapImage? LoadImageFromCache(string key)
    {
        string cacheFile = GetCacheFilePath(key);

        if (File.Exists(cacheFile))
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(cacheFile);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        return null;
    }

    /// <summary>
    /// Gets the file path for the cached image associated with the specified key.
    /// </summary>
    /// <param name="key">Unique key for the image.</param>
    /// <returns>Path to the cached image file.</returns>
    private string GetCacheFilePath(string key)
    {
        // Sanitize the key to create a valid filename
        string name = Path.GetFileNameWithoutExtension(key).Split('.').Last();
        string sanitizedKey = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_imageCache, sanitizedKey + ".png");
    }

    public void Insert(Song song)
    {
        _writeQueue.Enqueue(() => WriteToFile(() =>
        {
            using StreamWriter writer = new(_database, true);
            writer.WriteLine(SongToLine(song));
        }));
    }
    public void Update(Song song)
    {
        _writeQueue.Enqueue(() => WriteToFile(() =>
        {
            List<Song> list = Read();
            int index = list.FindIndex(s => s.Id == song.Id);
            list[index] = song;
            using StreamWriter writer = new(_database, false);
            foreach (Song s in list)
                writer.WriteLine(SongToLine(s));
        }));
    }

    public void Delete(Song song)
    {
        _writeQueue.Enqueue(() => WriteToFile(() =>
        {
            List<Song> list = Read();
            Song found = list.Find(s => s.Id == song.Id);
            list.Remove(found);
            using StreamWriter writer = new(_database, false);
            foreach (Song s in list)
                writer.WriteLine(SongToLine(s));
        }));
    }
    public List<Song> Read()
    {
        using StreamReader reader = new(_database);
        return reader
            .ReadToEnd()
            .Split(Environment.NewLine)
            .Where(line => !string.IsNullOrEmpty(line))
            .Select(LineToSong)
            .ToList();
    }

    private void InitializeCSV(string filePath)
    {
        if (File.Exists(filePath)) return;
        using StreamWriter writer = new(filePath);
        writer.WriteLine($"sep={separator}");
    }

    private Song LineToSong(string line)
    {
        string[] parts = line.Split(separator);
        return new Song(int.Parse(parts[0]), parts[1], parts[2], parts[3]);
    }

    private string SongToLine(Song song) 
        => $"{song.Id}{separator}{song.Url}{separator}{song.Game}{separator}{song.Title}";

    private async Task ProcessQueue()
    {
        while (true)
        {
            if (_writeQueue.TryDequeue(out var action))
            {
                await _writeSemaphore.WaitAsync();
                try
                {
                    action();
                }
                finally
                {
                    _writeSemaphore.Release();
                }
            }
            else
            {
                await Task.Delay(100); // Adjust delay as needed
            }
        }
    }

    private void WriteToFile(Action writeAction) => writeAction();
}
