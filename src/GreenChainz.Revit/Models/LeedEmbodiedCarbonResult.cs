using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// LEED v4.1 MRpc132 Embodied Carbon Result
    /// Per UW Carbon Leadership Forum methodology
    /// </summary>
    public class LeedEmbodiedCarbonResult
    {
        public string ProjectName { get; set; }
        public DateTime CalculationDate { get; set; }
        public string BaselineYear { get; set; }
        
        // Building Embodied Carbon Intensity
        public double bECIb { get; set; }  // Baseline (kgCO2e/sf)
        public double bECIa { get; set; }  // Actual (kgCO2e/sf)
        
        public double TotalBaselineCarbon { get; set; }  // Total kgCO2e baseline
        public double TotalActualCarbon { get; set; }    // Total kgCO2e actual
        public double BuildingAreaSF { get; set; }
        
        public double PercentReduction { get; set; }
        public int PointsEarned { get; set; }  // 0, 1, or 2
        public string CreditStatus { get; set; }
        
        public List<LeedMaterialCarbon> Materials { get; set; }

        public LeedEmbodiedCarbonResult()
        {
            Materials = new List<LeedMaterialCarbon>();
        }

        /// <summary>
        /// Gets display string for points
        /// </summary>
        public string PointsDisplay => $"{PointsEarned}/2";

        /// <summary>
        /// Gets certification path recommendation
        /// </summary>
        public string GetRecommendation()
        {
            if (PercentReduction >= 30)
                return "Excellent! You qualify for 2 points. Document with EPDs.";
            else if (PercentReduction >= 20)
                return "Close to 2 points! Consider low-carbon concrete to reach 30% reduction.";
            else if (PercentReduction > 0)
                return "You qualify for 1 point. Target 30% reduction for maximum points.";
            else
                return "No reduction achieved. Specify low-carbon alternatives.";
        }
    }

    /// <summary>
    /// Individual material carbon data per LEED MRpc132
    /// </summary>
    public class LeedMaterialCarbon
    {
        public string MaterialCategory { get; set; }
        public string MaterialName { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        
        // Baseline (from CLF)
        public double BaselineECI { get; set; }  // mECIb
        public string BaselineUnit { get; set; }
        public double BaselineCarbon { get; set; }
        
        // Actual (from EPD or estimate)
        public double ActualECI { get; set; }    // mECIa
        public double ActualCarbon { get; set; }
        
        // EPD Info
        public bool HasEPD { get; set; }
        public string EPDSource { get; set; }
        
        /// <summary>
        /// Percent reduction for this material
        /// </summary>
        public double PercentReduction => BaselineCarbon > 0 
            ? ((BaselineCarbon - ActualCarbon) / BaselineCarbon) * 100 
            : 0;

        /// <summary>
        /// Display quantity with unit
        /// </summary>
        public string QuantityDisplay => $"{Quantity:F2} {Unit}";

        /// <summary>
        /// Display baseline carbon
        /// </summary>
        public string BaselineCarbonDisplay => $"{BaselineCarbon:N0} kgCO2e";

        /// <summary>
        /// Display actual carbon
        /// </summary>
        public string ActualCarbonDisplay => $"{ActualCarbon:N0} kgCO2e";
    }
}
