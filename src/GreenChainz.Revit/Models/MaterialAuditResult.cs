using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents the audit result for a specific material in the project.
    /// </summary>
    public class MaterialAuditResult
    {
        /// <summary>
        /// Gets or sets the name of the material.
        /// </summary>
        public string MaterialName { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the material used.
        /// </summary>
        public double Quantity { get; set; }

        /// <summary>
        /// Gets or sets the embodied carbon for this material (kgCO2e).
        /// </summary>
        public double EmbodiedCarbon { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets the list of sustainable alternatives for this material.
        /// </summary>
        public List<string> SustainableAlternatives { get; set; }
    }
}
