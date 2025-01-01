using NAudio.Wave;
using RPGGamer_Radio_Desktop.Models;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace RPGGamer_Radio_Desktop.Services;

public class AudioService
{
    public System.Windows.Media.ImageSource GetGrahpicFromWave(SongImage song)
    {
        using var reader = new AudioFileReader(song.Song.Url);

        const int height = 1080; // Image height
        const int width = height * 20;  // Image width
        var bitmap = new Bitmap(width, height);
        var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);

        // Create a pen to draw the waveform
        using var pen = new Pen(Color.DarkRed, 1);
        var samples = new float[reader.Length];
        int samplesRead = reader.Read(samples, 0, samples.Length);

        // Number of samples per pixel
        int samplesPerPixel = samplesRead / width;
        for (int i = 0; i < width; i++)
        {
            float sampleSum = 0f;
            int sampleCount = 0;

            // Calculate average sample value for each pixel
            for (int j = 0; j < samplesPerPixel; j++)
            {
                int sampleIndex = (i * samplesPerPixel) + j;
                if (sampleIndex < samples.Length)
                {
                    sampleSum += Math.Abs(samples[sampleIndex]);
                    sampleCount++;
                }
            }

            if (sampleCount > 0)
            {
                float averageSample = sampleSum / sampleCount;
                int y = (int)(averageSample * height);
                y = Math.Min(height - 1, Math.Max(0, y));
                y *= 2;

                // Draw a line for this pixel
                const int size = height / 2;
                graphics.DrawLine(new Pen(Color.DarkRed, 1), i, size, i, size - y);
                graphics.DrawLine(new Pen(Color.Orange , 1), i, size, i, size + y);
            }
        }

        return ConvertToBitmapSource(bitmap);
    }

    private BitmapSource ConvertToBitmapSource(Bitmap bitmap)
    {
        using var memoryStream = new System.IO.MemoryStream();
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        memoryStream.Seek(0, System.IO.SeekOrigin.Begin);

        return BitmapFrame.Create(memoryStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
    }
}
