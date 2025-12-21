using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to perform carbon audit analysis on the current Revit model.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CarbonAuditCommand : IExternalCommand
    {
        /// <summary>
        /// Execute the Carbon Audit command.
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Check if user is logged in
                if (!AuthService.Instance.IsLoggedIn)
                {
                    // Prompt login
                    LoginWindow loginWindow = new LoginWindow();
                    bool? result = loginWindow.ShowDialog();

                    if (result != true || !AuthService.Instance.IsLoggedIn)
                    {
                        message = "You must be logged in to perform a Carbon Audit.";
                        return Result.Cancelled;
                    }
                }

                // Check audit credits
                if (AuthService.Instance.Credits <= 0)
                {
                    TaskDialog.Show("Insufficient Credits", "You do not have enough credits to perform a Carbon Audit. Please contact support or upgrade your plan.");
                    return Result.Cancelled;
                }

                // Get the current document
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("Error", "No active document found.");
                    return Result.Failed;
                }

                Document doc = uidoc.Document;
                string modelName = doc.Title;

                // TODO: Deduct credit here? Or after successful audit?
                // Assuming we check first.

                MessageBox.Show(
                    $"Current Model: {modelName}\n\nCarbon Audit running...\nCredits available: {AuthService.Instance.Credits}",
                    "GreenChainz - Carbon Audit",
                    $"Current Model: {modelName}\n\nCarbon Audit feature coming soon!");

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
