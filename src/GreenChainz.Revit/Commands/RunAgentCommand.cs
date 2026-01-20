using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Agent;
using GreenChainz.Revit.Models;
using RevitMaterial = Autodesk.Revit.DB.Material;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to run the GreenChainz AI Agent on the current model.
    /// Collects materials, sends to agent, applies returned actions.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class RunAgentCommand : IExternalCommand
    {
        private static AgentActionExecutor _executor;
        private static ExternalEvent _externalEvent;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Initialize external event (lazy)
                if (_executor == null)
                {
                    _executor = new AgentActionExecutor();
                    _externalEvent = ExternalEvent.Create(_executor);
                }

                // 1. Check if agent is running
                TaskDialog.Show("GreenChainz Agent", "Connecting to AI Agent...\n\nMake sure the agent is running:\ndocker run -p 8000:8000 greenchainz-agent");

                AgentClient client = null;
                try
                {
                    client = Task.Run(() => AgentClient.FindRunningAgentAsync()).Result;
                }
                catch { }

                if (client == null)
                {
                    TaskDialog.Show("GreenChainz Agent", 
                        "Agent not found!\n\n" +
                        "Start the agent with:\n" +
                        "cd agent\n" +
                        "docker run -p 8000:8000 greenchainz-agent\n\n" +
                        "Or run locally:\n" +
                        "uvicorn main:app --reload");
                    return Result.Cancelled;
                }

                // 2. Collect materials from selection or whole model
                IList<Element> selection = uiDoc.Selection.GetElementIds()
                    .Select(id => doc.GetElement(id))
                    .Where(e => e != null)
                    .ToList();

                var materials = new Dictionary<long, RevitMaterial>();

                if (selection.Count > 0)
                {
                    // Get materials from selected elements
                    foreach (var elem in selection)
                    {
                        var matIds = elem.GetMaterialIds(false);
                        foreach (var mid in matIds)
                        {
                            var mat = doc.GetElement(mid) as RevitMaterial;
                            if (mat != null && !materials.ContainsKey(mat.Id.Value))
                            {
                                materials[mat.Id.Value] = mat;
                            }
                        }
                    }
                }
                else
                {
                    // Collect all materials in the document
                    var collector = new FilteredElementCollector(doc)
                        .OfClass(typeof(RevitMaterial));

                    foreach (RevitMaterial mat in collector)
                    {
                        materials[mat.Id.Value] = mat;
                    }
                }

                if (materials.Count == 0)
                {
                    TaskDialog.Show("GreenChainz Agent", "No materials found to analyze.");
                    return Result.Succeeded;
                }

                // 3. Map to DTOs
                var materialDtos = materials.Values.Select(m => new MaterialDto
                {
                    id = (int)m.Id.Value,
                    name = m.Name,
                    category = m.MaterialCategory,
                    carbon_score = null,
                    volume_m3 = 1.0, // Default - could calculate from elements
                    ifc_guid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                    ifc_category = null
                }).ToList();

                // 4. Create request
                var request = new AgentRequest
                {
                    task = "score_materials",
                    materials = materialDtos,
                    project_name = doc.Title,
                    project_zip = "94105" // Default - could get from project info
                };

                // 5. Call the agent
                AgentResponse response;
                try
                {
                    response = Task.Run(() => client.InferAsync(request)).Result;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("GreenChainz Agent", $"Agent error:\n\n{ex.Message}");
                    client.Dispose();
                    return Result.Failed;
                }

                client.Dispose();

                if (response == null || !response.success)
                {
                    TaskDialog.Show("GreenChainz Agent", "Agent returned no response.");
                    return Result.Failed;
                }

                // 6. Filter to actionable items (set_parameter only for now)
                var actionsToApply = response.actions
                    .Where(a => a.type == "set_parameter" || a.type == "flag_issue")
                    .ToList();

                if (actionsToApply.Count == 0)
                {
                    TaskDialog.Show("GreenChainz Agent", 
                        $"Analysis complete!\n\n{response.message}\n\nNo actions to apply.");
                    return Result.Succeeded;
                }

                // 7. Queue actions and raise external event
                _executor.PendingActions = actionsToApply;
                _externalEvent.Raise();

                // 8. Show summary
                var summaryText = $"Agent Analysis Complete!\n\n{response.message}";
                
                if (response.summary != null)
                {
                    summaryText += $"\n\nSummary:\n" +
                        $"- Materials analyzed: {response.summary.total_materials}\n" +
                        $"- Total carbon: {response.summary.total_carbon_kgco2e:N0} kgCO2e\n" +
                        $"- High-carbon materials: {response.summary.high_carbon_materials}\n" +
                        $"- Actions queued: {actionsToApply.Count}";
                }

                // Find high-carbon materials for recommendations
                var highCarbonActions = response.actions
                    .Where(a => a.type == "recommend_swap")
                    .Take(3)
                    .ToList();

                if (highCarbonActions.Count > 0)
                {
                    summaryText += "\n\nTop Recommendations:";
                    foreach (var rec in highCarbonActions)
                    {
                        summaryText += $"\n• {rec.note}";
                    }
                }

                TaskDialog.Show("GreenChainz Agent", summaryText);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("GreenChainz Agent Error", ex.ToString());
                return Result.Failed;
            }
        }
    }
}
