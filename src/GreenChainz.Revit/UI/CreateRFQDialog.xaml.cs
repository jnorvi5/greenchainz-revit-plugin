using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.UI
{
    public partial class CreateRFQDialog : Window
    {
        private readonly RfqService _rfqService;
        private ObservableCollection<SupplierSelection> _suppliers;
        public RFQRequest RequestPayload { get; private set; }

        public CreateRFQDialog(List<RFQItem> initialMaterials)
        {
            InitializeComponent();
            _rfqService = new RfqService();
            _suppliers = new ObservableCollection<SupplierSelection>();
            
            MaterialsDataGrid.ItemsSource = initialMaterials;
            SuppliersDataGrid.ItemsSource = _suppliers;
            DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(14);

            // Auto-find suppliers on load if materials exist
            if (initialMaterials.Count > 0)
            {
                FindSuppliersAsync(initialMaterials);
            }
        }

        private async void FindSuppliersButton_Click(object sender, RoutedEventArgs e)
        {
            var materials = MaterialsDataGrid.ItemsSource as List<RFQItem>;
            if (materials == null || !materials.Any())
            {
                MessageBox.Show("No materials to search for suppliers.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await FindSuppliersAsync(materials);
        }

        private async Task FindSuppliersAsync(List<RFQItem> materials)
        {
            try
            {
                FindSuppliersButton.IsEnabled = false;
                StatusText.Text = "Searching for sustainable suppliers...";
                _suppliers.Clear();

                // Get unique categories from materials
                var categories = materials
                    .Select(m => GetMaterialCategory(m.MaterialName))
                    .Distinct()
                    .ToList();

                foreach (var category in categories)
                {
                    var suppliers = await _rfqService.GetSuppliersAsync(category);
                    foreach (var supplier in suppliers)
                    {
                        if (!_suppliers.Any(s => s.Id == supplier.Id))
                        {
                            _suppliers.Add(new SupplierSelection
                            {
                                Id = supplier.Id,
                                Name = supplier.Name,
                                SustainabilityScore = supplier.SustainabilityScore,
                                CertificationsDisplay = supplier.CertificationsDisplay,
                                ContactEmail = supplier.ContactEmail,
                                Website = supplier.Website,
                                IsSelected = true
                            });
                        }
                    }
                }

                StatusText.Text = $"Found {_suppliers.Count} sustainable suppliers for your materials.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error finding suppliers: {ex.Message}";
            }
            finally
            {
                FindSuppliersButton.IsEnabled = true;
            }
        }

        private string GetMaterialCategory(string materialName)
        {
            string name = materialName.ToLower();
            if (name.Contains("concrete")) return "concrete";
            if (name.Contains("steel") || name.Contains("metal")) return "steel";
            if (name.Contains("wood") || name.Contains("timber")) return "wood";
            if (name.Contains("glass") || name.Contains("glazing")) return "glass";
            if (name.Contains("insulation")) return "insulation";
            if (name.Contains("aluminum")) return "aluminum";
            if (name.Contains("gypsum") || name.Contains("drywall")) return "gypsum";
            return "general";
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
                {
                    MessageBox.Show("Project Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var materials = MaterialsDataGrid.ItemsSource as List<RFQItem>;
                if (materials == null || !materials.Any(m => !string.IsNullOrWhiteSpace(m.MaterialName)))
                {
                    MessageBox.Show("Please include at least one material.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedSuppliers = _suppliers.Where(s => s.IsSelected).ToList();
                if (!selectedSuppliers.Any())
                {
                    var result = MessageBox.Show("No suppliers selected. Submit RFQ anyway?", "Confirm", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }

                SubmitButton.IsEnabled = false;
                SubmitButton.Content = "Submitting...";
                StatusText.Text = "Submitting RFQ to suppliers...";

                // Create request
                var request = new RFQRequest
                {
                    ProjectName = ProjectNameTextBox.Text,
                    ProjectAddress = ProjectAddressTextBox.Text,
                    Materials = materials.Where(m => !string.IsNullOrWhiteSpace(m.MaterialName)).ToList(),
                    DeliveryDate = DeliveryDatePicker.SelectedDate ?? DateTime.Today.AddDays(14),
                    SpecialInstructions = SpecialInstructionsTextBox.Text,
                    SelectedSupplierIds = selectedSuppliers.Select(s => s.Id).ToList()
                };

                // Submit
                var response = await _rfqService.SubmitRfqAsync(request);

                if (response.Success)
                {
                    string supplierList = selectedSuppliers.Any() 
                        ? "\n\nSuppliers notified:\n" + string.Join("\n", selectedSuppliers.Select(s => $"• {s.Name} ({s.ContactEmail})"))
                        : "";

                    MessageBox.Show(
                        $"RFQ Submitted Successfully!\n\nRFQ ID: {response.RfqId}\n{response.Message}{supplierList}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    RequestPayload = request;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show($"RFQ Submission Issue:\n\n{response.Message}", "Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "Submit RFQ";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // Helper class for supplier selection
    public class SupplierSelection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SustainabilityScore { get; set; }
        public string CertificationsDisplay { get; set; }
        public string ContactEmail { get; set; }
        public string Website { get; set; }
        public bool IsSelected { get; set; }
    }
}
