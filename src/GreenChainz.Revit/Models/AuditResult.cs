using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents the result of a carbon audit analysis.
    /// </summary>
    public class AuditResult
    {
        public string ProjectName { get; set; }
        public DateTime Date { get; set; }
        public double OverallScore { get; set; }
        public string Summary { get; set; }
        public string DataSource { get; set; }
        public List<MaterialBreakdown> Materials { get; set; }
        public List<Recommendation> Recommendations { get; set; }

        // IFC Export Support
        public string IfcProjectGuid { get; set; }
        public string IfcSchema { get; set; } = "IFC4";

        public AuditResult()
        {
            Date = DateTime.Now;
            Materials = new List<MaterialBreakdown>();
            Recommendations = new List<Recommendation>();
            DataSource = "CLF v2021 Baseline";
            IfcProjectGuid = Guid.NewGuid().ToString("N").ToUpper();
        }
    }

    /// <summary>
    /// Represents a material breakdown with carbon data and IFC mapping.
    /// IFC-compliant for interoperability with open BIM tools.
    /// </summary>
    public class MaterialBreakdown
    {
        public string MaterialName { get; set; }
        public string Quantity { get; set; }
        public double CarbonFactor { get; set; }
        public double TotalCarbon { get; set; }
        public string DataSource { get; set; }
        public string Ec3Category { get; set; }

        // IFC Standards Support - for openBIM interoperability
        /// <summary>
        /// IFC GlobalId (22-character base64 GUID) for cross-software tracking
        /// </summary>
        public string IfcGuid { get; set; }

        /// <summary>
        /// IFC Export Type (e.g., IfcMaterial, IfcMaterialLayer, IfcBuildingElementProxy)
        /// </summary>
        public string IfcExportAs { get; set; }

        /// <summary>
        /// IFC Category mapping (e.g., IfcWall, IfcSlab, IfcColumn)
        /// </summary>
        public string IfcCategory { get; set; }

        /// <summary>
        /// Revit Element ID for traceability
        /// </summary>
        public string RevitElementId { get; set; }

        /// <summary>
        /// Volume in cubic meters (IFC standard unit)
        /// </summary>
        public double VolumeM3 { get; set; }

        /// <summary>
        /// Mass in kilograms (IFC standard unit)
        /// </summary>
        public double MassKg { get; set; }

        // Display helpers
        public string CarbonFactorDisplay => $"{CarbonFactor:N2}";
        public string TotalCarbonDisplay => $"{TotalCarbon:N0}";
        public string IfcDisplay => !string.IsNullOrEmpty(IfcGuid) ? $"{IfcCategory} [{IfcGuid.Substring(0, 8)}...]" : "Not mapped";

        public MaterialBreakdown()
        {
            // Generate IFC-compliant GUID
            IfcGuid = GenerateIfcGuid();
            IfcExportAs = "IfcMaterial";
        }

        /// <summary>
        /// Generates an IFC-compliant 22-character base64 GUID
        /// </summary>
        private static string GenerateIfcGuid()
        {
            var guid = Guid.NewGuid();
            return ConvertToIfcGuid(guid);
        }

        /// <summary>
        /// Converts a .NET GUID to IFC base64 format (22 chars)
        /// </summary>
        public static string ConvertToIfcGuid(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            string base64 = Convert.ToBase64String(bytes);
            // IFC uses modified base64: replace + with $ and / with _
            return base64.Replace('+', '$').Replace('/', '_').TrimEnd('=');
        }
    }

    /// <summary>
    /// Represents a recommendation for carbon reduction.
    /// </summary>
    public class Recommendation
    {
        public string Description { get; set; }
        public double PotentialSavings { get; set; }
        public string Ec3Link { get; set; }
        
        public string SavingsDisplay => PotentialSavings > 0 
            ? $"Potential Savings: {PotentialSavings:N0} kgCO2e" 
            : "";
    }

    /// <summary>
    /// Represents a request for carbon audit analysis.
    /// </summary>
    public class AuditRequest
    {
        public string ProjectName { get; set; }
        public string ModelPath { get; set; }
        public List<MaterialInput> Materials { get; set; }

        public AuditRequest()
        {
            Materials = new List<MaterialInput>();
        }
    }

    /// <summary>
    /// Represents material input data for audit requests.
    /// </summary>
    public class MaterialInput
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        public string IfcCategory { get; set; }
        public string IfcGuid { get; set; }
    }

    /// <summary>
    /// Material comparison for swap feature - Current vs Recommended
    /// </summary>
    public class MaterialComparison
    {
        public string MaterialName { get; set; }
        public string SupplierName { get; set; }
        public double GwpValue { get; set; }
        public double Distance { get; set; }
        public double CarbonSavings { get; set; }
        public string LeedImpact { get; set; }
        public string EpdId { get; set; }
        public string EpdUrl { get; set; }
        public bool HasEpd { get; set; }
        public List<string> Certifications { get; set; }

        // Display helpers
        public string GwpDisplay => $"{GwpValue:N0} kgCO2e";
        public string SavingsDisplay => CarbonSavings > 0 ? $"{CarbonSavings:F0}% Lower Carbon" : "No Savings";
        public string DistanceDisplay => Distance > 0 ? $"{Distance:N0} miles" : "Global";
        public string CertificationsDisplay => Certifications != null ? string.Join(", ", Certifications) : "";

        public MaterialComparison()
        {
            Certifications = new List<string>();
        }
    }

    /// <summary>
    /// Response from carbon comparison API
    /// </summary>
    public class AuditResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public MaterialComparison Original { get; set; }
        public MaterialComparison BestAlternative { get; set; }
        public List<MaterialComparison> Alternatives { get; set; }
        public string DataSource { get; set; }

        public AuditResponse()
        {
            Alternatives = new List<MaterialComparison>();
        }

        public double TotalSavings => Original != null && BestAlternative != null 
            ? Original.GwpValue - BestAlternative.GwpValue 
            : 0;

        public double SavingsPercent => Original != null && Original.GwpValue > 0 && BestAlternative != null
            ? ((Original.GwpValue - BestAlternative.GwpValue) / Original.GwpValue) * 100
            : 0;
    }
}
