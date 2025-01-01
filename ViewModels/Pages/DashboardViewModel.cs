using RPGGamer_Radio_Desktop.Models;
using RPGGamer_Radio_Desktop.Services;

namespace RPGGamer_Radio_Desktop.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        public MediaElementService MediaElementService { get; }


        [ObservableProperty]
        private string _duration = "00:00";

        [ObservableProperty]
        private string _currentPoint = "00:00";

        [ObservableProperty]
        private SongImage _currentlyPlaying = new() { Song = new() { Game = "None", Title = "None" } };

        public DashboardViewModel(MediaElementService mediaElementService)
        {
            MediaElementService = mediaElementService;
            MediaElementService.SongChange += HandleSongChange;
        }

        private void HandleSongChange(object? sender, EventArgs e)
        {
            if(sender is SongImage song)
            {
                CurrentlyPlaying = song;
            }
        }

        [RelayCommand]
        public void Play()
        {
            MediaElementService.MediaElement?.Play();
        }
        [RelayCommand]
        public void Pause()
        {
            MediaElementService.MediaElement?.Pause();
        }
        [RelayCommand]
        public void Stop()
        {
            MediaElementService.MediaElement?.Stop();
        }
    }
}
