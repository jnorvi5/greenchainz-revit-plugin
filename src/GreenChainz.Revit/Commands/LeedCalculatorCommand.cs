using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to calculate LEED certification points for the current Revit model.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class LeedCalculatorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("Error", "No active document found. Please open a Revit project.");
                    return Result.Failed;
                }

                Document doc = uidoc.Document;

                // Calculate LEED score
                var calculator = new LeedCalculatorService();
                var result = calculator.CalculateLeedScore(doc);

                // Show results
                var window = new LeedResultsWindow(result);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("LEED Calculator Error", $"An error occurred:\n\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
