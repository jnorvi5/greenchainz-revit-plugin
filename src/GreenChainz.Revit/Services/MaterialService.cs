using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Utils;

namespace GreenChainz.Revit.Services
{
    public class MaterialService
    {
        private readonly SdaConnectorService _sdaConnector;
        private readonly bool _useMockData;

        /// <summary>
        /// Creates a MaterialService with optional SDA connector.
        /// If no connector is provided, mock data will be used.
        /// </summary>
        public MaterialService(SdaConnectorService sdaConnector = null)
        {
            _sdaConnector = sdaConnector;
            _useMockData = (sdaConnector == null);
        }

        /// <summary>
        /// Indicates whether the service is using mock data or real SDA data.
        /// </summary>
        public bool IsUsingMockData => _useMockData;

        public async Task<List<Models.Material>> GetMaterialsAsync()
        {
            if (_useMockData)
            {
                return await GetMockMaterialsAsync();
            }

            try
            {
                var sdaMaterials = await _sdaConnector.GetMaterialsAsync();
                return ConvertSdaMaterialsToMaterials(sdaMaterials);
            }
            catch (Exception)
            {
                // Fallback to mock data if SDA API fails
                return await GetMockMaterialsAsync();
            }
        }

        public async Task<List<Models.Material>> GetMaterialsByCategoryAsync(string category)
        {
            if (_useMockData)
            {
                var allMaterials = await GetMockMaterialsAsync();
                return allMaterials.FindAll(m =>
                    m.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            try
            {
                var sdaMaterials = await _sdaConnector.GetMaterialsAsync(category);
                return ConvertSdaMaterialsToMaterials(sdaMaterials);
            }
            catch (Exception)
            {
                var allMaterials = await GetMockMaterialsAsync();
                return allMaterials.FindAll(m =>
                    m.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }
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
                    PricePerUnit = 0m, // SDA doesn't provide pricing data
                    Certifications = new List<string> { sdaMat.Certifications ?? "" },
                    Manufacturer = sdaMat.Manufacturer ?? "Unknown",
                    Description = !string.IsNullOrEmpty(sdaMat.EpdUrl)
                        ? $"EPD: {sdaMat.EpdUrl}"
                        : "No EPD available",
                    Unit = "kg", // Default unit
                    IsVerified = true // Assume SDA materials are verified
                });
            }

            return materials;
        }

        private const int MockApiDelayMilliseconds = 500;

        private async Task<List<Models.Material>> GetMockMaterialsAsync()
        {
            // Simulate API delay
            await Task.Delay(MockApiDelayMilliseconds);

            return new List<Models.Material>
            {
                new Models.Material
                {
                    Id = "1",
                    Name = "Recycled Steel Beam",
                    Category = "Steel",
                    EmbodiedCarbon = 120.5,
                    PricePerUnit = 450.00m,
                    Certifications = new List<string> { "ISO 14001", "EPD" },
                    Manufacturer = "Green Steel Co.",
                    Description = "High strength recycled steel beam.",
                    Unit = "ton",
                    IsVerified = true
                },
                new Models.Material
                {
                    Id = "2",
                    Name = "Low-Carbon Concrete Block",
                    Category = "Concrete",
                    EmbodiedCarbon = 15.2,
                    PricePerUnit = 5.50m,
                    Certifications = new List<string> { "GreenGuard" },
                    Manufacturer = "EcoBuild Concrete",
                    Description = "Concrete block with 40% fly ash replacement.",
                    Unit = "block",
                    IsVerified = true
                },
                new Models.Material
                {
                    Id = "3",
                    Name = "FSC Certified Oak Flooring",
                    Category = "Wood",
                    EmbodiedCarbon = 5.8,
                    PricePerUnit = 85.00m,
                    Certifications = new List<string> { "FSC", "LEED Compliant" },
                    Manufacturer = "Forest First",
                    Description = "Sustainably harvested oak flooring.",
                    Unit = "m2",
                    IsVerified = true
                },
                new Models.Material
                {
                    Id = "4",
                    Name = "Wool Insulation Batts",
                    Category = "Insulation",
                    EmbodiedCarbon = 2.1,
                    PricePerUnit = 22.00m,
                    Certifications = new List<string> { "Declare Label" },
                    Manufacturer = "Natural Warmth",
                    Description = "Natural wool insulation, treated with borates.",
                    Unit = "m2",
                    IsVerified = false
                },
                new Models.Material
                {
                    Id = "5",
                    Name = "Recycled Aluminum Panel",
                    Category = "Metal",
                    EmbodiedCarbon = 85.0,
                    PricePerUnit = 120.00m,
                    Certifications = new List<string> { "Cradle to Cradle" },
                    Manufacturer = "AluCycle",
                    Description = "100% post-consumer recycled aluminum façade panel.",
                    Unit = "m2",
                    IsVerified = true
                }
            };
        }

        public void CreateRevitMaterial(Document doc, Application app, Models.Material apiMaterial)
        {
            using (Transaction t = new Transaction(doc, "Create Green Material"))
            {
                t.Start();

                // 1. Ensure Shared Parameters Exist
                SharedParameterHelper.CreateSharedParameters(doc, app);

                // 2. Check if material exists
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

                // 3. Set standard properties
                revitMaterial.MaterialClass = apiMaterial.Category;

                // 4. Set Shared Parameters
                Parameter carbonParam = revitMaterial.LookupParameter("GC_CarbonScore");
                if (carbonParam != null && !carbonParam.IsReadOnly)
                    carbonParam.Set(apiMaterial.EmbodiedCarbon);

                Parameter manufacturerParam = revitMaterial.LookupParameter("GC_Supplier");
                if (manufacturerParam != null && !manufacturerParam.IsReadOnly)
                    manufacturerParam.Set(apiMaterial.Manufacturer);

                Parameter certParam = revitMaterial.LookupParameter("GC_Certifications");
                if (certParam != null && !certParam.IsReadOnly)
                    certParam.Set(apiMaterial.Certifications != null
                        ? string.Join(", ", apiMaterial.Certifications)
                        : string.Empty);

                Color displayColor = new Color(128, 128, 128); // Grey default
                if (apiMaterial.EmbodiedCarbon < 10)
                    displayColor = new Color(0, 200, 0); // Green for very low carbon
                else if (apiMaterial.EmbodiedCarbon < 50)
                    displayColor = new Color(200, 200, 0); // Yellow for medium carbon

                revitMaterial.Color = displayColor;

                t.Commit();
            }
        }
    }
}
