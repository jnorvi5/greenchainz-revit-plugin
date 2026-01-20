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

            // Expanded list of categories architects care about
            List<BuiltInCategory> categories = new List<BuiltInCategory>
            {
                // Structure
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_StructuralFoundation,
                
                // Openings
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_CurtainWallPanels,
                BuiltInCategory.OST_CurtainWallMullions,
                
                // Interior
                BuiltInCategory.OST_Stairs,
                BuiltInCategory.OST_StairsRailing,
                BuiltInCategory.OST_Ramps,
                BuiltInCategory.OST_Railings,
                
                // MEP (for enclosures)
                BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_Casework,
                BuiltInCategory.OST_Furniture,
                BuiltInCategory.OST_Columns,
                
                // Site
                BuiltInCategory.OST_Topography,
                BuiltInCategory.OST_Parking,
                BuiltInCategory.OST_Planting
            };

            ElementMulticategoryFilter categoryFilter = new ElementMulticategoryFilter(categories);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> elements = collector.WherePasses(categoryFilter).WhereElementIsNotElementType().ToElements();

            foreach (Element elem in elements)
            {
                // Get Revit category for IFC mapping
                string revitCategory = elem.Category?.Name ?? "";
                string ifcCategory = MapRevitCategoryToIfc(revitCategory);

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
                            var carbonFactor = GetCarbonFactor(matName);
                            
                            materialMap[matName] = new MaterialBreakdown
                            {
                                MaterialName = matName,
                                Quantity = "0 m3",
                                CarbonFactor = carbonFactor.AverageGwp,
                                TotalCarbon = 0,
                                DataSource = carbonFactor.Source,
                                Ec3Category = carbonFactor.Ec3Category,
                                // IFC fields for openBIM interoperability
                                IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                                IfcExportAs = "IfcMaterial",
                                IfcCategory = ifcCategory,
                                RevitElementId = elem.Id.ToString(),
                                VolumeM3 = 0,
                                MassKg = 0
                            };
                        }

                        double volumeM3 = volume * 0.0283168;
                        double currentQty = 0;
                        string qtyStr = materialMap[matName].Quantity.Replace(" m3", "").Replace(" m³", "");
                        if (double.TryParse(qtyStr, out currentQty))
                        {
                            materialMap[matName].Quantity = $"{(currentQty + volumeM3):F2} m3";
                        }
                        materialMap[matName].TotalCarbon += volumeM3 * materialMap[matName].CarbonFactor;
                        materialMap[matName].VolumeM3 += volumeM3;
                        materialMap[matName].MassKg += volumeM3 * GetDensity(materialMap[matName].Ec3Category);
                    }
                }
            }

            // Sort by total carbon (highest first) so biggest impact shows first
            var sortedList = new List<MaterialBreakdown>(materialMap.Values);
            sortedList.Sort((a, b) => b.TotalCarbon.CompareTo(a.TotalCarbon));
            
            return sortedList;
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

            // CONCRETE & CEMENT
            if (name.Contains("concrete") || name.Contains("cement") || name.Contains("cmu") || name.Contains("masonry unit"))
            {
                gwp = 340; category = "Concrete";
            }
            // STEEL & METALS
            else if (name.Contains("steel") || name.Contains("iron") || name.Contains("metal deck") || name.Contains("rebar"))
            {
                gwp = 1850; category = "Steel";
            }
            else if (name.Contains("aluminum") || name.Contains("aluminium"))
            {
                gwp = 8000; category = "Aluminum";
            }
            else if (name.Contains("copper"))
            {
                gwp = 3500; category = "Copper";
            }
            else if (name.Contains("zinc"))
            {
                gwp = 3200; category = "Zinc";
            }
            // GLASS & GLAZING
            else if (name.Contains("glass") || name.Contains("glazing") || name.Contains("window"))
            {
                gwp = 1500; category = "Glass";
            }
            // WOOD & TIMBER
            else if (name.Contains("wood") || name.Contains("timber") || name.Contains("lumber") || 
                     name.Contains("plywood") || name.Contains("osb") || name.Contains("particle"))
            {
                gwp = 110; category = "Wood";
            }
            else if (name.Contains("clt") || name.Contains("glulam") || name.Contains("mass timber"))
            {
                gwp = -500; category = "Mass Timber"; // Carbon negative!
            }
            // MASONRY
            else if (name.Contains("brick") || name.Contains("terra") || name.Contains("clay"))
            {
                gwp = 200; category = "Brick/Masonry";
            }
            else if (name.Contains("stone") || name.Contains("granite") || name.Contains("marble") || name.Contains("limestone"))
            {
                gwp = 100; category = "Stone";
            }
            // GYPSUM & INTERIOR
            else if (name.Contains("gypsum") || name.Contains("drywall") || name.Contains("sheetrock") || name.Contains("plaster"))
            {
                gwp = 200; category = "Gypsum Board";
            }
            else if (name.Contains("ceiling") || name.Contains("acoustic") || name.Contains("tile"))
            {
                gwp = 150; category = "Ceiling Tile";
            }
            // INSULATION
            else if (name.Contains("insulation") || name.Contains("mineral wool") || name.Contains("fiberglass") || 
                     name.Contains("rockwool") || name.Contains("foam") || name.Contains("xps") || name.Contains("eps"))
            {
                gwp = 50; category = "Insulation";
            }
            // ROOFING
            else if (name.Contains("roof") || name.Contains("shingle") || name.Contains("membrane") || 
                     name.Contains("tpo") || name.Contains("epdm") || name.Contains("bitumen"))
            {
                gwp = 300; category = "Roofing";
            }
            // FLOORING
            else if (name.Contains("carpet") || name.Contains("vinyl") || name.Contains("lvt") || name.Contains("linoleum"))
            {
                gwp = 25; category = "Flooring";
            }
            else if (name.Contains("terrazzo") || name.Contains("epoxy"))
            {
                gwp = 150; category = "Flooring";
            }
            // PAINT & COATINGS
            else if (name.Contains("paint") || name.Contains("coating") || name.Contains("finish"))
            {
                gwp = 5; category = "Coatings";
            }
            // WATERPROOFING
            else if (name.Contains("waterproof") || name.Contains("vapor") || name.Contains("barrier") || name.Contains("membrane"))
            {
                gwp = 80; category = "Waterproofing";
            }
            // SEALANTS & ADHESIVES
            else if (name.Contains("sealant") || name.Contains("adhesive") || name.Contains("caulk"))
            {
                gwp = 10; category = "Sealants";
            }
            // PLASTIC & COMPOSITES
            else if (name.Contains("plastic") || name.Contains("pvc") || name.Contains("hdpe") || name.Contains("composite"))
            {
                gwp = 3000; category = "Plastics";
            }
            // CURTAIN WALL
            else if (name.Contains("curtain") || name.Contains("panel") || name.Contains("cladding") || name.Contains("facade"))
            {
                gwp = 500; category = "Cladding";
            }
            // DEFAULT
            else
            {
                gwp = 100; category = "Other";
            }

            return new Ec3CarbonFactor
            {
                Category = materialName,
                Ec3Category = category,
                AverageGwp = gwp,
                Unit = "kgCO2e/m³",
                Source = "CLF v2021 Baseline"
            };
        }

        /// <summary>
        /// Maps Revit category to IFC element type
        /// </summary>
        private string MapRevitCategoryToIfc(string revitCategory)
        {
            return revitCategory?.ToLower() switch
            {
                "walls" => "IfcWall",
                "floors" => "IfcSlab",
                "roofs" => "IfcRoof",
                "ceilings" => "IfcCovering",
                "structural columns" => "IfcColumn",
                "structural framing" => "IfcBeam",
                "structural foundation" => "IfcFooting",
                "windows" => "IfcWindow",
                "doors" => "IfcDoor",
                "curtain wall panels" => "IfcPlate",
                "curtain wall mullions" => "IfcMember",
                "stairs" => "IfcStair",
                "railings" => "IfcRailing",
                "ramps" => "IfcRamp",
                "furniture" => "IfcFurniture",
                "casework" => "IfcFurniture",
                "mechanical equipment" => "IfcBuildingElementProxy",
                _ => "IfcBuildingElementProxy"
            };
        }

        /// <summary>
        /// Gets material density for mass calculation (kg/m3)
        /// </summary>
        private double GetDensity(string category)
        {
            return category?.ToLower() switch
            {
                "concrete" => 2400,
                "steel" => 7850,
                "aluminum" => 2700,
                "wood" => 500,
                "glass" => 2500,
                "gypsum board" => 800,
                "insulation" => 30,
                "brick/masonry" => 1800,
                "stone" => 2500,
                "roofing" => 1200,
                "cladding" => 1500,
                _ => 1000
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
