using RPGGamer_Radio_Desktop.Models;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace RPGGamer_Radio_Desktop.Services;

public class DatabaseService
{
    private const char separator = '|';

    private readonly Uri _database = new("Assets\\Links\\database.csv", UriKind.Relative);

    private readonly ConcurrentQueue<Action> _writeQueue = new();
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    public DatabaseService()
    {
        Task.Run(ProcessQueue);
    }

    //public void Insert(Song song)
    //{
    //    _writeQueue.Enqueue(() => WriteToFile(() =>
    //    {
    //        using StreamWriter writer = new(_database, true);
    //        writer.WriteLine(SongToLine(song));
    //    }));
    //}
    //public void Update(Song song)
    //{
    //    _writeQueue.Enqueue(() => WriteToFile(() =>
    //    {
    //        List<Song> list = Read();
    //        int index = list.FindIndex(s => s.Id == song.Id);
    //        list[index] = song;
    //        using StreamWriter writer = new(_database, false);
    //        foreach (Song s in list)
    //            writer.WriteLine(SongToLine(s));
    //    }));
    //}

    //public void Delete(Song song)
    //{
    //    _writeQueue.Enqueue(() => WriteToFile(() =>
    //    {
    //        List<Song> list = Read();
    //        Song found = list.Find(s => s.Id == song.Id);
    //        list.Remove(found);
    //        using Stream stream = Application.GetContentStream(_database).Stream;
    //        using StreamWriter writer = new(stream);
    //        foreach (Song s in list)
    //            writer.WriteLine(SongToLine(s));
    //    }));
    //}
    public List<Song> Read()
    {
        using Stream stream = Application.GetContentStream(_database).Stream;
        using StreamReader reader = new(stream);
        return reader
            .ReadToEnd()
            .Split(Environment.NewLine)
            .Where(line => !string.IsNullOrEmpty(line))
            .Select(LineToSong)
            .ToList();
    }

    //private void InitializeCSV(string filePath)
    //{
    //    if (File.Exists(filePath)) return;
    //    using StreamWriter writer = new(filePath);
    //}

    private Song LineToSong(string line)
    {
        string[] parts = line.Split(separator);
        return new Song(int.Parse(parts[0]), parts[1], parts[2], parts[3]);
    }

    //private string SongToLine(Song song) 
    //    => $"{song.Id}{separator}{song.Url}{separator}{song.Game}{separator}{song.Title}";

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

    //private void WriteToFile(Action writeAction) => writeAction();
}
