namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// CCPS (Carbon & Compliance Performance Score) breakdown for a material.
    /// Used by the CCPS Scorecard command and window.
    /// </summary>
    public class CcpsBreakdown
    {
        public double CcpsTotal { get; set; }
        public double CarbonScore { get; set; }
        public double ComplianceScore { get; set; }
        public double CertificationScore { get; set; }
        public double CostScore { get; set; }
        public double SupplyChainScore { get; set; }
        public double HealthScore { get; set; }
    }
}
