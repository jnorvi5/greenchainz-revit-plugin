using Newtonsoft.Json;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents a lower-carbon alternative material for swapping.
    /// </summary>
    public class SwapAlternative
    {
        /// <summary>Unique material identifier.</summary>
        [JsonProperty("materialId")]
        public string MaterialId { get; set; }

        /// <summary>Alternative material name.</summary>
        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        /// <summary>Manufacturer of the alternative material.</summary>
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        /// <summary>Carbon impact per unit volume (kgCO2e per m3).</summary>
        [JsonProperty("carbonPerUnit")]
        public double CarbonPerUnit { get; set; }

        /// <summary>Carbon savings as a percentage vs current material.</summary>
        [JsonProperty("carbonSavingsPercent")]
        public double CarbonSavingsPercent { get; set; }

        /// <summary>Compressive strength comparison (e.g. "4000 psi").</summary>
        [JsonProperty("compressiveStrength")]
        public string CompressiveStrength { get; set; }

        /// <summary>Fire rating comparison (e.g. "Class A", "1-Hour").</summary>
        [JsonProperty("fireRating")]
        public string FireRating { get; set; }

        /// <summary>Estimated cost delta as percentage (negative = cheaper).</summary>
        [JsonProperty("costDeltaPercent")]
        public double CostDeltaPercent { get; set; }

        /// <summary>Whether the material has a verified EPD.</summary>
        [JsonProperty("epdVerified")]
        public bool EpdVerified { get; set; }

        /// <summary>Whether this is the recommended "best swap" option.</summary>
        public bool IsBestSwap { get; set; }

        /// <summary>Whether the user has selected this alternative for RFQ.</summary>
        public bool IsSelectedForRfq { get; set; }

        // Display helpers
        /// <summary>Formatted carbon savings display.</summary>
        public string CarbonSavingsDisplay => $"{CarbonSavingsPercent:F0}%";

        /// <summary>Formatted cost delta display.</summary>
        public string CostDeltaDisplay => CostDeltaPercent < 0
            ? $"{CostDeltaPercent:F0}% cheaper"
            : CostDeltaPercent > 0
                ? $"+{CostDeltaPercent:F0}% more"
                : "Same cost";

        /// <summary>EPD status display text.</summary>
        public string EpdStatusDisplay => EpdVerified ? "Verified" : "Unverified";

        /// <summary>Carbon per unit display.</summary>
        public string CarbonPerUnitDisplay => $"{CarbonPerUnit:N0} kgCO\u2082e/m\u00B3";
    }
}
