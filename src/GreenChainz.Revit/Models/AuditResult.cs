using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
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
            Materials = new List<MaterialBreakdown>();
            Recommendations = new List<Recommendation>();
        }
    }
}
