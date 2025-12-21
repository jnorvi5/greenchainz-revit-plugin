using System;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents material usage data extracted from a Revit element.
    /// </summary>
    public class MaterialUsage
    {
        /// <summary>
        /// Gets or sets the unique identifier of the Revit element.
        /// </summary>
        public string ElementId { get; set; }

        /// <summary>
        /// Gets or sets the type of the Revit element (e.g., Wall, Floor, Beam).
        /// </summary>
        public string ElementType { get; set; }

        /// <summary>
        /// Gets or sets the name of the material.
        /// </summary>
        public string MaterialName { get; set; }

        /// <summary>
        /// Gets or sets the category of the material (e.g., Concrete, Steel, Wood).
        /// </summary>
        public string MaterialCategory { get; set; }

        /// <summary>
        /// Gets or sets the volume of material in cubic meters.
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// Gets or sets the area of material in square meters.
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        public string Unit { get; set; }
    }
}
