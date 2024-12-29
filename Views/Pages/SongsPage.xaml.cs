using RPGGamer_Radio_Desktop.Services;
using RPGGamer_Radio_Desktop.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.Views.Pages;

public partial class SongsPage : INavigableView<SongsViewModel>
{
    public SongsViewModel ViewModel { get; }

    public SongsPage(SongsViewModel viewModel, MediaElementService mediaElementService)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();

        mediaElementService.MediaElement = MyPlayer;
    }
}
