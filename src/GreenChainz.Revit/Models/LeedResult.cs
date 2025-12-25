using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents the results of a LEED certification analysis.
    /// </summary>
    public class LeedResult
    {
        public string ProjectName { get; set; }
        public DateTime CalculationDate { get; set; }
        public int TotalPoints { get; set; }
        public int MaxPossiblePoints { get; set; }
        public string CertificationLevel { get; set; }
        public List<LeedCredit> Credits { get; set; }

        public LeedResult()
        {
            Credits = new List<LeedCredit>();
        }

        /// <summary>
        /// Gets the percentage of points earned.
        /// </summary>
        public double PercentageScore => MaxPossiblePoints > 0 
            ? (double)TotalPoints / MaxPossiblePoints * 100 
            : 0;
    }

    /// <summary>
    /// Represents a single LEED credit category.
    /// </summary>
    public class LeedCredit
    {
        public string Category { get; set; }
        public string CreditName { get; set; }
        public int PointsEarned { get; set; }
        public int MaxPoints { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }

        /// <summary>
        /// Display string for the points (e.g., "2/3").
        /// </summary>
        public string PointsDisplay => $"{PointsEarned}/{MaxPoints}";
    }
}
