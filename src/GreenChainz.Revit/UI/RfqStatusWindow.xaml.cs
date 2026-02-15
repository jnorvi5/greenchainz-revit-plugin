using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.UI
{
    public partial class RfqStatusWindow : Window
    {
        private readonly ApiClient _apiClient;
        private dynamic _selectedRfq;

        public RfqStatusWindow()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            LoadRfqs();
        }

        private async void LoadRfqs()
        {
            try
            {
                // In a real implementation, this would call the API
                // For the prototype, we show the workflow
                var rfqs = await _apiClient.GetConversationsAsync();
                RfqListBox.ItemsSource = rfqs as IEnumerable<object>;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load RFQs: {ex.Message}");
            }
        }

        private void RfqListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRfq = RfqListBox.SelectedItem;
            if (_selectedRfq != null)
            {
                ActiveRfqTitle.Text = _selectedRfq.ProjectName;
                LoadMessages(_selectedRfq.Id);
            }
        }

        private async void LoadMessages(int rfqId)
        {
            // Implementation for loading messages from ApiClient
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRfq == null || string.IsNullOrWhiteSpace(MessageInput.Text)) return;

            try
            {
                await _apiClient.SendMessageAsync(_selectedRfq.ConversationId, MessageInput.Text);
                MessageInput.Clear();
                LoadMessages(_selectedRfq.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}");
            }
        }
    }
}
