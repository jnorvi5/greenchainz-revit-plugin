using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GreenChainz.Revit.Agent
{
    /// <summary>
    /// External event handler that applies agent actions to the Revit model.
    /// Revit requires model changes to happen through transactions via ExternalEvent.
    /// </summary>
    public class AgentActionExecutor : IExternalEventHandler
    {
        /// <summary>
        /// Actions queued by the agent to be applied
        /// </summary>
        public List<AgentAction> PendingActions { get; set; } = new List<AgentAction>();

        /// <summary>
        /// Results of applying actions
        /// </summary>
        public List<string> Results { get; private set; } = new List<string>();

        /// <summary>
        /// Count of successfully applied actions
        /// </summary>
        public int SuccessCount { get; private set; }

        /// <summary>
        /// Count of failed actions
        /// </summary>
        public int FailCount { get; private set; }

        public string GetName()
        {
            return "GreenChainz Agent Action Executor";
        }

        public void Execute(UIApplication app)
        {
            if (PendingActions == null || PendingActions.Count == 0)
                return;

            Results.Clear();
            SuccessCount = 0;
            FailCount = 0;

            UIDocument uiDoc = app.ActiveUIDocument;
            if (uiDoc == null) return;

            Document doc = uiDoc.Document;

            using (Transaction tx = new Transaction(doc, "GreenChainz Agent: Apply Actions"))
            {
                tx.Start();

                foreach (var action in PendingActions)
                {
                    try
                    {
                        bool success = ApplyAction(doc, action);
                        if (success)
                        {
                            SuccessCount++;
                            Results.Add($"OK: {action.type} on {action.elementId}");
                        }
                        else
                        {
                            FailCount++;
                            Results.Add($"SKIP: {action.type} on {action.elementId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        FailCount++;
                        Results.Add($"ERROR: {action.type} on {action.elementId} - {ex.Message}");
                    }
                }

                tx.Commit();
            }

            // Clear pending actions
            PendingActions.Clear();
        }

        private bool ApplyAction(Document doc, AgentAction action)
        {
            switch (action.type)
            {
                case "set_parameter":
                    return ApplySetParameter(doc, action);

                case "flag_issue":
                    return ApplyFlagIssue(doc, action);

                case "recommend_swap":
                    return ApplyRecommendSwap(doc, action);

                case "note":
                    return ApplyNote(doc, action);

                default:
                    return false;
            }
        }

        private bool ApplySetParameter(Document doc, AgentAction action)
        {
            if (string.IsNullOrWhiteSpace(action.parameterName))
                return false;

            Element element = doc.GetElement(new ElementId(action.elementId));
            if (element == null)
                return false;

            // Try to find the parameter
            Parameter param = element.LookupParameter(action.parameterName);
            
            // If parameter doesn't exist, try to create it or use Comments
            if (param == null)
            {
                // Fallback: use the Comments parameter to store data
                param = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (param != null && !param.IsReadOnly)
                {
                    string existingComment = param.AsString() ?? "";
                    string newEntry = $"{action.parameterName}={action.parameterValue}";
                    
                    // Append or update
                    if (!existingComment.Contains(action.parameterName))
                    {
                        string separator = string.IsNullOrEmpty(existingComment) ? "" : " | ";
                        param.Set(existingComment + separator + newEntry);
                    }
                    return true;
                }
                return false;
            }

            // Set the parameter value
            if (param.IsReadOnly)
                return false;

            switch (param.StorageType)
            {
                case StorageType.String:
                    param.Set(action.parameterValue ?? "");
                    return true;

                case StorageType.Double:
                    if (double.TryParse(action.parameterValue, out double dVal))
                    {
                        param.Set(dVal);
                        return true;
                    }
                    return false;

                case StorageType.Integer:
                    if (int.TryParse(action.parameterValue, out int iVal))
                    {
                        param.Set(iVal);
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private bool ApplyFlagIssue(Document doc, AgentAction action)
        {
            Element element = doc.GetElement(new ElementId(action.elementId));
            if (element == null)
                return false;

            // Store the issue in Comments parameter
            Parameter comments = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (comments != null && !comments.IsReadOnly)
            {
                string severity = action.severity?.ToUpper() ?? "INFO";
                string existingComment = comments.AsString() ?? "";
                string flag = $"[GC-{severity}] {action.note}";
                
                if (!existingComment.Contains(action.note))
                {
                    string separator = string.IsNullOrEmpty(existingComment) ? "" : " | ";
                    comments.Set(existingComment + separator + flag);
                }
                return true;
            }

            return false;
        }

        private bool ApplyRecommendSwap(Document doc, AgentAction action)
        {
            // Store recommendation in Comments for now
            // In future, could open a dialog or add to a schedule
            Element element = doc.GetElement(new ElementId(action.elementId));
            if (element == null)
                return false;

            Parameter comments = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (comments != null && !comments.IsReadOnly)
            {
                string existingComment = comments.AsString() ?? "";
                string rec = $"[GC-SWAP] {action.note}";
                
                if (!existingComment.Contains("[GC-SWAP]"))
                {
                    string separator = string.IsNullOrEmpty(existingComment) ? "" : " | ";
                    comments.Set(existingComment + separator + rec);
                }
                return true;
            }

            return false;
        }

        private bool ApplyNote(Document doc, AgentAction action)
        {
            Element element = doc.GetElement(new ElementId(action.elementId));
            if (element == null)
                return false;

            Parameter comments = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (comments != null && !comments.IsReadOnly)
            {
                string existingComment = comments.AsString() ?? "";
                string note = $"[GC] {action.note}";
                
                if (!existingComment.Contains(action.note))
                {
                    string separator = string.IsNullOrEmpty(existingComment) ? "" : " | ";
                    comments.Set(existingComment + separator + note);
                }
                return true;
            }

            return false;
        }
    }
}
