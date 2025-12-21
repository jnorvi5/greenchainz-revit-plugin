using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents the response from the materials API endpoint.
    /// </summary>
    public class MaterialsResponse
    {
        /// <summary>
        /// Gets or sets the list of materials.
        /// </summary>
        public List<Material> Materials { get; set; }

        /// <summary>
        /// Gets or sets the total count of materials matching the query.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the current page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size (number of items per page).
        /// </summary>
        public int PageSize { get; set; }
    }
}
