using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to perform carbon audit analysis on the current Revit model.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CarbonAuditCommand : IExternalCommand
    {
        /// <summary>
        /// Execute the Carbon Audit command.
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current document
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("Error", "No active document found.");
                    return Result.Failed;
                }

                Document doc = uidoc.Document;
                string modelName = doc.Title;

                TaskDialog.Show(
                    "GreenChainz - Carbon Audit",
                    $"Current Model: {modelName}\n\nCarbon Audit feature coming soon!");

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
