using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

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
                PushButton browseMaterialsButton = panel.AddItem(browseMaterialsButtonData) as PushButton;

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
                PushButton carbonAuditButton = panel.AddItem(carbonAuditButtonData) as PushButton;

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
                PushButton sendRfqButton = panel.AddItem(sendRfqButtonData) as PushButton;

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("GreenChainz Startup Error",
                    $"Failed to initialize GreenChainz plugin:\n\n{ex.Message}\n\n{ex.StackTrace}");
                return Result.Failed;
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
