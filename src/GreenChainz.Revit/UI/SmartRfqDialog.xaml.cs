using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;
using Newtonsoft.Json;

namespace GreenChainz.Revit.UI
{
    /// <summary>
    /// Smart RFQ dialog pre-populated from carbon audit results.
    /// Shows high-carbon materials with swap alternatives, quantities from model,
    /// and carbon impact/savings per material.
    /// </summary>
    public partial class SmartRfqDialog : Window
    {
        private readonly ObservableCollection<RfqMaterialItem> _materials;

        /// <summary>
        /// Creates the smart RFQ dialog with pre-populated materials from audit.
        /// </summary>
        public SmartRfqDialog(List<RfqMaterialItem> materials, string projectName = null)
        {
            InitializeComponent();

            _materials = new ObservableCollection<RfqMaterialItem>(materials ?? new List<RfqMaterialItem>());
            MaterialsGrid.ItemsSource = _materials;

            ProjectNameBox.Text = projectName ?? "";
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var selected = _materials.Where(m => m.IsSelected).ToList();
            double totalCarbon = selected.Sum(m => m.CurrentCarbonImpact);
            double totalSavings = selected.Sum(m => m.PotentialSavings);

            TotalCarbonText.Text = $"{totalCarbon:N0} kgCO\u2082e";
            TotalSavingsText.Text = totalSavings > 0 ? $"-{totalSavings:N0} kgCO\u2082e" : "N/A";
            MaterialCountText.Text = $"{selected.Count} selected";
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var m in _materials) m.IsSelected = true;
            MaterialsGrid.Items.Refresh();
            UpdateSummary();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var m in _materials) m.IsSelected = false;
            MaterialsGrid.Items.Refresh();
            UpdateSummary();
        }

        private async void SubmitRfq_Click(object sender, RoutedEventArgs e)
        {
            var selectedMaterials = _materials.Where(m => m.IsSelected).ToList();

            if (selectedMaterials.Count == 0)
            {
                MessageBox.Show("Please select at least one material.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ProjectNameBox.Text))
            {
                MessageBox.Show("Project name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SubmitButton.IsEnabled = false;
            SubmitButton.Content = "Submitting...";
            StatusText.Text = "Submitting RFQ to GreenChainz...";

            try
            {
                var request = new RfqSubmitRequest
                {
                    ProjectName = ProjectNameBox.Text,
                    ProjectLocation = ProjectLocationBox.Text ?? "",
                    ArchitectName = ArchitectNameBox.Text ?? "",
                    LineItems = selectedMaterials.Select(m => new RfqLineItem
                    {
                        MaterialId = m.SwapMaterialId ?? "",
                        MaterialName = m.SwapMaterialName ?? m.CurrentMaterialName,
                        CurrentMaterialName = m.CurrentMaterialName,
                        Quantity = m.Quantity,
                        Unit = m.Unit,
                        CurrentCarbonImpact = m.CurrentCarbonImpact,
                        EstimatedCarbonSavings = m.PotentialSavings,
                        Specifications = JsonConvert.SerializeObject(new { unit = m.Unit, quantity = m.Quantity })
                    }).ToList()
                };

                using (var apiClient = new ApiClient())
                {
                    string response = await Task.Run(() =>
                        apiClient.SubmitRfqWithLineItemsAsync(request));

                    MessageBox.Show(
                        $"RFQ submitted successfully!\n\n{selectedMaterials.Count} material(s) included.\nEstimated carbon savings: {selectedMaterials.Sum(m => m.PotentialSavings):N0} kgCO\u2082e",
                        "RFQ Submitted", MessageBoxButton.OK, MessageBoxImage.Information);

                    Close();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Submission failed: {ex.Message}";

                // Offer to save locally
                var result = MessageBox.Show(
                    $"Failed to submit RFQ online.\n\n{ex.Message}\n\nSave RFQ locally instead?",
                    "Submission Failed", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    SaveRfqLocally(selectedMaterials);
                }
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "Submit RFQ";
            }
        }

        private void SaveRfqLocally(List<RfqMaterialItem> materials)
        {
            try
            {
                string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string rfqDir = System.IO.Path.Combine(docsPath, "GreenChainz", "RFQs");
                System.IO.Directory.CreateDirectory(rfqDir);

                string fileName = $"RFQ-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                string filePath = System.IO.Path.Combine(rfqDir, fileName);

                var data = new
                {
                    projectName = ProjectNameBox.Text,
                    projectLocation = ProjectLocationBox.Text ?? "",
                    architectName = ArchitectNameBox.Text ?? "",
                    createdAt = DateTime.Now,
                    status = "pending_sync",
                    materials = materials.Select(m => new
                    {
                        currentMaterial = m.CurrentMaterialName,
                        swapMaterial = m.SwapMaterialName,
                        quantity = m.Quantity,
                        unit = m.Unit,
                        carbonImpact = m.CurrentCarbonImpact,
                        savings = m.PotentialSavings
                    })
                };

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                System.IO.File.WriteAllText(filePath, json);

                MessageBox.Show($"RFQ saved locally.\n\n{filePath}\n\nIt will be synced when connectivity is restored.",
                    "Saved Locally", MessageBoxButton.OK, MessageBoxImage.Information);

                StatusText.Text = $"Saved to {filePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save locally: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
