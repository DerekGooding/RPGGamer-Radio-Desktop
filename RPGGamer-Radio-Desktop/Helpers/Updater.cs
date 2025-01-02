using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace RPGGamer_Radio_Desktop.Helpers;

public static class Updater
{
    private const string GitHubApiUrl = "https://api.github.com/repos/DerekGooding/RPGGamer-Radio-Desktop/releases/latest";
    private const string CurrentVersion = "1.0.0";

    public static async Task CheckForUpdatesAsync()
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("request");

        try
        {
            string json = await client.GetStringAsync(GitHubApiUrl);
            var release = JsonSerializer.Deserialize<Release>(json);

            if (release != null && IsNewVersionAvailable(CurrentVersion, release.TagName))
            {
                string downloadUrl = release.Assets[0].BrowserDownloadUrl;
                string tempFilePath = Path.Combine(Path.GetTempPath(), "UpdateInstaller.exe");

                // Download the new installer
                using (var response = await client.GetAsync(downloadUrl))
                await using (var fs = new FileStream(tempFilePath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                // Run the installer
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFilePath,
                    UseShellExecute = true
                });

                Environment.Exit(0); // Close the app after starting the installer
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., network issues)
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    private static bool IsNewVersionAvailable(string currentVersion, string latestVersion) 
        => new Version(latestVersion.TrimStart('v')) > new Version(currentVersion);

    private class Release
    {
        public string TagName { get; set; } = string.Empty;
        public Asset[] Assets { get; set; } = [];
    }

    private class Asset
    {
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
