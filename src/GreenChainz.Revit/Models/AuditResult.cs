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

        public AuditResult()
        {
            Date = DateTime.Now;
            Materials = new List<MaterialBreakdown>();
            Recommendations = new List<Recommendation>();
            DataSource = "CLF v2021 Baseline";
        }
    }

    /// <summary>
    /// Represents a material breakdown with carbon data.
    /// </summary>
    public class MaterialBreakdown
    {
        public string MaterialName { get; set; }
        public string Quantity { get; set; }
        public double CarbonFactor { get; set; }
        public double TotalCarbon { get; set; }
        public string DataSource { get; set; }
        public string Ec3Category { get; set; }
        
        public string CarbonFactorDisplay => $"{CarbonFactor:N2}";
        public string TotalCarbonDisplay => $"{TotalCarbon:N0}";
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
    }
}
