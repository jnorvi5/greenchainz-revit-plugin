using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents the response from a carbon audit submission.
    /// </summary>
    public class AuditResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for this audit.
        /// </summary>
        public string AuditId { get; set; }

        /// <summary>
        /// Gets or sets the total embodied carbon for all materials in kgCO2e.
        /// </summary>
        public double TotalEmbodiedCarbon { get; set; }

        /// <summary>
        /// Gets or sets the detailed results for each material.
        /// </summary>
        public List<MaterialAuditResult> Results { get; set; }

        /// <summary>
        /// Gets or sets the list of recommendations to reduce carbon footprint.
        /// </summary>
        public List<string> Recommendations { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the audit was processed.
        /// </summary>
        public DateTime ProcessedAt { get; set; }
    }
}
