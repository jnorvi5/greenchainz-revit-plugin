using System;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to browse sustainable materials from the GreenChainz marketplace.
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
                TaskDialog.Show(
                    "GreenChainz - Browse Materials",
                    "Browse Materials feature coming soon!");

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
