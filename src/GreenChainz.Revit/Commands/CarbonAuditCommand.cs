using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to perform carbon audit analysis on the current Revit model and sync with the GreenChainz dashboard.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CarbonAuditCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("Error", "No active document found.");
                    return Result.Failed;
                }

                Document doc = uidoc.Document;

                // Run audit and sync asynchronously
                var auditService = new AuditService();
                
                // We run this synchronously in the Revit thread but the internal API calls are handled via Task.Run
                AuditResult result = Task.Run(async () => await auditService.ScanAndSyncProjectAsync(doc)).Result;

                // Show results
                AuditResultsWindow window = new AuditResultsWindow(result);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Audit Error", $"Failed to complete carbon audit: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
