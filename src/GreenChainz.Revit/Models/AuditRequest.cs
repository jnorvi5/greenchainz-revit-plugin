using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    public class AuditRequest
    {
        public string ProjectName { get; set; }
        public List<ProjectMaterial> Materials { get; set; }
    }
}
