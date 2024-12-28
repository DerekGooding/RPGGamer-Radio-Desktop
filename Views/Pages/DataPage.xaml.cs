using RPGGamer_Radio_Desktop.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace RPGGamer_Radio_Desktop.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        public DataPage(DataViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
