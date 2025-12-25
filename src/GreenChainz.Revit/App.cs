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
                    "GreenChainz.Revit.Commands.MaterialBrowserCmd")
                {
                    ToolTip = "Open the Sustainable Material Browser panel.",
                    LongDescription = "Open the GreenChainz materials browser to search and filter " +
                                      "sustainable building materials. View detailed specifications, " +
                                      "environmental certifications, and pricing information.",
                    LargeImage = GetEmbeddedImage("icon_32.png"),
                    Image = GetEmbeddedImage("icon_16.png")
                };
                panel.AddItem(browseBtnData);

                // 2. Carbon Audit Button
                PushButtonData carbonAuditButtonData = new PushButtonData(
                    "CarbonAudit",
                    "Carbon\nAudit",
                    assemblyPath,
                    "GreenChainz.Revit.Commands.CarbonAuditCommand")
                {
                    ToolTip = "Analyze the carbon footprint of your Revit model",
                    LongDescription = "Calculate the embodied carbon of materials in your current model. " +
                                      "Get detailed reports on carbon emissions by material category and " +
                                      "identify opportunities to reduce environmental impact.",
                    LargeImage = GetEmbeddedImage("icon_32.png"),
                    Image = GetEmbeddedImage("icon_16.png")
                };
                panel.AddItem(carbonAuditButtonData);

                // 3. Send RFQ Button
                PushButtonData sendRfqButtonData = new PushButtonData(
                    "SendRFQ",
                    "Send\nRFQ",
                    assemblyPath,
                    "GreenChainz.Revit.Commands.SendRFQCommand")
                {
                    ToolTip = "Send Request for Quotation to suppliers",
                    LongDescription = "Generate and send a Request for Quotation (RFQ) based on the " +
                                      "materials in your model. Connect directly with verified sustainable " +
                                      "material suppliers through the GreenChainz platform.",
                    LargeImage = GetEmbeddedImage("icon_32.png"),
                    Image = GetEmbeddedImage("icon_16.png")
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
                    LongDescription = "View information about the GreenChainz plugin and user account.",
                    LargeImage = GetEmbeddedImage("icon_32.png"),
                    Image = GetEmbeddedImage("icon_16.png")
                };
                panel.AddItem(aboutButtonData);

                // Register Dockable Pane
                MaterialBrowserPanel browserPanel = new MaterialBrowserPanel(MaterialService);
                DockablePaneId dpid = new DockablePaneId(MaterialBrowserPaneId);

                try
                {
                    application.RegisterDockablePane(dpid, "Material Browser", browserPanel);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to register dockable pane: {ex.Message}");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("GreenChainz Startup Error",
                    $"Failed to initialize GreenChainz plugin:\n\n{ex.Message}");
                return Result.Failed;
            }
        }

        private void InitializeServices()
        {
            // Read credentials from environment variables
            string clientId = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_ID") ?? "";
            string clientSecret = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_SECRET") ?? "";

            AuthService = new AutodeskAuthService(clientId, clientSecret);

            // Only create SDA connector if valid credentials are provided
            if (AuthService.HasValidCredentials())
            {
                SdaService = new SdaConnectorService(AuthService);
                MaterialService = new MaterialService(SdaService);
                System.Diagnostics.Debug.WriteLine("GreenChainz: Using live SDA service");
            }
            else
            {
                // Use mock data when no credentials are available
                MaterialService = new MaterialService();
                System.Diagnostics.Debug.WriteLine("GreenChainz: Using mock data (no credentials configured)");
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        /// <summary>
        /// Helper to load embedded image resources
        /// </summary>
        private BitmapImage GetEmbeddedImage(string name)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
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
