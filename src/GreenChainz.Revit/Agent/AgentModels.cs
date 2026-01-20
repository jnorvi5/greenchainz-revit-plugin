using System.Collections.Generic;

namespace GreenChainz.Revit.Agent
{
    /// <summary>
    /// Material data to send to the AI agent
    /// </summary>
    public class MaterialDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public double? carbon_score { get; set; }
        public double? volume_m3 { get; set; }
        public string ifc_guid { get; set; }
        public string ifc_category { get; set; }
    }

    /// <summary>
    /// Request to the AI agent
    /// </summary>
    public class AgentRequest
    {
        public string task { get; set; }  // "score_materials", "recommend_swaps", "ifc_mapping"
        public List<MaterialDto> materials { get; set; }
        public string project_zip { get; set; }
        public string project_name { get; set; }

        public AgentRequest()
        {
            materials = new List<MaterialDto>();
        }
    }

    /// <summary>
    /// Action returned by the agent to apply in Revit
    /// </summary>
    public class AgentAction
    {
        public string type { get; set; }  // "set_parameter", "flag_issue", "recommend_swap", "note"
        public int elementId { get; set; }
        public string parameterName { get; set; }
        public string parameterValue { get; set; }
        public string note { get; set; }
        public string severity { get; set; }  // "critical", "warning", "info"
        public Dictionary<string, object> recommendation { get; set; }
    }

    /// <summary>
    /// Response from the AI agent
    /// </summary>
    public class AgentResponse
    {
        public bool success { get; set; }
        public List<AgentAction> actions { get; set; }
        public string message { get; set; }
        public AgentSummary summary { get; set; }

        public AgentResponse()
        {
            actions = new List<AgentAction>();
        }
    }

    /// <summary>
    /// Summary statistics from agent analysis
    /// </summary>
    public class AgentSummary
    {
        public int total_materials { get; set; }
        public double total_carbon_kgco2e { get; set; }
        public int high_carbon_materials { get; set; }
        public List<string> categories { get; set; }
        public int actions_generated { get; set; }
    }
}
