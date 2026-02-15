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
                // Call the Agent API from the main app
                // This targets the 'material' or 'rfq' agents based on context
                var response = await _apiClient.SendMessageAsync(0, $"{text} [Context: {_currentContext}]");
                
                _messages.Add(new ChatMessage 
                { 
                    SenderName = "ChainBot", 
                    Content = response?.ToString() ?? "I'm here to help, but I couldn't reach the brain right now.", 
                    IsUser = false 
                });
            }
            catch (Exception ex)
            {
                _messages.Add(new ChatMessage { SenderName = "System", Content = $"Error: {ex.Message}", IsUser = false });
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
