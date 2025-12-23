using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.ApplicationServices;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;
using GreenChainz.Revit.Commands;

namespace GreenChainz.Revit
{
    public class App : IExternalApplication
    {
        public static readonly Guid MaterialBrowserPaneId = new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");
        public static AutodeskAuthService AuthService { get; private set; }
        public static SdaConnectorService SdaService { get; private set; }
        public static MaterialService MaterialService { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 1. Init Telemetry (The Black Box)
                TelemetryService.Initialize();
                TelemetryService.LogInfo("GreenChainz Plugin Starting...");

                // 2. Initialize Services
                InitializeServices();

                // 3. UI Setup
                string tabName = "GreenChainz";
                try { application.CreateRibbonTab(tabName); } catch { }
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Sustainable Materials");

                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Button 1: Material Browser
                PushButtonData browseBtnData = new PushButtonData(
                    "cmdBrowseMaterials", "Browse\nMaterials", assemblyPath,
                    "GreenChainz.Revit.Commands.MaterialBrowserCmd");
                browseBtnData.ToolTip = "Open the Sustainable Material Browser";
                browseBtnData.LargeImage = GetEmbeddedImage("icon_32.png");
                browseBtnData.Image = GetEmbeddedImage("icon_16.png");
                panel.AddItem(browseBtnData);

                // Button 2: Carbon Audit
                PushButtonData auditBtnData = new PushButtonData(
                    "CarbonAudit", "Carbon Audit", assemblyPath,
                    "GreenChainz.Revit.Commands.CarbonAuditCommand");
                auditBtnData.ToolTip = "Analyze carbon footprint";
                panel.AddItem(auditBtnData);

                // 4. Register Dockable Pane
                MaterialBrowserPanel browserPanel = new MaterialBrowserPanel(MaterialService);
                DockablePaneId dpid = new DockablePaneId(MaterialBrowserPaneId);
                application.RegisterDockablePane(dpid, "GreenChainz Browser", browserPanel);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TelemetryService.LogError(ex, "OnStartup");
                TaskDialog.Show("GreenChainz Error", "Plugin failed to start. See logs.");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            TelemetryService.LogInfo("Plugin Shutdown");
            return Result.Succeeded;
        }

        private void InitializeServices()
        {
            string clientId = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_ID");
            string clientSecret = Environment.GetEnvironmentVariable("AUTODESK_CLIENT_SECRET");

            AuthService = new AutodeskAuthService(clientId, clientSecret);

            if (AuthService.HasValidCredentials())
            {
                SdaService = new SdaConnectorService(AuthService);
                MaterialService = new MaterialService(SdaService);
            }
            else
            {
                TelemetryService.LogInfo("No credentials found. Using Mock Mode.");
                MaterialService = new MaterialService();
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
