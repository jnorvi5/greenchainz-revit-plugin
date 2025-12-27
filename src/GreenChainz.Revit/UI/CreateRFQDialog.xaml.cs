using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private ObservableCollection<RFQItem> _materials;
        private ObservableCollection<SupplierSelection> _suppliers;
        public RFQRequest RequestPayload { get; private set; }

        public CreateRFQDialog(List<RFQItem> initialMaterials)
        {
            InitializeComponent();
            _rfqService = new RfqService();
            
            // Use ObservableCollection to avoid ItemsSource issues
            _materials = new ObservableCollection<RFQItem>(initialMaterials ?? new List<RFQItem>());
            _suppliers = new ObservableCollection<SupplierSelection>();
            
            MaterialsDataGrid.ItemsSource = _materials;
            SuppliersDataGrid.ItemsSource = _suppliers;
            DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(14);

            // Auto-find suppliers after dialog loads
            this.Loaded += async (s, e) =>
            {
                if (_materials.Count > 0)
                {
                    await FindSuppliersAsync();
                }
                else
                {
                    StatusText.Text = "Select elements in Revit first, or no materials found.";
                }
            };
        }

        private async void FindSuppliersButton_Click(object sender, RoutedEventArgs e)
        {
            await FindSuppliersAsync();
        }

        private async Task FindSuppliersAsync()
        {
            if (_materials.Count == 0)
            {
                StatusText.Text = "No materials to search for suppliers.";
                return;
            }

            try
            {
                FindSuppliersButton.IsEnabled = false;
                StatusText.Text = "Searching for sustainable suppliers...";
                _suppliers.Clear();

                // Get unique categories from materials
                var categories = _materials
                    .Select(m => GetMaterialCategory(m.MaterialName))
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList();

                foreach (var category in categories)
                {
                    try
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
                                    CertificationsDisplay = supplier.CertificationsDisplay ?? "",
                                    ContactEmail = supplier.ContactEmail ?? "",
                                    Website = supplier.Website ?? "",
                                    IsSelected = true
                                });
                            }
                        }
                    }
                    catch { /* Continue with other categories */ }
                }

                StatusText.Text = _suppliers.Count > 0 
                    ? $"Found {_suppliers.Count} sustainable suppliers for your materials."
                    : "No suppliers found. RFQ will be saved locally.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Using local suppliers. ({ex.Message})";
                LoadFallbackSuppliers();
            }
            finally
            {
                FindSuppliersButton.IsEnabled = true;
            }
        }

        private void LoadFallbackSuppliers()
        {
            _suppliers.Clear();
            _suppliers.Add(new SupplierSelection { Id = "carboncure", Name = "CarbonCure Technologies", SustainabilityScore = 96, CertificationsDisplay = "EPD, Carbon Negative", IsSelected = true });
            _suppliers.Add(new SupplierSelection { Id = "nucor", Name = "Nucor Corporation", SustainabilityScore = 91, CertificationsDisplay = "EPD, ISO 14001", IsSelected = true });
            _suppliers.Add(new SupplierSelection { Id = "structurlam", Name = "Structurlam", SustainabilityScore = 96, CertificationsDisplay = "FSC, EPD", IsSelected = true });
            _suppliers.Add(new SupplierSelection { Id = "rockwool", Name = "Rockwool", SustainabilityScore = 92, CertificationsDisplay = "EPD, GREENGUARD", IsSelected = true });
        }

        private string GetMaterialCategory(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return "";
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
                if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
                {
                    MessageBox.Show("Project Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_materials.Count == 0)
                {
                    MessageBox.Show("Please include at least one material.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedSuppliers = _suppliers.Where(s => s.IsSelected).ToList();

                SubmitButton.IsEnabled = false;
                SubmitButton.Content = "Submitting...";
                StatusText.Text = "Submitting RFQ...";

                var request = new RFQRequest
                {
                    ProjectName = ProjectNameTextBox.Text,
                    ProjectAddress = ProjectAddressTextBox.Text ?? "",
                    Materials = _materials.ToList(),
                    DeliveryDate = DeliveryDatePicker.SelectedDate ?? DateTime.Today.AddDays(14),
                    SpecialInstructions = SpecialInstructionsTextBox.Text ?? "",
                    SelectedSupplierIds = selectedSuppliers.Select(s => s.Id).ToList()
                };

                var response = await _rfqService.SubmitRfqAsync(request);

                string supplierList = selectedSuppliers.Any() 
                    ? "\n\nSuppliers:\n" + string.Join("\n", selectedSuppliers.Take(5).Select(s => $"  • {s.Name}"))
                    : "";

                MessageBox.Show(
                    $"RFQ Submitted!\n\nID: {response.RfqId}\n{response.Message}{supplierList}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                RequestPayload = request;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Submission failed.";
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

    public class SupplierSelection : INotifyPropertyChanged
    {
        private bool _isSelected;
        
        public string Id { get; set; }
        public string Name { get; set; }
        public int SustainabilityScore { get; set; }
        public string CertificationsDisplay { get; set; }
        public string ContactEmail { get; set; }
        public string Website { get; set; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
