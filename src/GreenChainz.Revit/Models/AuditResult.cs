using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    public class AuditResult
    {
        public double CarbonScore { get; set; }
        public string Rating { get; set; } // e.g., "A", "B", "C"
        public List<string> Recommendations { get; set; }
    }
}
