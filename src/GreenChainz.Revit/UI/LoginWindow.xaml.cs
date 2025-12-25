using System.Windows;

namespace GreenChainz.Revit.UI
{
    public partial class LoginWindow : Window
    {
        public string UserEmail { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                StatusText.Text = "Please enter email and password.";
                return;
            }

            // For demo purposes, accept any login
            // In production, this would validate against an API
            UserEmail = email;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
