using System;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to send Request for Quotation (RFQ) to suppliers.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class SendRFQCommand : IExternalCommand
    {
        /// <summary>
        /// Execute the Send RFQ command.
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                MessageBox.Show(
                    "Send RFQ feature coming soon!",
                    "GreenChainz - Send RFQ",
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
