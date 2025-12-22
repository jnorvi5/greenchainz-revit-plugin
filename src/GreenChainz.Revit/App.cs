using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using GreenChainz.Revit.Services;
using Autodesk.Revit.UI;
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
                // Initialize Services (Local)
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

<<<<<<< HEAD
                // Get the assembly path
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // 1. Browse Materials Button (Updated with Icons)
                PushButtonData browseBtnData = new PushButtonData(
                    "cmdBrowseMaterials",
                    "Browse\nMaterials",
                    assemblyPath,
                    "GreenChainz.Revit.Commands.MaterialBrowserCmd"); // Using the command that opens the pane

                browseBtnData.ToolTip = "Open the Sustainable Material Browser panel.";
                browseBtnData.LongDescription = "Open the GreenChainz materials browser to search and filter " +
                                     "sustainable building materials. View detailed specifications, " +
                                     "environmental certifications, and pricing information.";

                // Set Icons
                browseBtnData.LargeImage = GetEmbeddedImage("icon_32.png");
                browseBtnData.Image = GetEmbeddedImage("icon_16.png");

                panel.AddItem(browseBtnData);

                // 2. Carbon Audit Button (From Remote)
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

                // 3. Send RFQ Button (From Remote)
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

                // 4. About Button (From Remote)
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

                // Register Dockable Pane (From Local)
                MaterialBrowserPanel browserPanel = new MaterialBrowserPanel(MaterialService);
                DockablePaneId dpid = new DockablePaneId(MaterialBrowserPaneId);
=======
            browseBtnData.ToolTip = "Open the Sustainable Material Browser panel.";

            // Set Icons
            browseBtnData.LargeImage = GetEmbeddedImage("icon_32.png");
            browseBtnData.Image = GetEmbeddedImage("icon_16.png");

            panel.AddItem(browseBtnData);

            // 3. Register Dockable Pane with injected MaterialService
            MaterialBrowserPanel browserPanel = new MaterialBrowserPanel(MaterialService);
            DockablePaneId dpid = new DockablePaneId(MaterialBrowserPaneId);

            try
            {
>>>>>>> 7ab4670 (Update Revit plugin UI with branding and service improvements)
                application.RegisterDockablePane(dpid, "Material Browser", browserPanel);

                // Handle Authentication (From Remote)
                // InitializeAuth(); // Commented out for now as AuthService might conflict or need merging. 
                // Assuming InitializeServices handles what we need for now, or we can add it back if AuthService is available.

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("GreenChainz Startup Error",
                    $"Failed to initialize GreenChainz plugin:\n\n{ex.Message}\n\n{ex.StackTrace}");
                return Result.Failed;
            }
<<<<<<< HEAD
        }
=======

            string clientId = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_SECRET");
>>>>>>> 7ab4670 (Update Revit plugin UI with branding and service improvements)

            // Log a warning when required credentials are missing or empty
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                System.Diagnostics.Debug.WriteLine(
                    "Warning: Autodesk credentials (AUTODESK_CLIENT_ID / AUTODESK_CLIENT_SECRET) are missing or empty. " +
                    "The plugin will use mock material data instead of live Autodesk services.");
            }
        private void InitializeServices()
        {
            // Read credentials from environment variables
            string clientId = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_ID") ?? "YOUR_CLIENT_ID";
            string clientSecret = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_SECRET") ?? "YOUR_CLIENT_SECRET";

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

        public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

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
            catch
            {
                return null;
            }
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
            catch
            {
                return null;
            }
        }
    }
}
