// File: PublicPCControl.Client/Views/SessionView.xaml.cs
using System.Windows.Controls;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using PublicPCControl.Client.Services;

namespace PublicPCControl.Client.Views
{
    public partial class SessionView : UserControl
    {
        public SessionView()
        {
            try
            {
                InitializeComponent();
            }
            catch (XamlParseException ex)
            {
                ErrorReporter.Log("SessionView-XAML", ex);
                Content = new TextBlock
                {
                    Text = "세션 화면을 불러오는 중 오류가 발생했습니다. error.log를 확인해주세요.",
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new System.Windows.Thickness(20)
                };
            }
        }
    }
}