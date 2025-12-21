namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents an item in a Request for Quotation (RFQ).
    /// </summary>
    public class RfqItem
    {
        /// <summary>
        /// Gets or sets the name of the material.
        /// </summary>
        public string MaterialName { get; set; }

        /// <summary>
        /// Gets or sets the category of the material.
        /// </summary>
        public string MaterialCategory { get; set; }

        /// <summary>
        /// Gets or sets the quantity required.
        /// </summary>
        public double Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets any specific specifications or requirements for the material.
        /// </summary>
        public string Specifications { get; set; }
    }
}
