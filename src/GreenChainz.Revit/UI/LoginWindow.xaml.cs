using System;
using System.Windows;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.UI
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                StatusText.Text = "Please enter email and password.";
                return;
            }

            LoginButton.IsEnabled = false;
            StatusText.Text = "Logging in...";

            bool success = await AuthService.Instance.LoginAsync(email, password);

            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                StatusText.Text = "Login failed. Please check your credentials.";
                LoginButton.IsEnabled = true;
            }
        }
    }
}
