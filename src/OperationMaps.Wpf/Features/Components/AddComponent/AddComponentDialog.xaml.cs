using System.Windows;
using OperationMaps.Wpf.Shared.Controls;

namespace OperationMaps.Wpf.Features.Components.AddComponent
{
    public partial class AddComponentDialog : Window
    {
        public AddComponentDialog(AddComponentDialogViewModel viewModel)
        {
            InitializeComponent();
            WindowChromeFixes.Apply(this);
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
