using RPGGamer_Radio_Desktop.Models;
using RPGGamer_Radio_Desktop.Services;

namespace RPGGamer_Radio_Desktop.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        public MediaElementService MediaElementService { get; }


        [ObservableProperty]
        private bool _isPlaying;

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
            MediaElementService.PlayStatusChange += HandlePlayStatusChange;
        }

        private void HandleSongChange(object? sender, EventArgs e)
        {
            if(sender is SongImage song)
            {
                CurrentlyPlaying = song;
            }
        }
        private void HandlePlayStatusChange(object? sender, EventArgs e)
        {
            if (sender is bool isPlaying)
            {
                IsPlaying = isPlaying;
            }
        }



        [RelayCommand]
        public void Pause() => MediaElementService.Pause();
        [RelayCommand]
        public void PlayRandomSong() => MediaElementService.PlayRandomSong();
        [RelayCommand]
        public void Previous()
        {

        }
    }
}
