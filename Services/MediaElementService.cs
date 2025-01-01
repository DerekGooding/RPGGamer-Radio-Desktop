using RPGGamer_Radio_Desktop.Models;
using System.Windows.Controls;

namespace RPGGamer_Radio_Desktop.Services;

public class MediaElementService()
{
    public MediaElement? MediaElement { get; set; }

    public SongImage CurrentlyPlaying
    {
        get { return currentlyPlaying; }
        set
        {
            currentlyPlaying = value;
            SongChange?.Invoke(value, EventArgs.Empty);
        }
    }

    public EventHandler SongChange;
    private SongImage currentlyPlaying = new() { Song = new() { Game = "None", Title = "None" } };
}
