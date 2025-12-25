using System.Reflection;
using System.Windows;

namespace GreenChainz.Revit.UI
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadInfo();
        }

        private void LoadInfo()
        {
            // Get version info from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            UserEmailText.Text = $"Version {version?.Major ?? 1}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
            CreditsText.Text = "Unlimited (Demo Mode)";
            LogoutButton.Visibility = Visibility.Collapsed;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
