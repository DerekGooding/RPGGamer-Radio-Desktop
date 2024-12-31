using RPGGamer_Radio_Desktop.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.Views.Pages;

public partial class SongsPage : INavigableView<SongsViewModel>
{
    public SongsViewModel ViewModel { get; }

    public SongsPage(SongsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
