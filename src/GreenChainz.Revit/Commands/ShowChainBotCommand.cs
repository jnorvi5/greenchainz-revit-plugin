using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ShowChainBotCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DockablePaneId dpid = new DockablePaneId(App.ChainBotPaneId);
                DockablePane pane = commandData.Application.GetDockablePane(dpid);

                if (pane.IsShown())
                    pane.Hide();
                else
                {
                    // Update context before showing
                    string context = ContextService.GetSelectionContext(commandData.Application.ActiveUIDocument);
                    // Note: In a real implementation, we would access the ChatPanel instance to call UpdateContext
                    pane.Show();
                }

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
