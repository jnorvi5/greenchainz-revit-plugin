using System.Windows;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.UI
{
    public partial class AuditResultsWindow : Window
    {
        public AuditResultsWindow(AuditResult result)
        {
            InitializeComponent();
            DisplayResults(result);
        }

        private void DisplayResults(AuditResult result)
        {
            if (result == null)
            {
                ScoreText.Text = "Error";
                RatingText.Text = "N/A";
                return;
            }

            ScoreText.Text = result.CarbonScore.ToString("F2");
            RatingText.Text = result.Rating;

            if (result.Recommendations != null)
            {
                RecommendationsList.ItemsSource = result.Recommendations;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
