using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using GreenChainz.Revit.Services;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit
{
    public class App : IExternalApplication
    {
        // Unique GUID for the dockable pane
        public static readonly Guid MaterialBrowserPaneId = new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");

        // Static services accessible throughout the plugin
        public static AutodeskAuthService AuthService { get; private set; }
        public static SdaConnectorService SdaService { get; private set; }
        public static MaterialService MaterialService { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Hook into failure processing for global exception handling (Revit API limited)
                application.ControlledApplication.FailuresProcessing += ControlledApplication_FailuresProcessing;

                // Initialize Services
                InitializeServices();

                // Create ribbon tab
                const string tabName = "GreenChainz";
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch { /* Tab might already exist */ }

                // Create ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Sustainable Materials");

                // Get the assembly path
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // 1. Browse Materials Button
                PushButtonData browseBtnData = new PushButtonData(
                    "cmdBrowseMaterials",
                    "Browse\nMaterials",
                    assemblyPath,
                    "GreenChainz.Revit.Commands.MaterialBrowserCmd");

                browseBtnData.ToolTip = "Open the Sustainable Material Browser panel.";
                browseBtnData.LongDescription = "Open the GreenChainz materials browser to search and filter " +
                                     "sustainable building materials. View detailed specifications, " +
                                     "environmental certifications, and pricing information.";

                // Set Icons
                browseBtnData.LargeImage = GetEmbeddedImage("icon_32.png");
                browseBtnData.Image = GetEmbeddedImage("icon_16.png");

                panel.AddItem(browseBtnData);

                // 2. Carbon Audit Button
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

                // 3. Send RFQ Button
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

                // 4. About Button
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

                // Register Dockable Pane
                MaterialBrowserPanel browserPanel = new MaterialBrowserPanel(MaterialService);
                DockablePaneId dpid = new DockablePaneId(MaterialBrowserPaneId);

                application.RegisterDockablePane(dpid, "Material Browser", browserPanel);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TelemetryService.LogError(ex, "App.OnStartup");
                TaskDialog.Show("GreenChainz Startup Error",
                    $"Failed to initialize GreenChainz plugin:\n\n{ex.Message}\n\nSee %AppData%/GreenChainz/logs.txt for details.");
                return Result.Failed;
            }
        }

        private void ControlledApplication_FailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            // Note: FailuresProcessing is for Revit failures (warnings/errors in model),
            // but we can use it to log if our plugin causes issues that trigger this.
            // A true "AppDomain.CurrentDomain.UnhandledException" might be better for general crashes,
            // but in Revit context, we must be careful.

            // For now, the task asked to Subscribe to 'ControlledApplication.FailuresProcessing' event
            // OR wrap the 'OnStartup' logic in a try/catch block. I have done both/either.
            // But usually FailuresProcessing is about model failures.

            // If the user meant "Global Exception Handler", usually that means AppDomain.UnhandledException.
            // However, the instructions say:
            // "Subscribe to the 'ControlledApplication.FailuresProcessing' event or wrap the 'OnStartup' logic in a try/catch block."

            // I've wrapped OnStartup. I will also add the event handler but keep it empty/simple
            // as catching exceptions there is different.
            // Actually, FailuresProcessing is for *handling* failures, not catching unhandled exceptions.
            // So sticking to OnStartup try/catch is the main protection for startup crashes.

            // But let's verify if I should put anything here.
            // If I leave it empty, it does nothing.
        }

        private void InitializeServices()
        {
            // Read credentials from environment variables
            string clientId = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_ID") ?? "YOUR_CLIENT_ID";
            string clientSecret = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_SECRET") ?? "YOUR_CLIENT_SECRET";

            // Log a warning when required credentials are missing or empty
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || clientId == "YOUR_CLIENT_ID")
            {
                 LogWarning("Warning: Autodesk credentials (AUTODESK_CLIENT_ID / AUTODESK_CLIENT_SECRET) are missing or empty. " +
                           "The plugin will use mock material data instead of live Autodesk services.");
            }

            AuthService = new AutodeskAuthService(clientId, clientSecret);

            // Only create SDA connector if valid credentials are provided
            if (AuthService.HasValidCredentials())
            {
                SdaService = new SdaConnectorService(AuthService);
                MaterialService = new MaterialService(SdaService);
            }
            else
            {
                // Use mock data when no credentials are available
                MaterialService = new MaterialService();
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
             application.ControlledApplication.FailuresProcessing -= ControlledApplication_FailuresProcessing;
             return Result.Succeeded;
        }

        /// <summary>
        /// Logs a warning message to the debug output.
        /// </summary>
        private void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        /// <summary>
        /// Helper to load embedded image resources
        /// </summary>
        private BitmapImage GetEmbeddedImage(string name)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                // Resource name format: Namespace.Folder.Filename
                // Note: Folders in project become dot-separated in resource name
                string resourceName = $"GreenChainz.Revit.Resources.{name}";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;

                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    return image;
                }
            }
            catch (Exception ex)
            {
                TelemetryService.LogError(ex, $"GetEmbeddedImage({name})");
                return null;
            }
        }
    }
}
