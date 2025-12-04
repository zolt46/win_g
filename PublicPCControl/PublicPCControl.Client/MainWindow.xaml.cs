// File: PublicPCControl.Client/MainWindow.xaml.cs
using System;
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
            Loaded += (_, _) => ApplyScale();
        }

        private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.A &&
                System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control) &&
                System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            {
                e.Handled = true;
                TriggerAdmin();
            }
        }

        private void TriggerAdmin()
        {
            _viewModel.HandleAdminShortcut();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyScale();
        }

        private void ApplyScale()
        {
            const double baseWidth = 1600.0;
            const double baseHeight = 900.0;

            if (ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            var scale = Math.Min(ActualWidth / baseWidth, ActualHeight / baseHeight);
            RootScale.ScaleX = scale;
            RootScale.ScaleY = scale;
        }
    }
}