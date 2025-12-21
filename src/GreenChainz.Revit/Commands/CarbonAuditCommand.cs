using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Views;
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

                // Mock Audit Result
                // In a real implementation, we would extract data from the model here.
                var audit = new AuditResult
                {
                    ProjectName = modelName,
                    Date = DateTime.Now,
                    OverallScore = 12500,
                    Summary = "The project shows a moderate carbon footprint. Concrete usage is the primary contributor. Consider using low-carbon concrete alternatives.",
                    Materials = new List<MaterialBreakdown>
                    {
                        new MaterialBreakdown { MaterialName = "Concrete (C30/37)", Quantity = "500 m3", CarbonFactor = 240, TotalCarbon = 120000 },
                        new MaterialBreakdown { MaterialName = "Steel Reinforcement", Quantity = "20 tons", CarbonFactor = 1.85 * 1000, TotalCarbon = 37000 },
                        new MaterialBreakdown { MaterialName = "Glass", Quantity = "200 m2", CarbonFactor = 25, TotalCarbon = 5000 },
                        new MaterialBreakdown { MaterialName = "Timber", Quantity = "50 m3", CarbonFactor = 10, TotalCarbon = 500 }
                    },
                    Recommendations = new List<Recommendation>
                    {
                        new Recommendation { Description = "Switch to Low-Carbon Concrete", PotentialSavings = 20000 },
                        new Recommendation { Description = "Use Recycled Steel", PotentialSavings = 10000 },
                        new Recommendation { Description = "Optimize Glazing Area", PotentialSavings = 1000 }
                    }
                };

                // Calculate total
                double total = 0;
                foreach (var m in audit.Materials) total += m.TotalCarbon;
                audit.OverallScore = total;

                // Open Results Window
                // Using Revit's window handle as owner is best practice but keeping it simple for now
                AuditResultsWindow window = new AuditResultsWindow(audit);
                window.ShowDialog();
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
