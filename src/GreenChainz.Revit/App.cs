using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit
{
    public class App : IExternalApplication
    {
        public static readonly Guid MaterialBrowserPaneId = new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");
        
        // Services
        public static AutodeskAuthService AuthService { get; private set; }
        public static SdaConnectorService SdaService { get; private set; }
        public static MaterialService MaterialService { get; private set; }
        public static Ec3ApiService Ec3Service { get; private set; }

        // API Keys (can be overridden by environment variables)
        private const string DEFAULT_EC3_API_KEY = ""; // Set via env var EC3_API_KEY
        private const string DEFAULT_AUTODESK_CLIENT_ID = ""; // Set via env var AUTODESK_CLIENT_ID
        private const string DEFAULT_AUTODESK_SECRET = ""; // Set via env var AUTODESK_CLIENT_SECRET

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                InitializeServices();

                string tabName = "GreenChainz";
                try { application.CreateRibbonTab(tabName); } catch { }
                
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Panel 1: Scorecard (Main Feature)
                RibbonPanel scorecardPanel = application.CreateRibbonPanel(tabName, "Scorecard");

                PushButtonData scorecardButtonData = new PushButtonData(
                    "Scorecard", "Sustainability\nScorecard", assemblyPath,
                    "GreenChainz.Revit.Commands.ScorecardCommand")
                { ToolTip = "Generate enterprise-grade sustainability scorecard (EPD, GWP, Verification)" };
                scorecardPanel.AddItem(scorecardButtonData);

                // Panel 2: Carbon Analysis
                RibbonPanel carbonPanel = application.CreateRibbonPanel(tabName, "Carbon");

                PushButtonData carbonAuditButtonData = new PushButtonData(
                    "CarbonAudit", "Carbon\nAudit", assemblyPath,
                    "GreenChainz.Revit.Commands.CarbonAuditCommand")
                { ToolTip = "Detailed carbon footprint analysis" };
                carbonPanel.AddItem(carbonAuditButtonData);

                // Panel 3: LEED Calculators
                RibbonPanel leedPanel = application.CreateRibbonPanel(tabName, "LEED");

                PushButtonData leedV5ButtonData = new PushButtonData(
                    "LeedV5", "LEED v5\nBD+C", assemblyPath,
                    "GreenChainz.Revit.Commands.LeedV5Command")
                { ToolTip = "LEED v5 BD+C full credit analysis" };
                leedPanel.AddItem(leedV5ButtonData);

                PushButtonData mrpc132ButtonData = new PushButtonData(
                    "LeedMRpc132", "MRpc132\nEmbodied C", assemblyPath,
                    "GreenChainz.Revit.Commands.LeedEmbodiedCarbonCommand")
                { ToolTip = "LEED v4.1 MRpc132 Pilot Credit" };
                leedPanel.AddItem(mrpc132ButtonData);

                // Panel 4: Procurement
                RibbonPanel procurementPanel = application.CreateRibbonPanel(tabName, "Procurement");

                PushButtonData sendRfqButtonData = new PushButtonData(
                    "SendRFQ", "Send\nRFQ", assemblyPath,
                    "GreenChainz.Revit.Commands.SendRFQCommand")
                { ToolTip = "Send RFQ to sustainable suppliers" };
                procurementPanel.AddItem(sendRfqButtonData);

                PushButtonData browseBtnData = new PushButtonData(
                    "cmdBrowseMaterials", "Browse\nMaterials", assemblyPath,
                    "GreenChainz.Revit.Commands.MaterialBrowserCmd")
                { ToolTip = "Browse sustainable materials" };
                procurementPanel.AddItem(browseBtnData);

                // Panel 5: Help
                RibbonPanel helpPanel = application.CreateRibbonPanel(tabName, "Help");

                PushButtonData aboutButtonData = new PushButtonData(
                    "About", "About", assemblyPath,
                    "GreenChainz.Revit.Commands.AboutCommand")
                { ToolTip = "About GreenChainz" };
                helpPanel.AddItem(aboutButtonData);

                // Register Dockable Pane
                MaterialBrowserPanel browserPanel = new MaterialBrowserPanel(MaterialService);
                DockablePaneId dpid = new DockablePaneId(MaterialBrowserPaneId);

                try
                {
                    application.RegisterDockablePane(dpid, "Material Browser", browserPanel);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Dockable pane: {ex.Message}");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("GreenChainz Error", $"Startup failed:\n\n{ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void InitializeServices()
        {
            // EC3 API (Building Transparency)
            string ec3Key = Environment.GetEnvironmentVariable("EC3_API_KEY") ?? DEFAULT_EC3_API_KEY;
            Ec3Service = new Ec3ApiService(ec3Key);
            
            // Autodesk Platform Services
            string clientId = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_ID") ?? DEFAULT_AUTODESK_CLIENT_ID;
            string clientSecret = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_SECRET") ?? DEFAULT_AUTODESK_SECRET;

            AuthService = new AutodeskAuthService(clientId, clientSecret);

            if (AuthService.HasValidCredentials())
            {
                SdaService = new SdaConnectorService(AuthService);
                MaterialService = new MaterialService(SdaService);
                System.Diagnostics.Debug.WriteLine("GreenChainz: Using Autodesk SDA + EC3 APIs");
            }
            else
            {
                MaterialService = new MaterialService();
                System.Diagnostics.Debug.WriteLine("GreenChainz: Using EC3 API + mock Autodesk data");
            }
        }

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
            catch { return null; }
        }
    }
}
