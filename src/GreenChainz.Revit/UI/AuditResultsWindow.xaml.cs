using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.UI
{
    /// <summary>
    /// Improved carbon audit results window with per-element breakdown,
    /// collapsible category groups, sortable columns, and swap recommendations.
    /// </summary>
    public partial class AuditResultsWindow : Window
    {
        private readonly AuditResult _result;
        private List<AuditElement> _allElements;
        private List<CategoryGroup> _categoryGroups;
        private List<SwapRecommendationGroup> _swapGroups;
        private readonly List<SwapAlternative> _rfqSelections = new List<SwapAlternative>();

        /// <summary>
        /// Initializes the audit results window with scan results.
        /// </summary>
        public AuditResultsWindow(AuditResult result)
        {
            InitializeComponent();
            _result = result;
            _allElements = new List<AuditElement>();
            _categoryGroups = new List<CategoryGroup>();
            _swapGroups = new List<SwapRecommendationGroup>();

            DisplayResults(result);
        }

        /// <summary>For XAML designer support.</summary>
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

            ProjectNameText.Text = $"{result.ProjectName} - {result.Date:MMM dd, yyyy}";

            // Build per-element list from MaterialBreakdown data
            _allElements = BuildElementList(result);

            // Summary header
            ScoreText.Text = result.OverallScore.ToString("N0");
            ElementCountText.Text = _allElements.Count.ToString("N0");

            // Top categories
            var topCategories = _allElements
                .GroupBy(e => e.Category)
                .Select(g => new CategorySummary
                {
                    Category = g.Key,
                    TotalCarbon = g.Sum(e => e.CarbonImpact)
                })
                .OrderByDescending(c => c.TotalCarbon)
                .Take(3)
                .ToList();

            double maxCarbon = topCategories.FirstOrDefault()?.TotalCarbon ?? 1;
            foreach (var cat in topCategories)
            {
                cat.Percentage = (cat.TotalCarbon / maxCarbon) * 100;
            }
            TopCategoriesList.ItemsSource = topCategories;

            // Category groups with elements (default sort: carbon highest first)
            BuildCategoryGroups("carbon_desc");

            // Recommendations
            if (result.Recommendations != null)
            {
                RecommendationsList.ItemsSource = result.Recommendations;
            }

            // Load swap recommendations asynchronously
            LoadSwapRecommendationsAsync();
        }

        /// <summary>
        /// Converts MaterialBreakdown entries into per-element AuditElement entries.
        /// Since the existing audit aggregates by material, we create one entry per material
        /// with the aggregated data and element-level detail where available.
        /// </summary>
        private List<AuditElement> BuildElementList(AuditResult result)
        {
            var elements = new List<AuditElement>();

            if (result?.Materials == null) return elements;

            foreach (var mat in result.Materials)
            {
                string category = MapMaterialToCategory(mat);
                double volume = mat.VolumeM3 > 0 ? mat.VolumeM3 : ParseVolume(mat.Quantity);

                int elementId = 0;
                if (!string.IsNullOrEmpty(mat.RevitElementId))
                {
                    int.TryParse(mat.RevitElementId, out elementId);
                }

                elements.Add(new AuditElement
                {
                    ElementId = elementId,
                    ElementName = mat.MaterialName,
                    Category = category,
                    MaterialName = mat.MaterialName,
                    MaterialId = mat.Ec3Category ?? mat.MaterialName,
                    Volume = volume,
                    Area = 0,
                    CarbonImpact = mat.TotalCarbon,
                    CarbonPerUnit = mat.CarbonFactor,
                    HasAlternatives = mat.TotalCarbon > 100 // Flag high-carbon materials
                });
            }

            return elements;
        }

        private string MapMaterialToCategory(MaterialBreakdown mat)
        {
            if (!string.IsNullOrEmpty(mat.IfcCategory))
            {
                return mat.IfcCategory switch
                {
                    "IfcWall" => "Walls",
                    "IfcSlab" => "Floors",
                    "IfcRoof" => "Roofs",
                    "IfcColumn" => "Columns",
                    "IfcBeam" => "Structural Framing",
                    "IfcFooting" => "Foundations",
                    "IfcWindow" => "Windows",
                    "IfcDoor" => "Doors",
                    "IfcPlate" => "Curtain Wall",
                    "IfcMember" => "Curtain Wall",
                    "IfcStair" => "Stairs",
                    "IfcRailing" => "Railings",
                    "IfcCovering" => "Ceilings",
                    "IfcFurniture" => "Furniture",
                    _ => "Other"
                };
            }

            // Fallback: guess from material/EC3 category
            string cat = (mat.Ec3Category ?? mat.MaterialName ?? "").ToLower();
            if (cat.Contains("concrete") || cat.Contains("cement")) return "Walls";
            if (cat.Contains("steel") || cat.Contains("metal")) return "Structural Framing";
            if (cat.Contains("wood") || cat.Contains("timber")) return "Structural Framing";
            if (cat.Contains("glass") || cat.Contains("glazing")) return "Windows";
            if (cat.Contains("insulation")) return "Walls";
            if (cat.Contains("roof")) return "Roofs";
            return "Other";
        }

        private double ParseVolume(string quantity)
        {
            if (string.IsNullOrEmpty(quantity)) return 0;
            string numStr = quantity.Replace(" m3", "").Replace(" m\u00B3", "").Replace(",", "").Trim();
            if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                return val;
            return 0;
        }

        private void BuildCategoryGroups(string sortMode)
        {
            IEnumerable<AuditElement> sorted = sortMode switch
            {
                "carbon_asc" => _allElements.OrderBy(e => e.CarbonImpact),
                "category" => _allElements.OrderBy(e => e.Category).ThenByDescending(e => e.CarbonImpact),
                "name" => _allElements.OrderBy(e => e.MaterialName),
                "volume" => _allElements.OrderByDescending(e => e.Volume),
                _ => _allElements.OrderByDescending(e => e.CarbonImpact)
            };

            _categoryGroups = sorted
                .GroupBy(e => e.Category)
                .OrderByDescending(g => g.Sum(e => e.CarbonImpact))
                .Select(g => new CategoryGroup
                {
                    CategoryName = g.Key,
                    Elements = g.ToList(),
                    Subtotal = g.Sum(e => e.CarbonImpact)
                })
                .ToList();

            CategoryGroupsList.ItemsSource = _categoryGroups;
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox?.SelectedIndex == null) return;

            string mode = SortComboBox.SelectedIndex switch
            {
                0 => "carbon_desc",
                1 => "carbon_asc",
                2 => "category",
                3 => "name",
                4 => "volume",
                _ => "carbon_desc"
            };

            BuildCategoryGroups(mode);
        }

        private async void LoadSwapRecommendationsAsync()
        {
            SwapLoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var highCarbonMaterials = _allElements
                    .Where(e => e.HasAlternatives)
                    .GroupBy(e => e.MaterialId)
                    .Select(g => g.First())
                    .OrderByDescending(e => e.CarbonImpact)
                    .Take(10)
                    .ToList();

                _swapGroups = new List<SwapRecommendationGroup>();

                using (var apiClient = new ApiClient())
                {
                    foreach (var mat in highCarbonMaterials)
                    {
                        try
                        {
                            var alternatives = await Task.Run(() =>
                                apiClient.GetSwapAlternativesAsync(mat.MaterialId));

                            if (alternatives != null && alternatives.Count > 0)
                            {
                                _swapGroups.Add(new SwapRecommendationGroup
                                {
                                    MaterialName = mat.MaterialName,
                                    MaterialId = mat.MaterialId,
                                    Category = mat.Category,
                                    CurrentCarbonPerUnit = mat.CarbonPerUnit,
                                    CurrentTotalCarbon = mat.CarbonImpact,
                                    Alternatives = alternatives.Take(3).ToList()
                                });
                            }
                        }
                        catch
                        {
                            // Skip materials that fail to fetch alternatives
                        }
                    }
                }

                SwapRecommendationsList.ItemsSource = _swapGroups;
            }
            catch
            {
                // Fail silently - swap recommendations are optional
            }
            finally
            {
                SwapLoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void SwapElement_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is AuditElement auditElement)
            {
                // Switch to swap recommendations tab and expand the relevant material
                MainTabControl.SelectedIndex = 1;
            }
        }

        private void AddToRfq_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is SwapAlternative alt)
            {
                if (!_rfqSelections.Any(r => r.MaterialId == alt.MaterialId))
                {
                    alt.IsSelectedForRfq = true;
                    _rfqSelections.Add(alt);
                    MessageBox.Show($"Added {alt.MaterialName} to RFQ.\n\n{_rfqSelections.Count} material(s) selected for RFQ.",
                        "Added to RFQ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"{alt.MaterialName} is already in the RFQ.",
                        "Already Added", MessageBoxButton.OK, MessageBoxImage.Information);
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
            // Build RFQ materials from audit results + any swap selections
            var materialList = new List<RfqMaterialItem>();

            // Include high-carbon materials with swap alternatives from _swapGroups
            if (_swapGroups != null && _swapGroups.Count > 0)
            {
                foreach (var group in _swapGroups)
                {
                    var bestSwap = group.Alternatives?.FirstOrDefault(a => a.IsBestSwap)
                                   ?? group.Alternatives?.FirstOrDefault();

                    // Find corresponding element for volume
                    var element = _allElements.FirstOrDefault(e => e.MaterialId == group.MaterialId);
                    double quantity = element?.Volume ?? 1.0;

                    materialList.Add(new RfqMaterialItem
                    {
                        CurrentMaterialName = group.MaterialName,
                        SwapMaterialName = bestSwap?.MaterialName,
                        SwapMaterialId = bestSwap?.MaterialId,
                        Quantity = quantity,
                        Unit = "m\u00B3",
                        CurrentCarbonImpact = group.CurrentTotalCarbon,
                        PotentialSavings = bestSwap != null
                            ? group.CurrentTotalCarbon * (bestSwap.CarbonSavingsPercent / 100.0)
                            : 0,
                        IsSelected = true,
                        HasSwapAlternative = bestSwap != null
                    });
                }
            }

            // Fallback: use all materials if no swap groups
            if (materialList.Count == 0 && _result?.Materials != null)
            {
                foreach (var mat in _result.Materials)
                {
                    double qty = mat.VolumeM3 > 0 ? mat.VolumeM3 : ParseVolume(mat.Quantity);
                    if (qty <= 0) qty = 1.0;

                    materialList.Add(new RfqMaterialItem
                    {
                        CurrentMaterialName = mat.MaterialName,
                        Quantity = qty,
                        Unit = "m\u00B3",
                        CurrentCarbonImpact = mat.TotalCarbon,
                        IsSelected = true,
                        HasSwapAlternative = false
                    });
                }
            }

            if (materialList.Count == 0)
            {
                MessageBox.Show("No materials found to send RFQ.", "No Materials", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SmartRfqDialog(materialList, _result?.ProjectName);
            dialog.Show();
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

    #region View Models for Category Groups and Swap Recommendations

    /// <summary>Category summary for the top categories display.</summary>
    public class CategorySummary
    {
        public string Category { get; set; }
        public double TotalCarbon { get; set; }
        public double Percentage { get; set; }
        public string CarbonDisplay => $"{TotalCarbon:N0} kgCO\u2082e";
    }

    /// <summary>Collapsible category group with element details.</summary>
    public class CategoryGroup
    {
        public string CategoryName { get; set; }
        public List<AuditElement> Elements { get; set; }
        public double Subtotal { get; set; }
        public string SubtotalDisplay => $"{Subtotal:N0} kgCO\u2082e";
        public string ElementCountDisplay => $"{Elements?.Count ?? 0} element(s)";
    }

    /// <summary>Material with its swap alternatives for the recommendations panel.</summary>
    public class SwapRecommendationGroup
    {
        public string MaterialName { get; set; }
        public string MaterialId { get; set; }
        public string Category { get; set; }
        public double CurrentCarbonPerUnit { get; set; }
        public double CurrentTotalCarbon { get; set; }
        public List<SwapAlternative> Alternatives { get; set; }
        public string CategoryDisplay => Category;
        public string CurrentCarbonDisplay => $"{CurrentTotalCarbon:N0} kgCO\u2082e";
        public string AlternativeCountDisplay => $"{Alternatives?.Count ?? 0} alternative(s)";
    }

    /// <summary>Material item for the smart RFQ flow.</summary>
    public class RfqMaterialItem
    {
        public string CurrentMaterialName { get; set; }
        public string SwapMaterialName { get; set; }
        public string SwapMaterialId { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        public double CurrentCarbonImpact { get; set; }
        public double PotentialSavings { get; set; }
        public bool IsSelected { get; set; }
        public bool HasSwapAlternative { get; set; }

        public string QuantityDisplay => $"{Quantity:F2} {Unit}";
        public string CarbonImpactDisplay => $"{CurrentCarbonImpact:N0} kgCO\u2082e";
        public string SavingsDisplay => PotentialSavings > 0 ? $"-{PotentialSavings:N0} kgCO\u2082e" : "-";
    }

    #endregion

    #region Value Converters

    /// <summary>Converts carbon score to color brush.</summary>
    public class ScoreToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score)
            {
                if (score < 1000) return new SolidColorBrush(Color.FromRgb(0x2D, 0x6A, 0x4F));
                if (score < 10000) return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
                return new SolidColorBrush(Color.FromRgb(0xD6, 0x28, 0x28));
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Converts boolean to Visibility.</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Converts IsBestSwap to border thickness or background.</summary>
    public class BestSwapBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isBest = value is bool b && b;
            string param = parameter as string;

            if (param == "bg")
            {
                return isBest
                    ? new SolidColorBrush(Color.FromRgb(0xF0, 0xF7, 0xF4))
                    : new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
            }

            return isBest ? new Thickness(2) : new Thickness(1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Converts EPD verified status to color.</summary>
    public class EpdStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool verified = value is bool b && b;
            return verified
                ? new SolidColorBrush(Color.FromRgb(0x2D, 0x6A, 0x4F))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Converts cost delta to color (green for cheaper, red for more expensive).</summary>
    public class CostDeltaColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double delta)
            {
                if (delta < 0) return new SolidColorBrush(Color.FromRgb(0x2D, 0x6A, 0x4F));
                if (delta > 5) return new SolidColorBrush(Color.FromRgb(0xD6, 0x28, 0x28));
                return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
            }
            return new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Converts container width minus offset for expander headers to fill properly.</summary>
    public class WidthMinusConverter : IValueConverter
    {
        public static readonly WidthMinusConverter Instance = new WidthMinusConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return Math.Max(0, width - 40);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
