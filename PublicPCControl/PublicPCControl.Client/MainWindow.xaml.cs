// File: PublicPCControl.Client/MainWindow.xaml.cs
using System.Windows;

namespace PublicPCControl.Client
{
    public partial class MainWindow : Window
    {
        private readonly ViewModels.MainViewModel _viewModel;

        public MainWindow(ViewModels.MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F12 &&
                System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control) &&
                System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
            {
                e.Handled = true;
                TriggerAdmin();
            }
        }

        private void TriggerAdmin()
        {
            _viewModel.HandleAdminShortcut();
        }
    }
}