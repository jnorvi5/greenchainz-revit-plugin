using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    public class AuditService
    {
        public AuditResult ScanProject(Document doc)
        {
            List<MaterialBreakdown> materials = ExtractMaterials(doc);

            // Calculate totals
            double totalCarbon = 0;
            foreach (var mat in materials)
            {
                totalCarbon += mat.TotalCarbon;
            }

            return new AuditResult
            {
                ProjectName = doc.Title,
                Date = DateTime.Now,
                OverallScore = totalCarbon,
                Summary = $"Analyzed {materials.Count} materials from your Revit model.",
                Materials = materials,
                Recommendations = GenerateRecommendations(materials)
            };
        }

        private List<MaterialBreakdown> ExtractMaterials(Document doc)
        {
            Dictionary<string, MaterialBreakdown> materialMap = new Dictionary<string, MaterialBreakdown>();

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
                foreach (ElementId matId in elem.GetMaterialIds(false))
                {
                    Autodesk.Revit.DB.Material mat = doc.GetElement(matId) as Autodesk.Revit.DB.Material;
                    if (mat == null) continue;

                    string matName = mat.Name;
                    double volume = elem.GetMaterialVolume(matId);

                    if (volume > 0.0001)
                    {
                        if (!materialMap.ContainsKey(matName))
                        {
                            // Estimate carbon factor based on material name
                            double carbonFactor = EstimateCarbonFactor(matName);
                            
                            materialMap[matName] = new MaterialBreakdown
                            {
                                MaterialName = matName,
                                Quantity = "0 m³",
                                CarbonFactor = carbonFactor,
                                TotalCarbon = 0
                            };
                        }

                        // Convert cubic feet to cubic meters
                        double volumeM3 = volume * 0.0283168;
                        double currentQty = 0;
                        if (double.TryParse(materialMap[matName].Quantity.Replace(" m³", ""), out currentQty))
                        {
                            materialMap[matName].Quantity = $"{(currentQty + volumeM3):F2} m³";
                        }
                        materialMap[matName].TotalCarbon += volumeM3 * materialMap[matName].CarbonFactor;
                    }
                }
            }

            return new List<MaterialBreakdown>(materialMap.Values);
        }

        private double EstimateCarbonFactor(string materialName)
        {
            string name = materialName.ToLower();
            
            if (name.Contains("concrete")) return 240;
            if (name.Contains("steel")) return 1850;
            if (name.Contains("aluminum") || name.Contains("aluminium")) return 8000;
            if (name.Contains("glass")) return 25;
            if (name.Contains("wood") || name.Contains("timber")) return 10;
            if (name.Contains("brick")) return 200;
            if (name.Contains("insulation")) return 50;
            
            return 100; // Default estimate
        }

        private List<Recommendation> GenerateRecommendations(List<MaterialBreakdown> materials)
        {
            var recommendations = new List<Recommendation>();

            foreach (var mat in materials)
            {
                if (mat.MaterialName.ToLower().Contains("concrete") && mat.TotalCarbon > 10000)
                {
                    recommendations.Add(new Recommendation
                    {
                        Description = $"Consider low-carbon concrete alternatives for {mat.MaterialName}",
                        PotentialSavings = mat.TotalCarbon * 0.3
                    });
                }
                
                if (mat.MaterialName.ToLower().Contains("steel") && mat.TotalCarbon > 5000)
                {
                    recommendations.Add(new Recommendation
                    {
                        Description = $"Use recycled steel to reduce emissions from {mat.MaterialName}",
                        PotentialSavings = mat.TotalCarbon * 0.5
                    });
                }
            }

            if (recommendations.Count == 0)
            {
                recommendations.Add(new Recommendation
                {
                    Description = "Your project has a good carbon profile. Consider EPD-certified materials.",
                    PotentialSavings = 0
                });
            }

            return recommendations;
        }
    }
}
