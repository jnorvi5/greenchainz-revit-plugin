using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// LEED v5 BD+C Calculator Command
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class LeedV5Command : IExternalCommand
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

                var calculator = new LeedV5Calculator();
                var result = calculator.CalculateLeedV5Score(doc);

                var window = new LeedV5Window(result);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("LEED v5 Error", $"An error occurred:\n\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
