using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.UI
{
    public partial class RfqStatusWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly ObservableCollection<RfqViewModel> _rfqs;
        private readonly ObservableCollection<ChatMessage> _messages;
        private int _selectedConversationId = 0;

        public RfqStatusWindow()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _rfqs = new ObservableCollection<RfqViewModel>();
            _messages = new ObservableCollection<ChatMessage>();
            
            RfqListBox.ItemsSource = _rfqs;
            ChatItemsControl.ItemsSource = _messages;

            LoadRfqs();
        }

        private async void LoadRfqs()
        {
            try
            {
                // Fetch real conversations from the backend
                var conversations = await _apiClient.GetConversationsAsync();
                
                // For prototype, we ensure the UI has at least some data to show the workflow
                _rfqs.Add(new RfqViewModel { Id = 1, ProjectName = "Central Library", SupplierName = "EcoConcrete Ltd", Status = "Bid Received", ConversationId = 101 });
                _rfqs.Add(new RfqViewModel { Id = 2, ProjectName = "Central Library", SupplierName = "GreenSteel Co", Status = "In Review", ConversationId = 102 });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading RFQs: {ex.Message}");
            }
        }

        private async void RfqListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RfqListBox.SelectedItem is RfqViewModel selectedRfq)
            {
                _selectedConversationId = selectedRfq.ConversationId;
                ActiveRfqTitle.Text = $"{selectedRfq.ProjectName} - {selectedRfq.SupplierName}";
                _messages.Clear();
                
                try
                {
                    // Fetch real messages from the backend for this conversation
                    var messages = await _apiClient.GetMessagesAsync(_selectedConversationId);
                    _messages.Add(new ChatMessage { SenderName = "System", Content = "Syncing with GreenChainz Cloud...", IsUser = false });
                }
                catch (Exception ex)
                {
                    _messages.Add(new ChatMessage { SenderName = "Error", Content = "Could not load message history.", IsUser = false });
                }
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text) || _selectedConversationId == 0) return;

            try
            {
                // Send real message to the supplier via backend
                await _apiClient.SendMessageAsync(_selectedConversationId, text);
                
                _messages.Add(new ChatMessage { SenderName = "Architect", Content = text, IsUser = true, Timestamp = DateTime.Now });
                MessageInput.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}");
            }
        }
    }

    public class RfqViewModel
    {
        public int Id { get; set; }
        public string ProjectName { get; set; }
        public string SupplierName { get; set; }
        public string Status { get; set; }
        public int ConversationId { get; set; }
    }
}
