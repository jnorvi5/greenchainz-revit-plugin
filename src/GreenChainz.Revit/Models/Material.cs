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
        /// Helper property for XAML binding to display certifications as a comma-separated string.
        /// </summary>
        public string CertificationsDisplay => Certifications != null ? string.Join(", ", Certifications) : string.Empty;

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

        // Engineering specs for swap engine matching

        /// <summary>
        /// Gets or sets the compressive strength in MPa.
        /// </summary>
        public double CompressiveStrength { get; set; }

        /// <summary>
        /// Gets or sets the density in kg/m³.
        /// </summary>
        public double Density { get; set; }

        /// <summary>
        /// Gets or sets the fire rating (e.g. "Class A", "1-Hour").
        /// </summary>
        public string FireRating { get; set; }

        /// <summary>
        /// Gets or sets the thermal conductivity in W/(m·K).
        /// </summary>
        public double ThermalConductivity { get; set; }

        /// <summary>
        /// Gets or sets the cost per unit in USD.
        /// </summary>
        public double CostPerUnit { get; set; }

        /// <summary>
        /// Gets or sets the life expectancy (e.g. "50+ years").
        /// </summary>
        public string LifeExpectancy { get; set; }

        /// <summary>
        /// Gets or sets the recycled content as a percentage (0–100).
        /// </summary>
        public double RecycledContent { get; set; }

        /// <summary>
        /// Gets or sets the shipping origin region.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Returns true when the material sequesters more carbon than it emits (EmbodiedCarbon &lt; 0).
        /// Used for carbon score color-coding in the UI.
        /// </summary>
        public bool IsCarbonNegative => EmbodiedCarbon < 0;
    }
}
