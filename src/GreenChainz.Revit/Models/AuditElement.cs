using System;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents a single Revit element with its carbon impact data for per-element audit breakdown.
    /// </summary>
    public class AuditElement
    {
        /// <summary>Revit ElementId for traceability.</summary>
        [JsonProperty("elementId")]
        public int ElementId { get; set; }

        /// <summary>Element name from Revit (type name or family name).</summary>
        [JsonProperty("elementName")]
        public string ElementName { get; set; }

        /// <summary>Revit category (Wall, Floor, Roof, Column, etc.).</summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>Material name applied to this element.</summary>
        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        /// <summary>Material identifier for API lookups.</summary>
        [JsonProperty("materialId")]
        public string MaterialId { get; set; }

        /// <summary>Volume in cubic meters.</summary>
        [JsonProperty("volume")]
        public double Volume { get; set; }

        /// <summary>Area in square meters.</summary>
        [JsonProperty("area")]
        public double Area { get; set; }

        /// <summary>Total carbon impact in kgCO2e.</summary>
        [JsonProperty("carbonImpact")]
        public double CarbonImpact { get; set; }

        /// <summary>Carbon intensity per unit volume (kgCO2e per m3).</summary>
        [JsonProperty("carbonPerUnit")]
        public double CarbonPerUnit { get; set; }

        /// <summary>Whether lower-carbon swap alternatives are available.</summary>
        [JsonProperty("hasAlternatives")]
        public bool HasAlternatives { get; set; }

        // Display helpers
        /// <summary>Formatted volume display.</summary>
        public string VolumeDisplay => Volume > 0 ? $"{Volume:F3} m\u00B3" : "-";

        /// <summary>Formatted area display.</summary>
        public string AreaDisplay => Area > 0 ? $"{Area:F2} m\u00B2" : "-";

        /// <summary>Formatted carbon impact display.</summary>
        public string CarbonImpactDisplay => $"{CarbonImpact:N1}";

        /// <summary>Formatted carbon per unit display.</summary>
        public string CarbonPerUnitDisplay => $"{CarbonPerUnit:N0}";
    }
}
