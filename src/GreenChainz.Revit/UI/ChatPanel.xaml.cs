using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.UI
{
    public partial class ChatPanel : UserControl
    {
        private readonly ApiClient _apiClient;
        private readonly ObservableCollection<ChatMessage> _messages;
        private string _currentContext = "";
        private int _activeConversationId = 0; // Should be initialized from user session or specific RFQ

        public ChatPanel()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _messages = new ObservableCollection<ChatMessage>();
            ChatItemsControl.ItemsSource = _messages;

            // Initial greeting from Otto
            _messages.Add(new ChatMessage 
            { 
                SenderName = "Otto", 
                Content = "Hi! I'm Otto. I can help you find sustainable materials or manage your RFQs. Select an element in Revit to give me more context!", 
                IsUser = false 
            });
        }

        public void UpdateContext(string materialName, string category)
        {
            _currentContext = $"Selected: {materialName} ({category})";
            ContextText.Text = _currentContext;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Add user message
            _messages.Add(new ChatMessage { SenderName = "Architect", Content = text, IsUser = true });
            MessageInput.Clear();
            ChatScrollViewer.ScrollToBottom();

            try
            {
                // Call the Agent API with the new sendWithAgent endpoint
                // This ensures Otto/ChainBot processes the message with BIM context
                var response = await _apiClient.SendWithAgentAsync(_activeConversationId, text, _currentContext);
                
                // Note: Real implementation would parse the TRPC response object
                _messages.Add(new ChatMessage 
                { 
                    SenderName = "ChainBot", 
                    Content = "Processing your request with Otto and ChainBot...", 
                    IsUser = false 
                });
            }
            catch (Exception ex)
            {
                _messages.Add(new ChatMessage { SenderName = "System", Content = $"Connection Error: {ex.Message}. Check your GreenChainz login.", IsUser = false });
            }
            
            ChatScrollViewer.ScrollToBottom();
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                SendButton_Click(this, null);
            }
        }
    }

    public class ChatMessage
    {
        public string SenderName { get; set; }
        public string Content { get; set; }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
