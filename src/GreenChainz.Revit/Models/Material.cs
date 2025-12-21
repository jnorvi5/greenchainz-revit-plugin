using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents a sustainable building material from the GreenChainz database.
    /// </summary>
    public class Material
    {
        /// <summary>
        /// Gets or sets the unique identifier of the material.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the material.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the category of the material.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the description of the material.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer of the material.
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the embodied carbon in kgCO2e per unit.
        /// </summary>
        public double EmbodiedCarbon { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets the list of certifications (e.g., LEED, EPD, Cradle to Cradle).
        /// </summary>
        public List<string> Certifications { get; set; }

        /// <summary>
        /// Gets or sets whether the material has been verified by GreenChainz.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the URL of the material image.
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the price per unit (optional).
        /// </summary>
        public decimal? PricePerUnit { get; set; }
    }
}
