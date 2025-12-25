using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.UI
{
    public partial class LeedResultsWindow : Window
    {
        private readonly LeedResult _result;

        public LeedResultsWindow(LeedResult result)
        {
            InitializeComponent();
            _result = result;
            DisplayResults();
        }

        private void DisplayResults()
        {
            if (_result == null) return;

            ProjectNameText.Text = _result.ProjectName;
            CertLevelText.Text = _result.CertificationLevel.ToUpper();
            PointsText.Text = $"{_result.TotalPoints}/{_result.MaxPossiblePoints}";
            PercentText.Text = $"{_result.PercentageScore:F0}%";

            // Set badge color based on certification level
            switch (_result.CertificationLevel)
            {
                case "Platinum":
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(229, 228, 226)); // Platinum silver
                    CertLevelText.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case "Gold":
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Gold
                    CertLevelText.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case "Silver":
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(192, 192, 192)); // Silver
                    CertLevelText.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case "Certified":
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    break;
                default:
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                    break;
            }

            CreditsDataGrid.ItemsSource = _result.Credits;
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_result == null)
            {
                MessageBox.Show("No LEED data to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = $"LEED_Report_{_result.ProjectName}_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var pdfService = new LeedPdfExportService();
                    pdfService.GenerateLeedReport(_result, saveDialog.FileName);
                    
                    MessageBox.Show($"LEED Report exported successfully!\n\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export PDF:\n\n{ex.Message}", 
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImproveScore_Click(object sender, RoutedEventArgs e)
        {
            string recommendations = "To improve your LEED score:\n\n" +
                "• Use more recycled content materials (target 20%+)\n" +
                "• Source materials regionally within 500 miles\n" +
                "• Specify low-VOC paints, adhesives, and sealants\n" +
                "• Include rapidly renewable materials like bamboo or cork\n" +
                "• Use FSC-certified wood products\n" +
                "• Consider reclaimed/salvaged materials\n\n" +
                "Click 'Browse Materials' in GreenChainz to find certified options!";

            MessageBox.Show(recommendations, "LEED Score Improvement Tips", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
