using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.UI
{
    public partial class AuditResultsWindow : Window
    {
        private readonly AuditResult _result;

        public AuditResultsWindow(AuditResult result)
        {
            InitializeComponent();
            _result = result;
            DataContext = result;
            DisplayResults(result);
        }

        // For XAML designer support
        public AuditResultsWindow()
        {
            InitializeComponent();
        }

        private void DisplayResults(AuditResult result)
        {
            if (result == null)
            {
                ScoreText.Text = "Error";
                return;
            }

            ScoreText.Text = result.OverallScore.ToString("N0") + " kgCO2e";
            
            if (result.Materials != null)
            {
                MaterialsDataGrid.ItemsSource = result.Materials;
            }

            if (result.Recommendations != null)
            {
                RecommendationsList.ItemsSource = result.Recommendations;
            }
        }

        private void SwapMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MaterialBreakdown item)
            {
                MessageBox.Show($"Swap requested for: {item.MaterialName}", "Swap Material", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SendRFQ_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sending RFQ for flagged materials...", "Send RFQ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
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
            if (value is double score)
            {
                if (score < 1000)
                    return Brushes.Green;
                else if (score < 10000)
                    return Brushes.Orange;
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
