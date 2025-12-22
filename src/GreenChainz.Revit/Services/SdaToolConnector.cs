using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Connector for the Autodesk Sustainability Data Accelerator (SDA) API.
    /// </summary>
    public class SdaToolConnector : IAutodeskToolConnector
    {
        private readonly SdaConnectorService _sdaService;

        public string ToolName => "Sustainability Data Accelerator";
        public string ToolId => "sda";

        public SdaToolConnector(SdaConnectorService sdaService)
        {
            _sdaService = sdaService ?? throw new ArgumentNullException(nameof(sdaService));
        }

        public async Task<bool> IsAvailableAsync()
        {
            return await _sdaService.TestConnectionAsync();
        }

        public async Task<Dictionary<string, object>> FetchDataAsync(Dictionary<string, string> parameters)
        {
            var result = new Dictionary<string, object>();

            if (parameters == null || !parameters.ContainsKey("action"))
            {
                throw new ArgumentException("Missing required 'action' parameter");
            }

            string action = parameters["action"];

            switch (action.ToLowerInvariant())
            {
                case "list_materials":
                    string category = parameters.ContainsKey("category") ? parameters["category"] : null;
                    var materials = await _sdaService.GetMaterialsAsync(category);
                    result["materials"] = materials;
                    result["count"] = materials.Count;
                    break;

                case "get_detail":
                    if (!parameters.ContainsKey("material_id"))
                    {
                        throw new ArgumentException("Missing required 'material_id' parameter for get_detail action");
                    }
                    string materialId = parameters["material_id"];
                    var detail = await _sdaService.GetMaterialDetailAsync(materialId);
                    result["detail"] = detail;
                    break;

                default:
                    throw new ArgumentException($"Unknown action: {action}");
            }

            return result;
        }
    }
}
