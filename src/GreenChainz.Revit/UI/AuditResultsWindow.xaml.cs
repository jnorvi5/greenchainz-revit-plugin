using System.Windows;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
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
            DataContext = result;
        }

        // For XAML designer support (optional)
        public AuditResultsWindow()
        {
            InitializeComponent();
        }

        private void SwapMaterial_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for Swap Material logic
            // In a real scenario, this would trigger an event or command to open a material browser
            // and update the AuditResult model.
            if (sender is FrameworkElement element && element.DataContext is MaterialAuditItem item)
            {
                MessageBox.Show($"Swap requested for: {item.Name}", "Swap Material", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SendRFQ_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for Send RFQ logic
            MessageBox.Show("Sending RFQ for flagged materials...", "Send RFQ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for Export PDF logic
            MessageBox.Show("Exporting results to PDF...", "Export PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ScoreToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int score)
            {
                if (score > 80)
                    return Brushes.Green;
                else if (score >= 50)
                    return Brushes.Orange; // Yellow is sometimes hard to read on white
                else
                    return Brushes.Red;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
