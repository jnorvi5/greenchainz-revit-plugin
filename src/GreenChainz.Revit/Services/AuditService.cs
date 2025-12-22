using System.Collections.Generic;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    public class AuditService
    {
        private readonly ApiClient _apiClient;

        public AuditService()
        {
            _apiClient = new ApiClient();
        }

        public AuditResult ScanProject(Document doc)
        {
            List<ProjectMaterial> materials = ExtractMaterials(doc);

            AuditRequest request = new AuditRequest
            {
                ProjectName = doc.Title,
                Materials = materials
            };

            return _apiClient.SubmitAudit(request);
        }

        private List<ProjectMaterial> ExtractMaterials(Document doc)
        {
            // Use a dictionary to aggregate quantities by material name
            Dictionary<string, ProjectMaterial> materialMap = new Dictionary<string, ProjectMaterial>();

            // Collect all elements that might have materials.
            // This is a broad collection; we might want to filter by categories like Walls, Floors, Roofs, etc.
            // For now, let's target common building elements.
            List<BuiltInCategory> categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Doors
            };

            ElementMulticategoryFilter categoryFilter = new ElementMulticategoryFilter(categories);

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> elements = collector.WherePasses(categoryFilter).WhereElementIsNotElementType().ToElements();

            foreach (Element elem in elements)
            {
                // Retrieve materials from the element
                foreach (ElementId matId in elem.GetMaterialIds(false))
                {
                    Material mat = doc.GetElement(matId) as Material;
                    if (mat == null) continue;

                    string matName = mat.Name;
                    double volume = elem.GetMaterialVolume(matId); // Cubic feet internal units

                    // We could also get Area if needed, but Volume is often primary for mass calculation.
                    // double area = elem.GetMaterialArea(matId);

                    if (volume > 0.0001) // Filter out negligible amounts
                    {
                        if (!materialMap.ContainsKey(matName))
                        {
                            materialMap[matName] = new ProjectMaterial
                            {
                                Name = matName,
                                Quantity = 0,
                                Unit = "cubic ft", // Revit internal unit for volume
                                Category = elem.Category?.Name ?? "Unknown"
                            };
                        }

                        materialMap[matName].Quantity += volume;
                    }
                }
            }

            return new List<ProjectMaterial>(materialMap.Values);
        }
    }
}
