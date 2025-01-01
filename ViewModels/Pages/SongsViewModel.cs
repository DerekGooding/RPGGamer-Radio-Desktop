using RPGGamer_Radio_Desktop.Models;
using RPGGamer_Radio_Desktop.Services;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.ViewModels.Pages
{
    public partial class SongsViewModel(MediaElementService mediaElementService) : ObservableObject, INavigationAware
    {
        private readonly MediaElementService _mediaElementService = mediaElementService;

        [ObservableProperty]
        private string _search = string.Empty;

        [ObservableProperty]
        private List<SongImage> _songImages = [];

        public void OnNavigatedTo() { SongImages = _mediaElementService.SongImages; }
        public void OnNavigatedFrom() { }

        [RelayCommand]
        public void PlayByButton(SongImage? songImage)
        {
            if (songImage is not SongImage s) return;
            _mediaElementService.PlayMedia(s);
        }

        [RelayCommand]
        public void PlayRandomSong() => _mediaElementService.PlayRandomSong();

        [RelayCommand]
        public void Pause() => _mediaElementService.Pause();
    }
}
