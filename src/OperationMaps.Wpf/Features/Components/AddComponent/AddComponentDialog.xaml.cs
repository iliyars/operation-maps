using System.Windows;

namespace OperationMaps.Wpf.Features.Components.AddComponent
{
    public partial class AddComponentDialog : Window
    {
        public AddComponentDialog(AddComponentDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AddComponentDialogViewModel.IsCompleted)
                    && viewModel.IsCompleted)
                {
                    DialogResult = true;
                    Close();
                }
            };

            Loaded += async (_, _) => await viewModel.InitializeAsync();
        }
    }
}
