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
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = $"Version {version?.Major ?? 1}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
            
            // Check API status
            bool ec3Connected = App.Ec3Service?.HasValidApiKey == true;
            bool autodeskConnected = App.AuthService?.HasValidCredentials() == true;
            
            if (ec3Connected && autodeskConnected)
            {
                StatusText.Text = "Status: All APIs Connected ?";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else if (ec3Connected)
            {
                StatusText.Text = "Status: EC3 Connected, Autodesk Offline";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                StatusText.Text = "Status: Using Local Data (Demo Mode)";
                StatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void OpenEC3_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://buildingtransparency.org/ec3",
                UseShellExecute = true
            });
        }

        private void OpenUSGBC_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.usgbc.org/leed",
                UseShellExecute = true
            });
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
