using RPGGamer_Radio_Desktop.Helpers;
using RPGGamer_Radio_Desktop.Models;
using System.IO;
using System.Net.Http;

namespace RPGGamer_Radio_Desktop.Services;

public partial class WebhookService()
{
    //private readonly DatabaseService _databaseService = databaseService;

    public readonly string ROOT = "http://www.rpgamers.net/radio/data/"; //  + data_# + / +  4 digit number .dat

    public async Task LookForLinks()
    {
        string dataUrl = Path.Combine(ROOT, "data_");
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
        //_databaseService.Insert(song);
    }
}