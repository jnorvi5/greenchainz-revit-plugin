using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
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

        private async void SwapMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MaterialBreakdown item)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    using (var apiClient = new ApiClient())
                    {
                        double volume = 1.0;
                        if (!string.IsNullOrEmpty(item.Quantity))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(item.Quantity, @"[\d.]+");
                            if (match.Success) double.TryParse(match.Value, out volume);
                        }

                        var comparison = await apiClient.GetCarbonComparisonAsync(
                            item.MaterialName, 
                            "94105",
                            volume,
                            item.CarbonFactor
                        );

                        Mouse.OverrideCursor = null;

                        if (comparison != null && comparison.BestAlternative != null)
                        {
                            comparison.Original = new MaterialComparison
                            {
                                MaterialName = item.MaterialName,
                                SupplierName = "Current Specification",
                                GwpValue = item.TotalCarbon,
                                HasEpd = item.DataSource?.Contains("EC3") == true,
                                LeedImpact = "Baseline"
                            };

                            var comparisonWindow = new MaterialComparisonWindow(comparison);
                            comparisonWindow.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show($"No lower-carbon alternatives found for {item.MaterialName}.\n\nTry browsing the EC3 database directly.", 
                                "No Alternatives", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Mouse.OverrideCursor = null;
                    MessageBox.Show($"Error finding alternatives: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportIFC_Click(object sender, RoutedEventArgs e)
        {
            if (_result == null)
            {
                MessageBox.Show("No audit data to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "IFC Files (*.ifc)|*.ifc|IFC JSON (*.json)|*.json",
                DefaultExt = "ifc",
                FileName = $"CarbonAudit_{_result.ProjectName}_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var ifcService = new IfcExportService();
                    
                    if (saveDialog.FileName.EndsWith(".json"))
                    {
                        ifcService.SaveIfcMapping(_result, saveDialog.FileName);
                    }
                    else
                    {
                        ifcService.SaveIfcSpf(_result, saveDialog.FileName);
                    }
                    
                    MessageBox.Show($"IFC exported successfully!\n\n{saveDialog.FileName}\n\nThis file contains:\n- Pset_EnvironmentalImpactIndicators\n- IFC GUIDs for all materials\n- LCA Stage A1-A3 data", 
                        "IFC Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Open folder
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = Path.GetDirectoryName(saveDialog.FileName),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export IFC:\n\n{ex.Message}", 
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportBCF_Click(object sender, RoutedEventArgs e)
        {
            if (_result == null)
            {
                MessageBox.Show("No audit data to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "BCF Files (*.bcf)|*.bcf|BCF XML (*.bcfzip)|*.bcfzip",
                DefaultExt = "bcf",
                FileName = $"HighCarbon_Issues_{_result.ProjectName}_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var bcfService = new BcfExportService();
                    bcfService.ExportHighCarbonIssues(_result, saveDialog.FileName);
                    
                    int issueCount = 0;
                    foreach (var mat in _result.Materials)
                    {
                        if (mat.TotalCarbon > 5000) issueCount++;
                    }

                    MessageBox.Show($"BCF exported successfully!\n\n{saveDialog.FileName}\n\n{issueCount} high-carbon issues flagged.\n\nOpen this file in any BCF-compatible viewer\n(BIMcollab, Solibri, Navisworks, etc.)", 
                        "BCF Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export BCF:\n\n{ex.Message}", 
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SendRFQ_Click(object sender, RoutedEventArgs e)
        {
            var materialList = new System.Collections.Generic.List<RFQItem>();
            
            if (_result?.Materials != null)
            {
                foreach (var mat in _result.Materials)
                {
                    double qty = 1.0;
                    if (!string.IsNullOrEmpty(mat.Quantity))
                    {
                        string qtyStr = mat.Quantity.Replace(",", "").Trim();
                        var match = System.Text.RegularExpressions.Regex.Match(qtyStr, @"[\d.]+");
                        if (match.Success)
                        {
                            double.TryParse(match.Value, out qty);
                        }
                    }
                    if (qty <= 0) qty = 1.0;
                    
                    materialList.Add(new RFQItem(mat.MaterialName, qty, "unit"));
                }
            }

            if (materialList.Count == 0)
            {
                MessageBox.Show("No materials found to send RFQ.", "No Materials", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new CreateRFQDialog(materialList);
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
