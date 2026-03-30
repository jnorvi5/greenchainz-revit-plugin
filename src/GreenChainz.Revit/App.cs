using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;
using GreenChainz.Revit.Utils;

namespace GreenChainz.Revit
{
    public class App : IExternalApplication
    {
        public static readonly Guid MaterialBrowserPaneId = new Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF");
        public static readonly Guid ChainBotPaneId = new Guid("B2C3D4E5-F6A7-8901-2345-67890ABCDEF0");
        
        public static AutodeskAuthService AuthService { get; private set; }
        public static SdaConnectorService SdaService { get; private set; }
        public static MaterialService MaterialService { get; private set; }
        public static Ec3ApiService Ec3Service { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                InitializeServices();

                // Register document opened event to ensure GC_AgentTag parameter exists
                application.ControlledApplication.DocumentOpened += OnDocumentOpened;

                string tabName = "GreenChainz";
                try { application.CreateRibbonTab(tabName); } catch { }
                
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Panel: AI & Intelligence
                RibbonPanel intelPanel = application.CreateRibbonPanel(tabName, "Intelligence");
                
                PushButtonData chainBotBtn = new PushButtonData(
                    "ChainBot", "ChainBot\n(AI)", assemblyPath,
                    "GreenChainz.Revit.Commands.ShowChainBotCommand")
                { ToolTip = "Open ChainBot for contextual sustainability advice" };
                intelPanel.AddItem(chainBotBtn);

                PushButtonData ccpsBtn = new PushButtonData(
                    "CCPS", "CCPS\nScorecard", assemblyPath,
                    "GreenChainz.Revit.Commands.ShowCcpsScorecardCommand")
                { ToolTip = "View the 6-pillar Comprehensive Carbon Performance Score for selected material" };
                intelPanel.AddItem(ccpsBtn);

                // Panel 1: Analysis
                RibbonPanel analysisPanel = application.CreateRibbonPanel(tabName, "Analysis");
                
                PushButtonData scorecardButtonData = new PushButtonData(
                    "Scorecard", "Project\nScorecard", assemblyPath,
                    "GreenChainz.Revit.Commands.ScorecardCommand")
                { ToolTip = "Generate project-wide sustainability scorecard" };
                analysisPanel.AddItem(scorecardButtonData);

                PushButtonData carbonAuditButtonData = new PushButtonData(
                    "CarbonAudit", "Carbon\nAudit", assemblyPath,
                    "GreenChainz.Revit.Commands.CarbonAuditCommand")
                { ToolTip = "Sync BIM data with GreenChainz Carbon Dashboard" };
                analysisPanel.AddItem(carbonAuditButtonData);

                // Panel 2: Materials
                RibbonPanel materialsPanel = application.CreateRibbonPanel(tabName, "Materials");

                PushButtonData browseMaterialsButtonData = new PushButtonData(
                    "BrowseMaterials", "Browse\nMaterials", assemblyPath,
                    "GreenChainz.Revit.Commands.BrowseMaterialsCommand")
                { ToolTip = "Browse sustainable materials and add them to your Revit project" };
                materialsPanel.AddItem(browseMaterialsButtonData);

                // Panel 3: Procurement
                RibbonPanel procurementPanel = application.CreateRibbonPanel(tabName, "Procurement");
                
                PushButtonData sendRfqButtonData = new PushButtonData(
                    "SendRFQ", "Send\nRFQ", assemblyPath,
                    "GreenChainz.Revit.Commands.SendRFQCommand")
                { ToolTip = "Send RFQ to sustainable suppliers" };
                procurementPanel.AddItem(sendRfqButtonData);

                PushButtonData rfqStatusBtn = new PushButtonData(
                    "RFQStatus", "RFQ\nStatus", assemblyPath,
                    "GreenChainz.Revit.Commands.ShowRfqStatusCommand")
                { ToolTip = "Track RFQ status and message suppliers" };
                procurementPanel.AddItem(rfqStatusBtn);

<<<<<<< HEAD
                // Panel 5: AI Agent
                RibbonPanel agentPanel = application.CreateRibbonPanel(tabName, "AI Agent");

                PushButtonData runAgentButtonData = new PushButtonData(
                    "RunAgent", "AI\nAgent", assemblyPath,
                    "GreenChainz.Revit.Commands.RunAgentCommand")
                { ToolTip = "Run AI Agent to analyze and tag materials by carbon impact" };
                agentPanel.AddItem(runAgentButtonData);

                // Panel 6: Export (IFC/BCF)
                RibbonPanel exportPanel = application.CreateRibbonPanel(tabName, "Export");

                PushButtonData sampleIfcButtonData = new PushButtonData(
                    "SampleIFC", "Generate\nSample IFC", assemblyPath,
                    "GreenChainz.Revit.Commands.GenerateSampleIfcCommand")
                { ToolTip = "Generate sample IFC + BCF files for openBIM demonstration" };
                exportPanel.AddItem(sampleIfcButtonData);

                // Panel 7: Help
                RibbonPanel helpPanel = application.CreateRibbonPanel(tabName, "Help");

                PushButtonData aboutButtonData = new PushButtonData(
                    "About", "About", assemblyPath,
                    "GreenChainz.Revit.Commands.AboutCommand")
                { ToolTip = "About GreenChainz" };
                helpPanel.AddItem(aboutButtonData);

                // Register Dockable Pane
=======
                // Register Dockable Panes
>>>>>>> 039e306a47b2bc6544e95c271ca02a818ce678bf
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
            // Unregister event handler
            application.ControlledApplication.DocumentOpened -= OnDocumentOpened;
            return Result.Succeeded;
        }

        private void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            try
            {
                Document doc = e.Document;
                if (doc == null) return;

                Autodesk.Revit.ApplicationServices.Application app = doc.Application;
                SharedParameterHelper.EnsureGcAgentTag(app, doc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GreenChainz: DocumentOpened handler error: {ex.Message}");
            }
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
