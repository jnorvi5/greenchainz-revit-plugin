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
        public static readonly Guid ChainBotPaneId = new Guid("B2C3D4E5-F6A7-8901-2345-67890ABCDEF0");
        
        // Services
        public static AutodeskAuthService AuthService { get; private set; }
        public static SdaConnectorService SdaService { get; private set; }
        public static MaterialService MaterialService { get; private set; }
        public static Ec3ApiService Ec3Service { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                InitializeServices();

                string tabName = "GreenChainz";
                try { application.CreateRibbonTab(tabName); } catch { }
                
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Panel: AI Assistant
                RibbonPanel aiPanel = application.CreateRibbonPanel(tabName, "AI Assistant");
                PushButtonData chainBotBtn = new PushButtonData(
                    "ChainBot", "ChainBot\n(AI)", assemblyPath,
                    "GreenChainz.Revit.Commands.ShowChainBotCommand")
                { ToolTip = "Open ChainBot for contextual sustainability advice" };
                aiPanel.AddItem(chainBotBtn);

                // Panel 1: Scorecard
                RibbonPanel scorecardPanel = application.CreateRibbonPanel(tabName, "Scorecard");
                PushButtonData scorecardButtonData = new PushButtonData(
                    "Scorecard", "Sustainability\nScorecard", assemblyPath,
                    "GreenChainz.Revit.Commands.ScorecardCommand")
                { ToolTip = "Generate enterprise-grade sustainability scorecard" };
                scorecardPanel.AddItem(scorecardButtonData);

                // Panel 2: Carbon Analysis
                RibbonPanel carbonPanel = application.CreateRibbonPanel(tabName, "Carbon");
                PushButtonData carbonAuditButtonData = new PushButtonData(
                    "CarbonAudit", "Carbon\nAudit", assemblyPath,
                    "GreenChainz.Revit.Commands.CarbonAuditCommand")
                { ToolTip = "Detailed carbon footprint analysis" };
                carbonPanel.AddItem(carbonAuditButtonData);

                // Panel 4: Procurement
                RibbonPanel procurementPanel = application.CreateRibbonPanel(tabName, "Procurement");
                PushButtonData sendRfqButtonData = new PushButtonData(
                    "SendRFQ", "Send\nRFQ", assemblyPath,
                    "GreenChainz.Revit.Commands.SendRFQCommand")
                { ToolTip = "Send RFQ to sustainable suppliers" };
                procurementPanel.AddItem(sendRfqButtonData);

                // Register Dockable Panes
                MaterialBrowserPanel browserPanel = new MaterialBrowserPanel(MaterialService);
                application.RegisterDockablePane(new DockablePaneId(MaterialBrowserPaneId), "Material Browser", browserPanel);

                ChatPanel chatPanel = new ChatPanel();
                application.RegisterDockablePane(new DockablePaneId(ChainBotPaneId), "ChainBot (AI)", chatPanel as IDockablePaneProvider);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("GreenChainz Error", $"Startup failed: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void InitializeServices()
        {
            Ec3Service = new Ec3ApiService(Environment.GetEnvironmentVariable("EC3_API_KEY") ?? "");
            AuthService = new AutodeskAuthService(Environment.GetEnvironmentVariable("AUTODESK_CLIENT_ID") ?? "", Environment.GetEnvironmentVariable("AUTODESK_CLIENT_SECRET") ?? "");
            
            if (AuthService.HasValidCredentials())
            {
                SdaService = new SdaConnectorService(AuthService);
                MaterialService = new MaterialService(SdaService);
            }
            else
            {
                MaterialService = new MaterialService();
            }
        }
    }
}
