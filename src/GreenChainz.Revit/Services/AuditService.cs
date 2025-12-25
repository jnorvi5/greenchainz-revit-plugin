using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    public class AuditService
    {
        private readonly Ec3ApiService _ec3Service;
        private Dictionary<string, Ec3CarbonFactor> _carbonFactorCache;

        public AuditService()
        {
            _ec3Service = App.Ec3Service;
            _carbonFactorCache = new Dictionary<string, Ec3CarbonFactor>();
        }

        public AuditResult ScanProject(Document doc)
        {
            List<MaterialBreakdown> materials = ExtractMaterials(doc);

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
                Summary = $"Analyzed {materials.Count} materials from your Revit model using EC3 data.",
                Materials = materials,
                Recommendations = GenerateRecommendations(materials),
                DataSource = _ec3Service?.HasValidApiKey == true ? "EC3 Building Transparency" : "CLF v2021 Baseline"
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
                            // Get carbon factor from EC3 or fallback
                            var carbonFactor = GetCarbonFactor(matName);
                            
                            materialMap[matName] = new MaterialBreakdown
                            {
                                MaterialName = matName,
                                Quantity = "0 m�",
                                CarbonFactor = carbonFactor.AverageGwp,
                                TotalCarbon = 0,
                                DataSource = carbonFactor.Source,
                                Ec3Category = carbonFactor.Ec3Category
                            };
                        }

                        double volumeM3 = volume * 0.0283168;
                        double currentQty = 0;
                        if (double.TryParse(materialMap[matName].Quantity.Replace(" m�", ""), out currentQty))
                        {
                            materialMap[matName].Quantity = $"{(currentQty + volumeM3):F2} m�";
                        }
                        materialMap[matName].TotalCarbon += volumeM3 * materialMap[matName].CarbonFactor;
                    }
                }
            }

            return new List<MaterialBreakdown>(materialMap.Values);
        }

        private Ec3CarbonFactor GetCarbonFactor(string materialName)
        {
            // Check cache first
            if (_carbonFactorCache.ContainsKey(materialName))
                return _carbonFactorCache[materialName];

            Ec3CarbonFactor factor;

            // Try to get from EC3 API
            if (_ec3Service?.HasValidApiKey == true)
            {
                try
                {
                    // Run synchronously for simplicity in Revit context
                    var task = Task.Run(() => _ec3Service.GetCarbonFactorAsync(materialName));
                    task.Wait(TimeSpan.FromSeconds(5));
                    factor = task.Result;
                }
                catch
                {
                    factor = GetFallbackFactor(materialName);
                }
            }
            else
            {
                factor = GetFallbackFactor(materialName);
            }

            _carbonFactorCache[materialName] = factor;
            return factor;
        }

        private Ec3CarbonFactor GetFallbackFactor(string materialName)
        {
            string name = materialName.ToLower();
            double gwp;
            string category;

            if (name.Contains("concrete"))
            {
                gwp = 340; category = "Concrete";
            }
            else if (name.Contains("steel"))
            {
                gwp = 1850; category = "Steel";
            }
            else if (name.Contains("aluminum") || name.Contains("aluminium"))
            {
                gwp = 8000; category = "Aluminum";
            }
            else if (name.Contains("glass"))
            {
                gwp = 1500; category = "Glass";
            }
            else if (name.Contains("wood") || name.Contains("timber"))
            {
                gwp = 110; category = "Wood";
            }
            else if (name.Contains("brick"))
            {
                gwp = 200; category = "Brick";
            }
            else if (name.Contains("gypsum") || name.Contains("drywall"))
            {
                gwp = 200; category = "Gypsum Board";
            }
            else if (name.Contains("insulation"))
            {
                gwp = 50; category = "Insulation";
            }
            else
            {
                gwp = 100; category = "Other";
            }

            return new Ec3CarbonFactor
            {
                Category = materialName,
                Ec3Category = category,
                AverageGwp = gwp,
                Unit = "kgCO2e/m�",
                Source = "CLF v2021 Baseline"
            };
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
                        PotentialSavings = mat.TotalCarbon * 0.3,
                        Ec3Link = "https://buildingtransparency.org/ec3/material-search?category=Concrete"
                    });
                }
                
                if (mat.MaterialName.ToLower().Contains("steel") && mat.TotalCarbon > 5000)
                {
                    recommendations.Add(new Recommendation
                    {
                        Description = $"Use recycled steel to reduce emissions from {mat.MaterialName}",
                        PotentialSavings = mat.TotalCarbon * 0.5,
                        Ec3Link = "https://buildingtransparency.org/ec3/material-search?category=Steel"
                    });
                }
            }

            if (recommendations.Count == 0)
            {
                recommendations.Add(new Recommendation
                {
                    Description = "Your project has a good carbon profile. Consider EPD-certified materials for documentation.",
                    PotentialSavings = 0,
                    Ec3Link = "https://buildingtransparency.org/ec3"
                });
            }

            return recommendations;
        }
    }
}
