using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Enterprise-Grade Sustainability Scorecard
    /// Focus: EPD, GWP, Verification Tier - What architects ACTUALLY need
    /// </summary>
    public class SustainabilityScorecard
    {
        public string ProjectName { get; set; }
        public DateTime GeneratedDate { get; set; }
        
        // Location
        public ProjectLocationInfo Location { get; set; }
        
        // Overall Scores
        public string OverallGrade { get; set; }  // A, B, C, D, F
        public int OverallScore { get; set; }      // 0-100
        
        // The Three Metrics That Matter
        public EpdCoverage EpdScore { get; set; }
        public EmbodiedCarbonScore GwpScore { get; set; }
        public VerificationTier VerificationScore { get; set; }
        
        // Material breakdown
        public List<MaterialScore> Materials { get; set; }
        
        // Compliance
        public bool LeedCompliant { get; set; }
        public bool BuyCleanCompliant { get; set; }
        public int EstimatedLeedPoints { get; set; }
        public int RegionalBonusPoints { get; set; }
        public List<string> RegionalPriorityCredits { get; set; }
        public BuyCleanComplianceInfo BuyCleanInfo { get; set; }

        public SustainabilityScorecard()
        {
            Materials = new List<MaterialScore>();
            RegionalPriorityCredits = new List<string>();
            GeneratedDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Project location info for scorecard
    /// </summary>
    public class ProjectLocationInfo
    {
        public string Address { get; set; } = "";
        public string State { get; set; } = "Unknown";
        public string Region { get; set; } = "Unknown";
        public string ClimateZone { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double GridCarbonIntensity { get; set; }
        public double RegionalMultiplier { get; set; } = 1.0;
        
        public string Display => State != "Unknown" ? $"{State}, {Region}" : "Location Not Set";
        public string GridDisplay => $"{GridCarbonIntensity:N0} lbs CO2/MWh";
    }

    /// <summary>
    /// Buy Clean compliance info
    /// </summary>
    public class BuyCleanComplianceInfo
    {
        public bool HasRequirements { get; set; }
        public string PolicyName { get; set; } = "";
        public bool ConcreteCompliant { get; set; }
        public bool SteelCompliant { get; set; }
        public double ConcreteLimit { get; set; }
        public double SteelLimit { get; set; }
        public double ActualConcreteGwp { get; set; }
        public double ActualSteelGwp { get; set; }
    }

    /// <summary>
    /// EPD Coverage - Do materials have Environmental Product Declarations?
    /// </summary>
    public class EpdCoverage
    {
        public int TotalMaterials { get; set; }
        public int MaterialsWithEpd { get; set; }
        public double CoveragePercent { get; set; }
        public string Grade { get; set; }  // A (>80%), B (>60%), C (>40%), D (>20%), F (<20%)
        
        public string Display => $"{CoveragePercent:F0}% EPD Coverage";
        public string Status => CoveragePercent >= 50 ? "LEED Compliant" : "Below Threshold";
    }

    /// <summary>
    /// Embodied Carbon (GWP) - Global Warming Potential
    /// </summary>
    public class EmbodiedCarbonScore
    {
        public double TotalGwp { get; set; }           // kgCO2e total
        public double GwpPerSqFt { get; set; }         // kgCO2e/sf intensity
        public double GwpPerSqM { get; set; }          // kgCO2e/m²
        public double BaselineGwp { get; set; }        // Industry baseline
        public double ReductionPercent { get; set; }   // % below baseline
        public string Grade { get; set; }
        public double RegionalAdjustedGwp { get; set; }   
        
        public string Display => $"{TotalGwp:N0} kgCO2e";
        public string IntensityDisplay => $"{GwpPerSqM:F1} kgCO2e/m²";
        public string ReductionDisplay => ReductionPercent > 0 
            ? $"{ReductionPercent:F0}% below baseline" 
            : $"{Math.Abs(ReductionPercent):F0}% above baseline";
    }

    /// <summary>
    /// Verification Tier - Level of third-party verification
    /// </summary>
    public class VerificationTier
    {
        public string Tier { get; set; }  // Platinum, Gold, Silver, Unverified
        public int TierScore { get; set; } // 3, 2, 1, 0
        
        public int PlatinumCount { get; set; }  // Third-party verified + chain of custody
        public int GoldCount { get; set; }       // EPD available
        public int SilverCount { get; set; }     // Self-declared only
        public int UnverifiedCount { get; set; } // No verification
        
        public string Display => $"{Tier} Verification";
        public string Breakdown => $"P:{PlatinumCount} | G:{GoldCount} | S:{SilverCount} | N:{UnverifiedCount}";
    }

    /// <summary>
    /// Individual material score
    /// </summary>
    public class MaterialScore
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        
        // EPD
        public bool HasEpd { get; set; }
        public string EpdId { get; set; }
        public string EpdUrl { get; set; }
        
        // GWP
        public double Gwp { get; set; }           // kgCO2e per unit
        public double TotalGwp { get; set; }      // Total for this material
        public double GwpBaseline { get; set; }   // Industry baseline
        public double GwpReduction { get; set; } // % vs baseline
        public double RegionalGwp { get; set; }     // Adjusted for regional factors
        
        // Verification
        public string VerificationTier { get; set; }  // Platinum, Gold, Silver, None
        public List<string> Certifications { get; set; }
        
        // Regional
        public bool MeetsBuyClean { get; set; }   // Compliance with Buy Clean standards
        public double DistanceFromProject { get; set; }  // Transport distance impact
        
        // Display
        public string GwpDisplay => $"{Gwp:F1} kgCO2e/{Unit}";
        public string TotalGwpDisplay => $"{TotalGwp:N0} kgCO2e";
        public string TierDisplay => VerificationTier;
        public string EpdStatus => HasEpd ? "EPD Available" : "No EPD";
        public string DistanceDisplay => DistanceFromProject > 0 ? $"{DistanceFromProject:N0} mi" : "-";

        public MaterialScore()
        {
            Certifications = new List<string>();
        }
    }
}
