using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    public class AuditRequest
    {
        public string ProjectName { get; set; }
        public List<ProjectMaterial> Materials { get; set; }
    /// <summary>
    /// Represents a request to audit the carbon footprint of a Revit project.
    /// </summary>
    public class AuditRequest
    {
        /// <summary>
        /// Gets or sets the name of the project.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the project.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the list of materials used in the project.
        /// </summary>
        public List<MaterialUsage> Materials { get; set; }

        /// <summary>
        /// Gets or sets the date when the audit was performed.
        /// </summary>
        public DateTime AuditDate { get; set; }

        /// <summary>
        /// Gets or sets the version of Revit used.
        /// </summary>
        public string RevitVersion { get; set; }
    }
}
