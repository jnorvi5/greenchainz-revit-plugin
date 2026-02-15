using System;
using System.Threading.Tasks;
using Autodesk.Revit.UI;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Handles real-time notifications from the GreenChainz backend within Revit.
    /// </summary>
    public class NotificationService
    {
        private readonly ApiClient _apiClient;
        private bool _isPolling;

        public NotificationService()
        {
            _apiClient = new ApiClient();
        }

        public async void StartPolling()
        {
            if (_isPolling) return;
            _isPolling = true;

            while (_isPolling)
            {
                try
                {
                    // Poll for unread message counts or new notifications
                    // Maps to TRPC messaging.getUnreadCount
                    // In a production environment, this would use WebPubSub or WebSockets
                    await Task.Delay(30000); // Poll every 30 seconds
                }
                catch
                {
                    // Silently fail to not disrupt the architect's workflow
                }
            }
        }

        public void StopPolling()
        {
            _isPolling = false;
        }

        public static void ShowToast(string title, string message)
        {
            // Revit doesn't have a native toast, so we use a TaskDialog with low priority
            // or a custom WPF overlay in a real implementation.
            TaskDialog toast = new TaskDialog(title)
            {
                MainInstruction = message,
                CommonButtons = TaskDialogCommonButtons.Close,
                DefaultButton = TaskDialogResult.Close
            };
            toast.Show();
        }
    }
}
