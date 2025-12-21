using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.UI
{
    public partial class CreateRFQDialog : Window
    {
        private readonly ApiClient _apiClient;
        public RFQRequest RequestPayload { get; private set; }

        public CreateRFQDialog(List<RFQItem> initialMaterials)
        {
            InitializeComponent();
            _apiClient = new ApiClient();

            // Populate materials
            MaterialsDataGrid.ItemsSource = initialMaterials;

            // Set default date to today + 7 days
            DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(7);
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(ProjectNameTextBox.Text))
                {
                    MessageBox.Show("Project Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ProjectAddressTextBox.Text))
                {
                    MessageBox.Show("Project Address is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!DeliveryDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Delivery Date is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var materials = MaterialsDataGrid.ItemsSource as List<RFQItem>;
                if (materials == null || !materials.Any())
                {
                     // If the user added items via the DataGrid empty row, they might not be in the bound list if it wasn't ObservableCollection.
                     // But List<T> works if initialized.
                     // However, DataGrid editing usually requires ObservableCollection for updates to reflect automatically or just reading ItemsSource back.
                     // Let's rely on what was passed or check Items.
                     // A better way is to use ObservableCollection.
                }

                // Collect data
                var request = new RFQRequest
                {
                    ProjectName = ProjectNameTextBox.Text,
                    ProjectAddress = ProjectAddressTextBox.Text,
                    Materials = materials ?? new List<RFQItem>(),
                    DeliveryDate = DeliveryDatePicker.SelectedDate.Value,
                    SpecialInstructions = SpecialInstructionsTextBox.Text
                };

                // Filter out empty materials if any (e.g. user added a row but didn't fill it)
                request.Materials = request.Materials.Where(m => !string.IsNullOrWhiteSpace(m.MaterialName)).ToList();

                if (request.Materials.Count == 0)
                {
                    MessageBox.Show("Please include at least one material.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SubmitButton.IsEnabled = false;
                SubmitButton.Content = "Submitting...";

                // Submit to API
                string response = await _apiClient.SubmitRFQ(request);

                // Success
                MessageBox.Show($"RFQ Submitted Successfully! Response: {response}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                RequestPayload = request;
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to submit RFQ: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "Submit RFQ";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
