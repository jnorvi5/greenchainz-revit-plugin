using System.Windows;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.UI
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        private void LoadUserInfo()
        {
            if (AuthService.Instance.IsLoggedIn)
            {
                UserEmailText.Text = AuthService.Instance.UserEmail;
                CreditsText.Text = AuthService.Instance.Credits.ToString();
                LogoutButton.Visibility = Visibility.Visible;
            }
            else
            {
                UserEmailText.Text = "Not logged in";
                CreditsText.Text = "-";
                LogoutButton.Visibility = Visibility.Collapsed;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AuthService.Instance.Logout();
            LoadUserInfo();
            // Optionally close or show login
            DialogResult = true; // Signal that logout happened
            Close();
        }
    }
}
