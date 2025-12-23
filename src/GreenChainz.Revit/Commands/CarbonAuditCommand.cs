using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Models;
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
        private AuditCompletedHandler _handler;
        private ExternalEvent _externalEvent;

        /// <summary>
        /// Execute the Carbon Audit command.
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Initialize External Event Handler
                _handler = new AuditCompletedHandler();
                _externalEvent = ExternalEvent.Create(_handler);

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

                // Initialize Service
                var auditService = new AuditService();

                // 1. Extract Data (Must be on Main Thread)
                // We call ExtractMaterials directly here instead of inside ScanProject
                // so we can pass the data to the background thread.
                List<ProjectMaterial> materials = auditService.ExtractMaterials(doc);

                AuditRequest request = new AuditRequest
                {
                    ProjectName = modelName,
                    Materials = materials
                };

                // 2. Show Loading UI
                AuditProgressWindow progressWindow = new AuditProgressWindow();
                progressWindow.Show(); // Modeless

                // 3. Run API Call in Background
                Task.Run(async () =>
                {
                    try
                    {
                        AuditResult result = await auditService.SubmitAuditAsync(request);

                        // 4. Marshal back to UI Thread
                        _handler.SetData(result, progressWindow);
                        _externalEvent.Raise();
                    }
                    catch (Exception ex)
                    {
                        // Marshal exception back to UI Thread
                        _handler.SetError(ex.Message, progressWindow);
                        _externalEvent.Raise();
                    }
                });

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
