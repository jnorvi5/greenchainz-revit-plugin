using System;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ProjectAuditCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Initialize Service
                AuditService auditService = new AuditService();

                // Scan Project
                AuditResult result = auditService.ScanProject(doc);

                // Display Results
                // We need to run the WPF window on a separate thread or use ShowDialog if valid handle.
                // Revit plugins are usually running in the main thread.
                // We can use the Revit window handle as owner to ensure it stays on top.

                AuditResultsWindow window = new AuditResultsWindow(result);

                // Getting Revit handle for parenting
                // System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(window);
                // helper.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
