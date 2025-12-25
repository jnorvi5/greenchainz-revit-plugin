using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// LEED v4.1 MRpc132 Pilot Credit Calculator
    /// Procurement of Low Carbon Construction Materials
    /// Uses UW Carbon Leadership Forum (CLF) baseline methodology v2021
    /// </summary>
    public class LeedMRpc132Calculator
    {
        // UW CLF Material Embodied Carbon Intensity Baselines (mECIb) v2021
        // Source: University of Washington / Carbon Leadership Forum
        // Units: kgCO2e per unit as specified
        private static readonly Dictionary<string, CLFBaseline> CLF_BASELINES = new Dictionary<string, CLFBaseline>
        {
            // Concrete - kgCO2e/m³
            { "concrete_ready_mix", new CLFBaseline("Concrete (Ready Mix)", 340, "m³", "kgCO2e/m³") },
            { "concrete_slurry", new CLFBaseline("Concrete (Slurry)", 280, "m³", "kgCO2e/m³") },
            { "concrete_shotcrete", new CLFBaseline("Concrete (Shotcrete)", 380, "m³", "kgCO2e/m³") },
            
            // Masonry - kgCO2e/m²
            { "cmu", new CLFBaseline("CMU (Concrete Masonry Unit)", 45, "m²", "kgCO2e/m²") },
            { "brick", new CLFBaseline("Brick Masonry", 52, "m²", "kgCO2e/m²") },
            
            // Steel - kgCO2e/metric ton
            { "steel_rebar", new CLFBaseline("Steel Rebar", 1100, "mt", "kgCO2e/mt") },
            { "steel_structural", new CLFBaseline("Structural Steel", 1370, "mt", "kgCO2e/mt") },
            { "steel_decking", new CLFBaseline("Steel Decking", 2050, "mt", "kgCO2e/mt") },
            { "steel_cold_formed", new CLFBaseline("Cold Formed Steel", 2580, "mt", "kgCO2e/mt") },
            { "steel_open_web_joist", new CLFBaseline("Open-Web Steel Joists", 1680, "mt", "kgCO2e/mt") },
            { "steel_plate", new CLFBaseline("Plate Steel", 1500, "mt", "kgCO2e/mt") },
            
            // Aluminum - kgCO2e/kg
            { "aluminum_extrusion", new CLFBaseline("Aluminum Extrusions", 12.8, "kg", "kgCO2e/kg") },
            { "aluminum_thermal", new CLFBaseline("Thermally Improved Aluminum", 14.2, "kg", "kgCO2e/kg") },
            
            // Wood & Composites - kgCO2e/m³
            { "wood_dimensional", new CLFBaseline("Dimensional Lumber", 110, "m³", "kgCO2e/m³") },
            { "wood_plywood", new CLFBaseline("Plywood/OSB Sheathing", 340, "m³", "kgCO2e/m³") },
            { "wood_mass_timber", new CLFBaseline("Mass Timber (CLT/Glulam)", 215, "m³", "kgCO2e/m³") },
            { "wood_prefab", new CLFBaseline("Prefabricated Wood Products", 285, "m³", "kgCO2e/m³") },
            { "wood_composite_lumber", new CLFBaseline("Composite Lumber", 380, "m³", "kgCO2e/m³") },
            
            // Insulation - kgCO2e/m² (R-1)
            { "insulation_board", new CLFBaseline("Board Insulation", 5.8, "m²", "kgCO2e/m²·R1") },
            { "insulation_blanket", new CLFBaseline("Blanket Insulation", 1.3, "m²", "kgCO2e/m²·R1") },
            { "insulation_foam", new CLFBaseline("Foamed-in-Place", 4.2, "m²", "kgCO2e/m²·R1") },
            { "insulation_blown", new CLFBaseline("Blown Insulation", 1.8, "m²", "kgCO2e/m²·R1") },
            
            // Cladding - kgCO2e/m²
            { "cladding_metal_insulated", new CLFBaseline("Insulated Metal Panel", 42, "m²", "kgCO2e/m²") },
            { "cladding_metal", new CLFBaseline("Metal Panel", 28, "m²", "kgCO2e/m²") },
            
            // Finishes - kgCO2e/m²
            { "gypsum_board", new CLFBaseline("Gypsum Board", 3.2, "m²", "kgCO2e/m²") },
            { "ceiling_tile", new CLFBaseline("Acoustical Ceiling Tiles", 4.8, "m²", "kgCO2e/m²") },
            { "flooring_resilient", new CLFBaseline("Resilient Flooring", 12.5, "m²", "kgCO2e/m²") },
            { "carpet", new CLFBaseline("Carpet", 8.9, "m²", "kgCO2e/m²") },
            
            // Bulk Materials - kgCO2e/m²
            { "glass_flat", new CLFBaseline("Flat Glass", 35, "m²", "kgCO2e/m²") },
            { "glass_glazing", new CLFBaseline("Glazing System", 85, "m²", "kgCO2e/m²") },
            
            // Communications
            { "data_cabling", new CLFBaseline("Data Cabling", 2.1, "m", "kgCO2e/m") }
        };

        public LeedEmbodiedCarbonResult CalculateEmbodiedCarbon(Document doc)
        {
            var result = new LeedEmbodiedCarbonResult
            {
                ProjectName = doc.Title,
                CalculationDate = DateTime.Now,
                BaselineYear = "CLF v2021",
                Materials = new List<LeedMaterialCarbon>()
            };

            // Extract materials from model
            var materialVolumes = ExtractMaterialQuantities(doc);

            double totalBaselineCarbon = 0;
            double totalActualCarbon = 0;

            foreach (var kvp in materialVolumes)
            {
                string materialName = kvp.Key;
                double quantity = kvp.Value.Quantity;
                string unit = kvp.Value.Unit;

                // Match to CLF baseline
                var baseline = MatchToCLFBaseline(materialName);
                
                double baselineCarbon = baseline.BaselineValue * quantity;
                
                // Actual carbon (estimated - would use EPD data if available)
                // Apply 10-30% reduction estimate for materials with EPDs
                double actualCarbon = baselineCarbon * GetActualCarbonMultiplier(materialName);

                var materialResult = new LeedMaterialCarbon
                {
                    MaterialCategory = baseline.Category,
                    MaterialName = materialName,
                    Quantity = quantity,
                    Unit = unit,
                    BaselineECI = baseline.BaselineValue,
                    BaselineUnit = baseline.BaselineUnit,
                    BaselineCarbon = baselineCarbon,
                    ActualECI = baseline.BaselineValue * GetActualCarbonMultiplier(materialName),
                    ActualCarbon = actualCarbon,
                    HasEPD = false, // Would check EPD database
                    EPDSource = "Estimated (no EPD)"
                };

                result.Materials.Add(materialResult);
                totalBaselineCarbon += baselineCarbon;
                totalActualCarbon += actualCarbon;
            }

            // Calculate building area (simplified - would get from Revit)
            double buildingAreaSF = EstimateBuildingArea(doc);
            
            result.TotalBaselineCarbon = totalBaselineCarbon;
            result.TotalActualCarbon = totalActualCarbon;
            result.BuildingAreaSF = buildingAreaSF;
            result.bECIb = buildingAreaSF > 0 ? totalBaselineCarbon / buildingAreaSF : 0;
            result.bECIa = buildingAreaSF > 0 ? totalActualCarbon / buildingAreaSF : 0;
            result.PercentReduction = result.bECIb > 0 
                ? ((result.bECIb - result.bECIa) / result.bECIb) * 100 
                : 0;

            // Award points per LEED v4.1 MRpc132
            if (result.PercentReduction >= 30)
                result.PointsEarned = 2;
            else if (result.PercentReduction > 0)
                result.PointsEarned = 1;
            else
                result.PointsEarned = 0;

            result.CreditStatus = result.PercentReduction >= 30 ? "Mid-range Reduction (2 pts)" :
                                  result.PercentReduction > 0 ? "Low-range Reduction (1 pt)" :
                                  "Not Achieved";

            return result;
        }

        private Dictionary<string, MaterialQuantity> ExtractMaterialQuantities(Document doc)
        {
            var quantities = new Dictionary<string, MaterialQuantity>();

            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_StructuralFoundation,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_CurtainWallPanels
            };

            var categoryFilter = new ElementMulticategoryFilter(categories);
            var collector = new FilteredElementCollector(doc);
            var elements = collector.WherePasses(categoryFilter).WhereElementIsNotElementType().ToElements();

            foreach (Element elem in elements)
            {
                foreach (ElementId matId in elem.GetMaterialIds(false))
                {
                    var mat = doc.GetElement(matId) as Autodesk.Revit.DB.Material;
                    if (mat == null) continue;

                    string matName = mat.Name;
                    double volumeCuFt = elem.GetMaterialVolume(matId);
                    double volumeM3 = volumeCuFt * 0.0283168;

                    if (volumeM3 > 0.0001)
                    {
                        if (!quantities.ContainsKey(matName))
                        {
                            quantities[matName] = new MaterialQuantity { Quantity = 0, Unit = "m³" };
                        }
                        quantities[matName].Quantity += volumeM3;
                    }
                }
            }

            return quantities;
        }

        private CLFBaseline MatchToCLFBaseline(string materialName)
        {
            string name = materialName.ToLower();

            // Concrete
            if (name.Contains("concrete") || name.Contains("cast-in-place"))
                return CLF_BASELINES["concrete_ready_mix"];
            if (name.Contains("shotcrete") || name.Contains("gunite"))
                return CLF_BASELINES["concrete_shotcrete"];

            // Masonry
            if (name.Contains("cmu") || name.Contains("block"))
                return CLF_BASELINES["cmu"];
            if (name.Contains("brick"))
                return CLF_BASELINES["brick"];

            // Steel
            if (name.Contains("rebar") || name.Contains("reinforc"))
                return CLF_BASELINES["steel_rebar"];
            if (name.Contains("structural steel") || name.Contains("wide flange") || name.Contains("w-shape"))
                return CLF_BASELINES["steel_structural"];
            if (name.Contains("deck") && name.Contains("steel"))
                return CLF_BASELINES["steel_decking"];
            if (name.Contains("stud") && (name.Contains("metal") || name.Contains("steel")))
                return CLF_BASELINES["steel_cold_formed"];
            if (name.Contains("joist"))
                return CLF_BASELINES["steel_open_web_joist"];
            if (name.Contains("steel") || name.Contains("metal"))
                return CLF_BASELINES["steel_structural"];

            // Aluminum
            if (name.Contains("aluminum") || name.Contains("aluminium"))
                return name.Contains("thermal") ? CLF_BASELINES["aluminum_thermal"] : CLF_BASELINES["aluminum_extrusion"];

            // Wood
            if (name.Contains("clt") || name.Contains("cross laminated") || name.Contains("glulam") || name.Contains("mass timber"))
                return CLF_BASELINES["wood_mass_timber"];
            if (name.Contains("plywood") || name.Contains("osb") || name.Contains("sheathing"))
                return CLF_BASELINES["wood_plywood"];
            if (name.Contains("wood") || name.Contains("timber") || name.Contains("lumber"))
                return CLF_BASELINES["wood_dimensional"];

            // Insulation
            if (name.Contains("rigid insulation") || name.Contains("board insulation") || name.Contains("xps") || name.Contains("eps"))
                return CLF_BASELINES["insulation_board"];
            if (name.Contains("batt") || name.Contains("blanket") || name.Contains("fiberglass"))
                return CLF_BASELINES["insulation_blanket"];
            if (name.Contains("spray foam") || name.Contains("foamed"))
                return CLF_BASELINES["insulation_foam"];
            if (name.Contains("insulation"))
                return CLF_BASELINES["insulation_blanket"];

            // Finishes
            if (name.Contains("gypsum") || name.Contains("drywall") || name.Contains("gyp"))
                return CLF_BASELINES["gypsum_board"];
            if (name.Contains("ceiling") && name.Contains("tile"))
                return CLF_BASELINES["ceiling_tile"];
            if (name.Contains("carpet"))
                return CLF_BASELINES["carpet"];
            if (name.Contains("vinyl") || name.Contains("resilient") || name.Contains("lvt"))
                return CLF_BASELINES["flooring_resilient"];

            // Glass
            if (name.Contains("glazing") || name.Contains("curtain wall"))
                return CLF_BASELINES["glass_glazing"];
            if (name.Contains("glass"))
                return CLF_BASELINES["glass_flat"];

            // Default
            return new CLFBaseline("Other Material", 100, "m³", "kgCO2e/m³");
        }

        private double GetActualCarbonMultiplier(string materialName)
        {
            // This would be replaced with actual EPD data
            // For now, estimate based on material availability of low-carbon options
            string name = materialName.ToLower();

            if (name.Contains("recycled")) return 0.50;
            if (name.Contains("low carbon") || name.Contains("low-carbon")) return 0.60;
            if (name.Contains("green") || name.Contains("eco")) return 0.75;
            
            // Materials with commonly available EPDs
            if (name.Contains("concrete")) return 0.85;
            if (name.Contains("steel")) return 0.80;
            if (name.Contains("wood") || name.Contains("timber")) return 0.70;
            if (name.Contains("gypsum")) return 0.90;
            
            return 0.95; // Default - assume near baseline
        }

        private double EstimateBuildingArea(Document doc)
        {
            double totalArea = 0;

            var collector = new FilteredElementCollector(doc);
            var floors = collector.OfCategory(BuiltInCategory.OST_Floors)
                                 .WhereElementIsNotElementType()
                                 .ToElements();

            foreach (var floor in floors)
            {
                var areaParam = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                if (areaParam != null)
                {
                    totalArea += areaParam.AsDouble(); // sq ft
                }
            }

            return totalArea > 0 ? totalArea : 10000; // Default 10,000 sf if can't calculate
        }
    }

    public class CLFBaseline
    {
        public string Category { get; set; }
        public double BaselineValue { get; set; }
        public string Unit { get; set; }
        public string BaselineUnit { get; set; }

        public CLFBaseline(string category, double value, string unit, string baselineUnit)
        {
            Category = category;
            BaselineValue = value;
            Unit = unit;
            BaselineUnit = baselineUnit;
        }
    }

    public class MaterialQuantity
    {
        public double Quantity { get; set; }
        public string Unit { get; set; }
    }
}
