using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Utils;

namespace GreenChainz.Revit.Services
{
    public class MaterialService
    {
        private readonly SdaConnectorService _sdaConnector;
        private readonly Ec3ApiService _ec3Service;
        private readonly bool _useMockData;

        public MaterialService(SdaConnectorService sdaConnector = null)
        {
            _sdaConnector = sdaConnector;
            _ec3Service = App.Ec3Service;
            _useMockData = (sdaConnector == null && _ec3Service == null);
        }

        public bool IsUsingMockData => _useMockData;

        public async Task<List<Models.Material>> GetMaterialsAsync()
        {
            // Try EC3 first for real sustainable materials
            if (_ec3Service?.HasValidApiKey == true)
            {
                try
                {
                    return await GetEc3MaterialsAsync();
                }
                catch { /* Fall through to other sources */ }
            }

            // Try SDA
            if (_sdaConnector != null)
            {
                try
                {
                    var sdaMaterials = await _sdaConnector.GetMaterialsAsync();
                    return ConvertSdaMaterialsToMaterials(sdaMaterials);
                }
                catch { /* Fall through to mock */ }
            }

            return await GetMockMaterialsAsync();
        }

        private async Task<List<Models.Material>> GetEc3MaterialsAsync()
        {
            var materials = new List<Models.Material>();
            
            // Search EC3 for common sustainable material categories
            var categories = new[] { "concrete", "steel", "wood", "insulation", "glass", "aluminum" };
            
            foreach (var category in categories)
            {
                try
                {
                    var ec3Materials = await _ec3Service.SearchMaterialsAsync(category);
                    foreach (var ec3Mat in ec3Materials)
                    {
                        materials.Add(new Models.Material
                        {
                            Id = ec3Mat.Id ?? Guid.NewGuid().ToString(),
                            Name = ec3Mat.Name ?? "Unknown",
                            Category = ec3Mat.Category ?? category,
                            EmbodiedCarbon = ec3Mat.Gwp,
                            PricePerUnit = 0m,
                            Certifications = new List<string> { "EPD" },
                            Manufacturer = ec3Mat.Manufacturer ?? "Unknown",
                            Description = $"GWP: {ec3Mat.Gwp:F2} {ec3Mat.GwpUnit}",
                            Unit = ec3Mat.DeclaredUnit ?? "kg",
                            IsVerified = true
                        });
                    }
                }
                catch { continue; }
            }

            // If EC3 returned nothing, use enhanced mock data
            if (materials.Count == 0)
            {
                return await GetMockMaterialsAsync();
            }

            return materials;
        }

        public async Task<List<Models.Material>> GetMaterialsByCategoryAsync(string category)
        {
            if (_ec3Service?.HasValidApiKey == true)
            {
                try
                {
                    var ec3Materials = await _ec3Service.SearchMaterialsAsync(category);
                    var materials = new List<Models.Material>();
                    foreach (var ec3Mat in ec3Materials)
                    {
                        materials.Add(new Models.Material
                        {
                            Id = ec3Mat.Id ?? Guid.NewGuid().ToString(),
                            Name = ec3Mat.Name ?? "Unknown",
                            Category = ec3Mat.Category ?? category,
                            EmbodiedCarbon = ec3Mat.Gwp,
                            Manufacturer = ec3Mat.Manufacturer ?? "Unknown",
                            Certifications = new List<string> { "EPD" },
                            IsVerified = true
                        });
                    }
                    if (materials.Count > 0) return materials;
                }
                catch { /* Fall through */ }
            }

            var allMaterials = await GetMockMaterialsAsync();
            return allMaterials.FindAll(m =>
                m.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        private List<Models.Material> ConvertSdaMaterialsToMaterials(List<SdaMaterial> sdaMaterials)
        {
            var materials = new List<Models.Material>();
            foreach (var sdaMat in sdaMaterials)
            {
                materials.Add(new Models.Material
                {
                    Id = sdaMat.Id,
                    Name = sdaMat.Name,
                    Category = sdaMat.Category ?? "Unknown",
                    EmbodiedCarbon = sdaMat.EmbodiedCarbon,
                    PricePerUnit = 0m,
                    Certifications = new List<string> { sdaMat.Certifications ?? "" },
                    Manufacturer = sdaMat.Manufacturer ?? "Unknown",
                    Description = !string.IsNullOrEmpty(sdaMat.EpdUrl)
                        ? $"EPD: {sdaMat.EpdUrl}"
                        : "No EPD available",
                    Unit = "kg",
                    IsVerified = true
                });
            }
            return materials;
        }

        private async Task<List<Models.Material>> GetMockMaterialsAsync()
        {
            await Task.Delay(200);

            return new List<Models.Material>
            {
                // CONCRETE
                new Models.Material
                {
                    Id = "1", Name = "CarbonCure Ready-Mix Concrete", Category = "Concrete",
                    EmbodiedCarbon = 280, PricePerUnit = 125m,
                    Certifications = new List<string> { "EPD", "Carbon Negative", "LEED" },
                    Manufacturer = "CarbonCure Technologies",
                    Description = "CO2 mineralized concrete - 5-10% lower GWP", Unit = "m³", IsVerified = true,
                    CompressiveStrength = 28, Density = 2400, FireRating = "4-Hour",
                    ThermalConductivity = 1.7, CostPerUnit = 125, LifeExpectancy = "75+ years",
                    RecycledContent = 15, Region = "North America"
                },
                new Models.Material
                {
                    Id = "2", Name = "Low-Carbon Concrete 4000 PSI", Category = "Concrete",
                    EmbodiedCarbon = 320, PricePerUnit = 110m,
                    Certifications = new List<string> { "EPD", "LEED Contributing" },
                    Manufacturer = "Central Concrete",
                    Description = "40% fly ash replacement concrete", Unit = "m³", IsVerified = true,
                    CompressiveStrength = 27.6, Density = 2350, FireRating = "3-Hour",
                    ThermalConductivity = 1.6, CostPerUnit = 110, LifeExpectancy = "50+ years",
                    RecycledContent = 40, Region = "North America"
                },
                // STEEL
                new Models.Material
                {
                    Id = "3", Name = "HYBRIT Fossil-Free Steel", Category = "Steel",
                    EmbodiedCarbon = 450, PricePerUnit = 1200m,
                    Certifications = new List<string> { "EPD", "Science Based Targets" },
                    Manufacturer = "SSAB",
                    Description = "World's first fossil-free steel", Unit = "ton", IsVerified = true,
                    CompressiveStrength = 250, Density = 7850, FireRating = "Non-Combustible",
                    ThermalConductivity = 50, CostPerUnit = 1200, LifeExpectancy = "100+ years",
                    RecycledContent = 0, Region = "Scandinavia"
                },
                new Models.Material
                {
                    Id = "4", Name = "Recycled Steel Beam W12x26", Category = "Steel",
                    EmbodiedCarbon = 750, PricePerUnit = 850m,
                    Certifications = new List<string> { "EPD", "ISO 14001", "Responsible Steel" },
                    Manufacturer = "Nucor Corporation",
                    Description = "97% recycled EAF steel", Unit = "ton", IsVerified = true,
                    CompressiveStrength = 345, Density = 7850, FireRating = "Non-Combustible",
                    ThermalConductivity = 50, CostPerUnit = 850, LifeExpectancy = "100+ years",
                    RecycledContent = 97, Region = "North America"
                },
                // WOOD
                new Models.Material
                {
                    Id = "5", Name = "Cross-Laminated Timber (CLT)", Category = "Wood",
                    EmbodiedCarbon = -500, PricePerUnit = 450m,
                    Certifications = new List<string> { "FSC", "PEFC", "EPD" },
                    Manufacturer = "Structurlam",
                    Description = "Carbon sequestering mass timber", Unit = "m³", IsVerified = true,
                    CompressiveStrength = 24, Density = 500, FireRating = "2-Hour",
                    ThermalConductivity = 0.13, CostPerUnit = 450, LifeExpectancy = "80+ years",
                    RecycledContent = 0, Region = "North America"
                },
                new Models.Material
                {
                    Id = "6", Name = "Glulam Beam GL24h", Category = "Wood",
                    EmbodiedCarbon = -400, PricePerUnit = 380m,
                    Certifications = new List<string> { "FSC", "EPD" },
                    Manufacturer = "Nordic Structures",
                    Description = "Structural glued laminated timber", Unit = "m³", IsVerified = true,
                    CompressiveStrength = 24, Density = 470, FireRating = "1-Hour",
                    ThermalConductivity = 0.12, CostPerUnit = 380, LifeExpectancy = "70+ years",
                    RecycledContent = 0, Region = "North America"
                },
                // INSULATION
                new Models.Material
                {
                    Id = "7", Name = "Rockwool ComfortBoard 80", Category = "Insulation",
                    EmbodiedCarbon = 1.2, PricePerUnit = 28m,
                    Certifications = new List<string> { "EPD", "GREENGUARD", "Cradle to Cradle" },
                    Manufacturer = "Rockwool",
                    Description = "Stone wool rigid board insulation", Unit = "m²", IsVerified = true,
                    CompressiveStrength = 0.08, Density = 128, FireRating = "Class A",
                    ThermalConductivity = 0.036, CostPerUnit = 28, LifeExpectancy = "50+ years",
                    RecycledContent = 70, Region = "North America"
                },
                new Models.Material
                {
                    Id = "8", Name = "EcoTouch Fiberglass Batts R-30", Category = "Insulation",
                    EmbodiedCarbon = 1.5, PricePerUnit = 18m,
                    Certifications = new List<string> { "EPD", "GREENGUARD Gold" },
                    Manufacturer = "Owens Corning",
                    Description = "50% recycled glass content", Unit = "m²", IsVerified = true,
                    CompressiveStrength = 0, Density = 11, FireRating = "Class A",
                    ThermalConductivity = 0.040, CostPerUnit = 18, LifeExpectancy = "30+ years",
                    RecycledContent = 50, Region = "North America"
                },
                // GLASS
                new Models.Material
                {
                    Id = "9", Name = "Guardian SunGuard Low-E Glass", Category = "Glass",
                    EmbodiedCarbon = 25, PricePerUnit = 85m,
                    Certifications = new List<string> { "EPD", "Cradle to Cradle" },
                    Manufacturer = "Guardian Glass",
                    Description = "30% recycled cullet, high performance", Unit = "m²", IsVerified = true,
                    CompressiveStrength = 0, Density = 2500, FireRating = "Non-Rated",
                    ThermalConductivity = 1.0, CostPerUnit = 85, LifeExpectancy = "40+ years",
                    RecycledContent = 30, Region = "North America"
                },
                // ALUMINUM
                new Models.Material
                {
                    Id = "10", Name = "CIRCAL 75R Recycled Aluminum", Category = "Metal",
                    EmbodiedCarbon = 2.3, PricePerUnit = 4500m,
                    Certifications = new List<string> { "EPD", "ASI Certified" },
                    Manufacturer = "Hydro Aluminum",
                    Description = "75% post-consumer recycled aluminum", Unit = "ton", IsVerified = true,
                    CompressiveStrength = 270, Density = 2700, FireRating = "Non-Combustible",
                    ThermalConductivity = 160, CostPerUnit = 4500, LifeExpectancy = "50+ years",
                    RecycledContent = 75, Region = "Europe"
                },
                // GYPSUM
                new Models.Material
                {
                    Id = "11", Name = "USG Sheetrock EcoSmart", Category = "Gypsum",
                    EmbodiedCarbon = 2.8, PricePerUnit = 12m,
                    Certifications = new List<string> { "EPD", "GREENGUARD Gold" },
                    Manufacturer = "USG Corporation",
                    Description = "100% recycled paper, synthetic gypsum", Unit = "m²", IsVerified = true,
                    CompressiveStrength = 0, Density = 700, FireRating = "1-Hour",
                    ThermalConductivity = 0.25, CostPerUnit = 12, LifeExpectancy = "30+ years",
                    RecycledContent = 95, Region = "North America"
                }
            };
        }

        public void CreateRevitMaterial(Document doc, Application app, Models.Material apiMaterial)
        {
            // Step 1: Try to create shared parameters (outside main transaction)
            try
            {
                using (Transaction paramTx = new Transaction(doc, "Setup GreenChainz Parameters"))
                {
                    paramTx.Start();
                    SharedParameterHelper.CreateSharedParameters(doc, app);
                    paramTx.Commit();
                }
            }
            catch
            {
                // Shared params failed — continue without them, material will still be created
            }

            // Step 2: Create or update the material
            using (Transaction t = new Transaction(doc, "Create Green Material"))
            {
                t.Start();

                Autodesk.Revit.DB.Material revitMaterial = null;
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                var materials = collector.OfClass(typeof(Autodesk.Revit.DB.Material)).ToElements();

                foreach (Autodesk.Revit.DB.Material m in materials)
                {
                    if (m.Name.Equals(apiMaterial.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        revitMaterial = m;
                        break;
                    }
                }

                if (revitMaterial == null)
                {
                    ElementId matId = Autodesk.Revit.DB.Material.Create(doc, apiMaterial.Name);
                    revitMaterial = doc.GetElement(matId) as Autodesk.Revit.DB.Material;
                }

                if (revitMaterial == null)
                    throw new Exception("Revit could not create the material element.");

                revitMaterial.MaterialClass = apiMaterial.Category ?? "Unknown";

                // Try to set GreenChainz custom parameters (may not exist if shared params failed)
                try
                {
                    Parameter carbonParam = revitMaterial.LookupParameter("GC_CarbonScore");
                    if (carbonParam != null && !carbonParam.IsReadOnly)
                        carbonParam.Set(apiMaterial.EmbodiedCarbon);
                }
                catch { /* Parameter not available */ }

                try
                {
                    Parameter manufacturerParam = revitMaterial.LookupParameter("GC_Supplier");
                    if (manufacturerParam != null && !manufacturerParam.IsReadOnly)
                        manufacturerParam.Set(apiMaterial.Manufacturer ?? "");
                }
                catch { /* Parameter not available */ }

                try
                {
                    Parameter certParam = revitMaterial.LookupParameter("GC_Certifications");
                    if (certParam != null && !certParam.IsReadOnly)
                        certParam.Set(apiMaterial.Certifications != null
                            ? string.Join(", ", apiMaterial.Certifications)
                            : string.Empty);
                }
                catch { /* Parameter not available */ }

                // Set display color based on carbon score
                try
                {
                    Color displayColor = new Color(128, 128, 128);
                    if (apiMaterial.EmbodiedCarbon < 0)
                        displayColor = new Color(0, 150, 0);
                    else if (apiMaterial.EmbodiedCarbon < 10)
                        displayColor = new Color(0, 200, 0);
                    else if (apiMaterial.EmbodiedCarbon < 50)
                        displayColor = new Color(200, 200, 0);
                    else if (apiMaterial.EmbodiedCarbon > 500)
                        displayColor = new Color(200, 100, 100);

                    revitMaterial.Color = displayColor;
                }
                catch { /* Color setting not critical */ }

                t.Commit();
            }
        }
    }
}
