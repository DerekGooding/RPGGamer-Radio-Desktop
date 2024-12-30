using NAudio.WaveFormRenderer;
using System.Windows.Media;

namespace RPGGamer_Radio_Desktop.Services;

public partial class WebhookService
{
    public readonly string ROOT = "http://www.rpgamers.net/radio/data/"; //  + data_# + / +  4 digit number .dat

    public WaveFormRenderer WaveFormRenderer { get; } = new();
    public ImageSource? WaveFormSource { get; set; }
}