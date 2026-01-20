using System;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.UI
{
    public partial class MaterialComparisonWindow : Window
    {
        private readonly AuditResponse _comparison;
        private readonly Document _doc;
        private readonly Element _element;
        private readonly ElementId _materialId;
        
        public bool SwapConfirmed { get; private set; }
        public MaterialComparison SelectedAlternative { get; private set; }

        public MaterialComparisonWindow(AuditResponse comparison, Document doc = null, Element element = null, ElementId materialId = null)
        {
            InitializeComponent();
            _comparison = comparison;
            _doc = doc;
            _element = element;
            _materialId = materialId;

            // Disable swap button if no Revit context
            if (_doc == null || _element == null)
            {
                SwapButton.Content = "Close";
                SwapButton.Click -= SwapMaterial_Click;
                SwapButton.Click += (s, e) => Close();
            }

            DisplayComparison();
        }

        private void DisplayComparison()
        {
            if (_comparison == null) return;

            // Original Material
            if (_comparison.Original != null)
            {
                OriginalMaterialName.Text = _comparison.Original.MaterialName ?? "Current Material";
                OriginalSupplier.Text = _comparison.Original.SupplierName ?? "Current Specification";
                OriginalGwp.Text = _comparison.Original.GwpDisplay;
                OriginalEpdStatus.Text = _comparison.Original.HasEpd ? "Available" : "Not Available";
                OriginalEpdStatus.Foreground = _comparison.Original.HasEpd 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Orange;
                OriginalLeedImpact.Text = _comparison.Original.LeedImpact ?? "Baseline";
            }

            // Recommended Alternative
            if (_comparison.BestAlternative != null)
            {
                AltMaterialName.Text = _comparison.BestAlternative.MaterialName ?? "Low-Carbon Alternative";
                AltSupplier.Text = _comparison.BestAlternative.SupplierName ?? "GreenChainz Verified";
                AltGwp.Text = _comparison.BestAlternative.GwpDisplay;
                AltEpdStatus.Text = _comparison.BestAlternative.HasEpd ? "Available" : "Not Available";
                AltEpdStatus.Foreground = _comparison.BestAlternative.HasEpd 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Orange;
                AltLeedImpact.Text = _comparison.BestAlternative.LeedImpact ?? "Improved";
                AltCertifications.Text = _comparison.BestAlternative.CertificationsDisplay;
                AltDistance.Text = _comparison.BestAlternative.DistanceDisplay;

                // Savings
                SavingsText.Text = $"{_comparison.SavingsPercent:F0}% Less Carbon";
            }

            // Total Savings Summary
            TotalSavingsText.Text = $"{_comparison.TotalSavings:N0} kgCO2e";
            
            // Calculate equivalent (roughly 4.6 metric tons CO2 per car per year)
            double carsEquivalent = _comparison.TotalSavings / 4600;
            if (carsEquivalent >= 1)
            {
                EquivalentText.Text = $"{carsEquivalent:F0} car{(carsEquivalent >= 2 ? "s" : "")} off road for 1 year";
            }
            else
            {
                double treesEquivalent = _comparison.TotalSavings / 21; // ~21 kg CO2 per tree per year
                EquivalentText.Text = $"{treesEquivalent:F0} trees planted";
            }

            // Cost impact (usually similar or slight premium for low-carbon)
            CostImpactText.Text = _comparison.SavingsPercent > 20 ? "Slight Premium (~5%)" : "Similar Cost";

            // Data source
            DataSourceText.Text = $"Data: {_comparison.DataSource ?? "GreenChainz Database"}";
        }

        private void SwapMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (_doc == null || _element == null)
            {
                MessageBox.Show("No Revit element context available for swap.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (Transaction trans = new Transaction(_doc, "GreenChainz Material Swap"))
                {
                    trans.Start();

                    // Option 1: Update shared parameter if it exists
                    bool paramUpdated = false;
                    var gwpParam = _element.LookupParameter("EC_GWP");
                    if (gwpParam != null && !gwpParam.IsReadOnly)
                    {
                        gwpParam.Set(_comparison.BestAlternative.GwpValue);
                        paramUpdated = true;
                    }

                    // Option 2: Rename the material to indicate it's been swapped
                    if (_materialId != null)
                    {
                        var material = _doc.GetElement(_materialId) as Autodesk.Revit.DB.Material;
                        if (material != null)
                        {
                            string newName = material.Name;
                            if (!newName.Contains("(GreenChainz)"))
                            {
                                newName = $"{material.Name} (GreenChainz)";
                                try
                                {
                                    material.Name = newName;
                                }
                                catch
                                {
                                    // Material name might be locked
                                }
                            }
                        }
                    }

                    trans.Commit();

                    SwapConfirmed = true;
                    SelectedAlternative = _comparison.BestAlternative;

                    string message = paramUpdated 
                        ? $"Material swapped!\n\nNew GWP: {_comparison.BestAlternative.GwpDisplay}\nCarbon Saved: {_comparison.TotalSavings:N0} kgCO2e"
                        : $"Material marked as swapped!\n\nRecommended: {_comparison.BestAlternative.MaterialName}\nFrom: {_comparison.BestAlternative.SupplierName}\n\nNote: Update your specification to match.";

                    MessageBox.Show(message, "Swap Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Swap failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewInEC3_Click(object sender, RoutedEventArgs e)
        {
            string category = GetCategoryForEC3(_comparison.Original?.MaterialName ?? "");
            string url = $"https://buildingtransparency.org/ec3/material-search?category={category}";
            
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private string GetCategoryForEC3(string materialName)
        {
            string lower = materialName.ToLower();
            if (lower.Contains("concrete")) return "Concrete";
            if (lower.Contains("steel")) return "Steel";
            if (lower.Contains("wood") || lower.Contains("timber")) return "Wood";
            if (lower.Contains("glass")) return "Glass";
            if (lower.Contains("aluminum")) return "Aluminum";
            if (lower.Contains("insulation")) return "Insulation";
            return "All";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
