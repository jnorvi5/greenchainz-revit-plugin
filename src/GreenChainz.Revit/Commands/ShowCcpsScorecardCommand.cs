using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ShowCcpsScorecardCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null) return Result.Failed;

                Document doc = uidoc.Document;
                var selectedIds = uidoc.Selection.GetElementIds();

                if (!selectedIds.Any())
                {
                    TaskDialog.Show("CCPS Scorecard", "Please select a Revit element to view its CCPS scorecard.");
                    return Result.Succeeded;
                }

                Element elem = doc.GetElement(selectedIds.First());
                var matIds = elem.GetMaterialIds(false);

                if (!matIds.Any())
                {
                    TaskDialog.Show("CCPS Scorecard", "The selected element does not have associated materials.");
                    return Result.Succeeded;
                }

                var mat = doc.GetElement(matIds.First()) as Autodesk.Revit.DB.Material;
                string matName = mat.Name;
                string category = elem.Category?.Name ?? "Unknown";

                // Fetch scorecard from backend
                var apiClient = new ApiClient();
                // In a real scenario, we'd pass a real material ID or search by name
                var scorecardTask = apiClient.GetMaterialScorecardAsync(0);
                scorecardTask.Wait();
                var scorecard = scorecardTask.Result;

                // Show the window
                var window = new CcpsScorecardWindow(matName, category, scorecard);
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
