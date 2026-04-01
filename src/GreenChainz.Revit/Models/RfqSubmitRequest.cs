using System.Collections.Generic;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// RFQ request payload matching the greenchainz.com/api/rfq endpoint spec.
    /// </summary>
    public class RfqSubmitRequest
    {
        /// <summary>Project name from Revit ProjectInfo.</summary>
        [JsonProperty("projectName")]
        public string ProjectName { get; set; }

        /// <summary>Project location from Revit ProjectInfo.</summary>
        [JsonProperty("projectLocation")]
        public string ProjectLocation { get; set; }

        /// <summary>Architect name from Revit ProjectInfo.</summary>
        [JsonProperty("architectName")]
        public string ArchitectName { get; set; }

        /// <summary>Materials included in the RFQ.</summary>
        [JsonProperty("lineItems")]
        public List<RfqLineItem> LineItems { get; set; }

        public RfqSubmitRequest()
        {
            LineItems = new List<RfqLineItem>();
        }
    }

    /// <summary>
    /// Individual material line item in an RFQ submission.
    /// </summary>
    public class RfqLineItem
    {
        /// <summary>ID of the swap/alternative material being requested.</summary>
        [JsonProperty("materialId")]
        public string MaterialId { get; set; }

        /// <summary>Name of the swap/alternative material.</summary>
        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        /// <summary>Name of the current material being replaced.</summary>
        [JsonProperty("currentMaterialName")]
        public string CurrentMaterialName { get; set; }

        /// <summary>Quantity needed (volume or area).</summary>
        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        /// <summary>Unit of measure (m3, m2, count).</summary>
        [JsonProperty("unit")]
        public string Unit { get; set; }

        /// <summary>Current carbon impact in kgCO2e.</summary>
        [JsonProperty("currentCarbonImpact")]
        public double CurrentCarbonImpact { get; set; }

        /// <summary>Estimated carbon savings in kgCO2e.</summary>
        [JsonProperty("estimatedCarbonSavings")]
        public double EstimatedCarbonSavings { get; set; }

        /// <summary>JSON string of material specifications.</summary>
        [JsonProperty("specifications")]
        public string Specifications { get; set; }
    }
}
