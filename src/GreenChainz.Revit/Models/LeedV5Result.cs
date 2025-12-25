using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// LEED v5 BD+C Analysis Result
    /// </summary>
    public class LeedV5Result
    {
        public string ProjectName { get; set; }
        public DateTime CalculationDate { get; set; }
        public string Version { get; set; }
        public int TotalPoints { get; set; }
        public int MaxPoints { get; set; }
        public int PrerequisitesMet { get; set; }
        public int PrerequisitesTotal { get; set; }
        public string CertificationLevel { get; set; }
        public List<LeedV5Category> Categories { get; set; }

        public LeedV5Result()
        {
            Categories = new List<LeedV5Category>();
        }

        public double PercentScore => MaxPoints > 0 ? (double)TotalPoints / MaxPoints * 100 : 0;
        public string PointsDisplay => $"{TotalPoints}/{MaxPoints}";
        public string PrereqDisplay => $"{PrerequisitesMet}/{PrerequisitesTotal}";
    }

    public class LeedV5Category
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public List<LeedV5Credit> Credits { get; set; }

        public LeedV5Category()
        {
            Credits = new List<LeedV5Credit>();
        }

        public int TotalPoints
        {
            get
            {
                int sum = 0;
                foreach (var c in Credits)
                    if (!c.IsPrerequisite) sum += c.PointsEarned;
                return sum;
            }
        }

        public int MaxPoints
        {
            get
            {
                int sum = 0;
                foreach (var c in Credits)
                    if (!c.IsPrerequisite) sum += c.MaxPoints;
                return sum;
            }
        }
    }

    public class LeedV5Credit
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsPrerequisite { get; set; }
        public int MaxPoints { get; set; }
        public int PointsEarned { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }

        public string PointsDisplay => IsPrerequisite ? "Required" : $"{PointsEarned}/{MaxPoints}";
        public string FullName => $"{Code}: {Name}";
    }
}
