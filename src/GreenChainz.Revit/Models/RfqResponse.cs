using System;

namespace GreenChainz.Revit.Models
{
    /// <summary>
    /// Represents the response from an RFQ submission.
    /// </summary>
    public class RfqResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for the RFQ.
        /// </summary>
        public string RfqId { get; set; }

        /// <summary>
        /// Gets or sets the status of the RFQ (e.g., "submitted", "processing").
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the RFQ was submitted.
        /// </summary>
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of suppliers notified about this RFQ.
        /// </summary>
        public int SuppliersNotified { get; set; }

        /// <summary>
        /// Gets or sets a message about the RFQ submission.
        /// </summary>
        public string Message { get; set; }
    }
}
