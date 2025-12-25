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
        public List<MaterialBreakdown> Materials { get; set; }
        public List<Recommendation> Recommendations { get; set; }

        public AuditResult()
        {
            Date = DateTime.Now;
            Materials = new List<MaterialBreakdown>();
            Recommendations = new List<Recommendation>();
        }
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
