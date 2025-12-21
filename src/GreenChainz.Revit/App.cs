using System;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit
{
    /// <summary>
    /// Main application entry point for the GreenChainz Revit plugin.
    /// Implements IExternalApplication to register the plugin with Revit.
    /// </summary>
    public class App : IExternalApplication
    {
        /// <summary>
        /// Called when Revit starts up. Creates the GreenChainz ribbon tab and buttons.
        /// </summary>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create ribbon tab
                const string tabName = "GreenChainz";
                application.CreateRibbonTab(tabName);

                // Create ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Sustainable Materials");

                // Get the assembly path
                string assemblyPath = typeof(App).Assembly.Location;

                // Create Browse Materials button
                PushButtonData browseMaterialsButtonData = new PushButtonData(
                    "BrowseMaterials",
                    "Browse Materials",
                    assemblyPath,
                    "GreenChainz.Revit.Commands.BrowseMaterialsCommand")
                {
                    ToolTip = "Browse sustainable materials from GreenChainz marketplace",
                    LongDescription = "Open the GreenChainz materials browser to search and filter " +
                                     "sustainable building materials. View detailed specifications, " +
                                     "environmental certifications, and pricing information."
                };
                panel.AddItem(browseMaterialsButtonData);

                // Create Carbon Audit button
                PushButtonData carbonAuditButtonData = new PushButtonData(
                    "CarbonAudit",
                    "Carbon Audit",
                    assemblyPath,
                    "GreenChainz.Revit.Commands.CarbonAuditCommand")
                {
                    ToolTip = "Analyze the carbon footprint of your Revit model",
                    LongDescription = "Calculate the embodied carbon of materials in your current model. " +
                                     "Get detailed reports on carbon emissions by material category and " +
                                     "identify opportunities to reduce environmental impact."
                };
                panel.AddItem(carbonAuditButtonData);

                // Create Send RFQ button
                PushButtonData sendRfqButtonData = new PushButtonData(
                    "SendRFQ",
                    "Send RFQ",
                    assemblyPath,
                    "GreenChainz.Revit.Commands.SendRFQCommand")
                {
                    ToolTip = "Send Request for Quotation to suppliers",
                    LongDescription = "Generate and send a Request for Quotation (RFQ) based on the " +
                                     "materials in your model. Connect directly with verified sustainable " +
                                     "material suppliers through the GreenChainz platform."
                };
                panel.AddItem(sendRfqButtonData);

                // Create About button
                PushButtonData aboutButtonData = new PushButtonData(
                    "About",
                    "About",
                    assemblyPath,
                    "GreenChainz.Revit.Commands.AboutCommand")
                {
                    ToolTip = "About GreenChainz",
                    LongDescription = "View information about the GreenChainz plugin and user account."
                };
                panel.AddItem(aboutButtonData);

                // Handle Authentication
                InitializeAuth();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("GreenChainz Startup Error",
                    $"Failed to initialize GreenChainz plugin:\n\n{ex.Message}\n\n{ex.StackTrace}");
                return Result.Failed;
            }
        }

        private void InitializeAuth()
        {
            // Auto-login
            if (!AuthService.Instance.AutoLogin())
            {
                // Show login dialog if not logged in or token expired
                // We use Dispatcher to show it on the UI thread, but OnStartup runs on UI thread anyway usually.
                // Note: Showing modal dialog in OnStartup might block splash screen, but it's requested.

                // We shouldn't block Revit startup entirely if user cancels login.
                // So we show the dialog, but handle cancellation gracefully.

                // Delaying the dialog show until Revit is fully loaded is hard without events.
                // Assuming immediate show is fine.

                LoginWindow loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                if (result != true)
                {
                    // User closed without logging in. That's fine, they can login later via About or when using a command.
                }
            }
        }

        /// <summary>
        /// Called when Revit shuts down. Cleanup if needed.
        /// </summary>
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
