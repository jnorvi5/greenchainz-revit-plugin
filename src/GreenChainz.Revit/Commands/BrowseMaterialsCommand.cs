using System;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

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
                MessageBox.Show(
                    "Browse Materials feature coming soon!",
                    "GreenChainz - Browse Materials",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

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
