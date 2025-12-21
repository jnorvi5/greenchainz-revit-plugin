using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents a Request for Quotation (RFQ) to be submitted to suppliers.
    /// </summary>
    public class RfqRequest
    {
        /// <summary>
        /// Gets or sets the name of the project.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets the email address of the contact person.
        /// </summary>
        public string ContactEmail { get; set; }

        /// <summary>
        /// Gets or sets the name of the contact person.
        /// </summary>
        public string ContactName { get; set; }

        /// <summary>
        /// Gets or sets the list of items requested in the RFQ.
        /// </summary>
        public List<RfqItem> Items { get; set; }

        /// <summary>
        /// Gets or sets any additional notes or requirements.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets the date by which the materials are required.
        /// </summary>
        public DateTime RequiredDate { get; set; }
    }
}
