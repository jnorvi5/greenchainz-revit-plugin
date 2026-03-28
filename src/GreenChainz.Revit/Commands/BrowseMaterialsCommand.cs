using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to browse sustainable materials from the GreenChainz marketplace.
    /// Opens the Material Browser dockable panel.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class BrowseMaterialsCommand : IExternalCommand
    {
        /// <summary>
        /// Execute the Browse Materials command.
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DockablePaneId dpid = new DockablePaneId(App.MaterialBrowserPaneId);
                DockablePane dp = commandData.Application.GetDockablePane(dpid);
                dp.Show();
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
