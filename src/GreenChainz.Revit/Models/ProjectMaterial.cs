namespace GreenChainz.Revit.Models
{
    public class ProjectMaterial
    {
        public string Name { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; } // e.g., "cubic ft", "sq ft"
        public string Category { get; set; } // e.g., "Wall", "Floor"
    }
}
