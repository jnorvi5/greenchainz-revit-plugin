using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.UI
{
    public partial class LeedEmbodiedCarbonWindow : Window
    {
        private readonly LeedEmbodiedCarbonResult _result;

        public LeedEmbodiedCarbonWindow(LeedEmbodiedCarbonResult result)
        {
            InitializeComponent();
            _result = result;
            DisplayResults();
        }

        private void DisplayResults()
        {
            if (_result == null) return;

            ProjectNameText.Text = $"{_result.ProjectName} | {_result.CalculationDate.ToShortDateString()} | Baseline: {_result.BaselineYear}";
            
            PointsText.Text = _result.PointsDisplay;
            ReductionText.Text = $"{_result.PercentReduction:F1}%";
            BaselineText.Text = $"{_result.bECIb:F2}";
            ActualText.Text = $"{_result.bECIa:F2}";

            // Set points badge color
            if (_result.PointsEarned == 2)
                PointsBadge.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            else if (_result.PointsEarned == 1)
                PointsBadge.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
            else
                PointsBadge.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red

            StatusText.Text = _result.CreditStatus;
            RecommendationText.Text = _result.GetRecommendation();

            MaterialsDataGrid.ItemsSource = _result.Materials;

            TotalBaselineText.Text = $"Total Baseline: {_result.TotalBaselineCarbon:N0} kgCO2e";
            TotalActualText.Text = $"Total Actual: {_result.TotalActualCarbon:N0} kgCO2e";
            BuildingAreaText.Text = $"Building Area: {_result.BuildingAreaSF:N0} sf";
        }

        private void FindEPDs_Click(object sender, RoutedEventArgs e)
        {
            // Open EC3 database
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://buildingtransparency.org/ec3",
                UseShellExecute = true
            });
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_result == null)
            {
                MessageBox.Show("No data to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|PDF Files (*.pdf)|*.pdf",
                DefaultExt = "csv",
                FileName = $"LEED_MRpc132_{_result.ProjectName}_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    if (saveDialog.FileName.EndsWith(".csv"))
                    {
                        ExportToCsv(saveDialog.FileName);
                    }
                    else
                    {
                        ExportToPdf(saveDialog.FileName);
                    }
                    
                    MessageBox.Show($"Report exported successfully!\n\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export:\n\n{ex.Message}", 
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToCsv(string path)
        {
            using (var writer = new System.IO.StreamWriter(path))
            {
                // Header info
                writer.WriteLine("LEED v4.1 MRpc132 - Embodied Carbon Report");
                writer.WriteLine($"Project:,{_result.ProjectName}");
                writer.WriteLine($"Date:,{_result.CalculationDate}");
                writer.WriteLine($"Baseline Year:,{_result.BaselineYear}");
                writer.WriteLine();
                
                // Summary
                writer.WriteLine("SUMMARY");
                writer.WriteLine($"Building Area (sf):,{_result.BuildingAreaSF:F0}");
                writer.WriteLine($"bECIb (Baseline):,{_result.bECIb:F2},kgCO2e/sf");
                writer.WriteLine($"bECIa (Actual):,{_result.bECIa:F2},kgCO2e/sf");
                writer.WriteLine($"Percent Reduction:,{_result.PercentReduction:F1}%");
                writer.WriteLine($"Points Earned:,{_result.PointsEarned}/2");
                writer.WriteLine($"Status:,{_result.CreditStatus}");
                writer.WriteLine();
                
                // Material breakdown
                writer.WriteLine("MATERIAL BREAKDOWN");
                writer.WriteLine("Category,Material,Quantity,Unit,mECIb,Baseline CO2 (kgCO2e),mECIa,Actual CO2 (kgCO2e),Reduction %,EPD Source");
                
                foreach (var mat in _result.Materials)
                {
                    writer.WriteLine($"{mat.MaterialCategory},{mat.MaterialName},{mat.Quantity:F2},{mat.Unit},{mat.BaselineECI:F2},{mat.BaselineCarbon:F0},{mat.ActualECI:F2},{mat.ActualCarbon:F0},{mat.PercentReduction:F1}%,{mat.EPDSource}");
                }
                
                writer.WriteLine();
                writer.WriteLine($"TOTAL,,,,{_result.TotalBaselineCarbon:F0},,{_result.TotalActualCarbon:F0}");
            }
        }

        private void ExportToPdf(string path)
        {
            // Use iText7 to generate PDF
            using (var writer = new iText.Kernel.Pdf.PdfWriter(path))
            using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
            using (var doc = new iText.Layout.Document(pdf))
            {
                doc.Add(new iText.Layout.Element.Paragraph("LEED v4.1 MRpc132 - Embodied Carbon Report")
                    .SetFontSize(20)
                    .SetBold());
                
                doc.Add(new iText.Layout.Element.Paragraph($"Project: {_result.ProjectName}")
                    .SetFontSize(14));
                doc.Add(new iText.Layout.Element.Paragraph($"Date: {_result.CalculationDate.ToShortDateString()} | Baseline: {_result.BaselineYear}")
                    .SetFontSize(12));
                
                doc.Add(new iText.Layout.Element.Paragraph("\n"));
                
                // Summary
                doc.Add(new iText.Layout.Element.Paragraph($"Points Earned: {_result.PointsEarned}/2")
                    .SetFontSize(24)
                    .SetBold());
                doc.Add(new iText.Layout.Element.Paragraph($"Carbon Reduction: {_result.PercentReduction:F1}%")
                    .SetFontSize(18));
                doc.Add(new iText.Layout.Element.Paragraph($"Status: {_result.CreditStatus}")
                    .SetFontSize(14));
                
                doc.Add(new iText.Layout.Element.Paragraph("\n"));
                doc.Add(new iText.Layout.Element.Paragraph($"bECIb (Baseline): {_result.bECIb:F2} kgCO2e/sf"));
                doc.Add(new iText.Layout.Element.Paragraph($"bECIa (Actual): {_result.bECIa:F2} kgCO2e/sf"));
                doc.Add(new iText.Layout.Element.Paragraph($"Building Area: {_result.BuildingAreaSF:N0} sf"));
                
                doc.Add(new iText.Layout.Element.Paragraph("\n"));
                doc.Add(new iText.Layout.Element.Paragraph("Material Breakdown").SetBold().SetFontSize(14));
                
                // Table
                var table = new iText.Layout.Element.Table(6).UseAllAvailableWidth();
                table.AddHeaderCell("Category");
                table.AddHeaderCell("Material");
                table.AddHeaderCell("Baseline CO2");
                table.AddHeaderCell("Actual CO2");
                table.AddHeaderCell("Reduction");
                table.AddHeaderCell("EPD");
                
                foreach (var mat in _result.Materials)
                {
                    table.AddCell(mat.MaterialCategory ?? "");
                    table.AddCell(mat.MaterialName ?? "");
                    table.AddCell($"{mat.BaselineCarbon:N0}");
                    table.AddCell($"{mat.ActualCarbon:N0}");
                    table.AddCell($"{mat.PercentReduction:F1}%");
                    table.AddCell(mat.HasEPD ? "Yes" : "No");
                }
                
                doc.Add(table);
                
                doc.Add(new iText.Layout.Element.Paragraph("\n"));
                doc.Add(new iText.Layout.Element.Paragraph("Generated by GreenChainz | UW Carbon Leadership Forum Methodology")
                    .SetFontSize(10)
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY));
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
