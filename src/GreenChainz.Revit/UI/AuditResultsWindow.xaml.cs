using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;

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
                MessageBox.Show($"Swap requested for: {item.MaterialName}\n\nThis will open the Material Browser to find low-carbon alternatives.", 
                    "Swap Material", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SendRFQ_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CreateRFQDialog(new System.Collections.Generic.List<RFQItem>());
            
            // Pre-populate with flagged materials
            if (_result?.Materials != null)
            {
                foreach (var mat in _result.Materials)
                {
                    if (mat.TotalCarbon > 10000) // High carbon materials
                    {
                        dialog.MaterialsDataGrid.Items.Add(new RFQItem(mat.MaterialName, 1, "unit"));
                    }
                }
            }
            
            dialog.ShowDialog();
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_result == null)
            {
                MessageBox.Show("No audit data to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = $"CarbonAudit_{_result.ProjectName}_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var pdfService = new PdfExportService();
                    pdfService.GenerateAuditReport(_result, saveDialog.FileName);
                    
                    MessageBox.Show($"PDF exported successfully!\n\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Open the PDF
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
