using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Generate Enterprise-Grade Sustainability Scorecard
    /// Focus: EPD Coverage, Embodied Carbon (GWP), Verification Tier
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ScorecardCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("Error", "No active document. Please open a Revit project.");
                    return Result.Failed;
                }

                Document doc = uidoc.Document;

                // Generate scorecard
                var service = new ScorecardService();
                var scorecard = service.GenerateScorecard(doc);

                // Display results
                var window = new ScorecardWindow(scorecard);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Scorecard Error", $"An error occurred:\n\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
