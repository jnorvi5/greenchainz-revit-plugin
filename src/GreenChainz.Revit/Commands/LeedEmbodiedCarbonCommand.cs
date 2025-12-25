using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// LEED v4.1 MRpc132 Pilot Credit Calculator
    /// Procurement of Low Carbon Construction Materials
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class LeedEmbodiedCarbonCommand : IExternalCommand
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

                // Calculate using CLF methodology
                var calculator = new LeedMRpc132Calculator();
                var result = calculator.CalculateEmbodiedCarbon(doc);

                // Show results
                var window = new LeedEmbodiedCarbonWindow(result);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("LEED MRpc132 Error", $"An error occurred:\n\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
