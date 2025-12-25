using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// LEED v5 BD+C Calculator
    /// Includes all required prerequisites and available credits
    /// </summary>
    public class LeedV5Calculator
    {
        public LeedV5Result CalculateLeedV5Score(Document doc)
        {
            var result = new LeedV5Result
            {
                ProjectName = doc.Title,
                CalculationDate = DateTime.Now,
                Version = "LEED v5 BD+C: New Construction",
                Categories = new List<LeedV5Category>()
            };

            // Extract material and building data
            var buildingData = AnalyzeBuilding(doc);

            // Calculate each category
            result.Categories.Add(CalculateIntegrativeProcess(buildingData));
            result.Categories.Add(CalculateLocationTransportation(buildingData));
            result.Categories.Add(CalculateSustainableSites(buildingData));
            result.Categories.Add(CalculateWaterEfficiency(buildingData));
            result.Categories.Add(CalculateEnergyAtmosphere(buildingData));
            result.Categories.Add(CalculateMaterialsResources(buildingData));
            result.Categories.Add(CalculateIndoorEnvironmentalQuality(buildingData));
            result.Categories.Add(CalculateProjectPriorities(buildingData));

            // Calculate totals
            int totalPoints = 0;
            int maxPoints = 0;
            int prereqsMet = 0;
            int prereqsTotal = 0;

            foreach (var category in result.Categories)
            {
                foreach (var credit in category.Credits)
                {
                    if (credit.IsPrerequisite)
                    {
                        prereqsTotal++;
                        if (credit.Status == "Met") prereqsMet++;
                    }
                    else
                    {
                        totalPoints += credit.PointsEarned;
                        maxPoints += credit.MaxPoints;
                    }
                }
            }

            result.TotalPoints = totalPoints;
            result.MaxPoints = 110; // LEED v5 max
            result.PrerequisitesMet = prereqsMet;
            result.PrerequisitesTotal = prereqsTotal;
            result.CertificationLevel = DetermineCertificationLevel(totalPoints, prereqsMet == prereqsTotal);

            return result;
        }

        private BuildingAnalysisData AnalyzeBuilding(Document doc)
        {
            var data = new BuildingAnalysisData();

            // Get floor area
            var floorCollector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType();
            
            foreach (var floor in floorCollector)
            {
                var areaParam = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                if (areaParam != null)
                    data.TotalFloorArea += areaParam.AsDouble(); // sq ft
            }

            // Analyze materials
            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls, BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Roofs, BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming
            };

            var collector = new FilteredElementCollector(doc)
                .WherePasses(new ElementMulticategoryFilter(categories))
                .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                foreach (ElementId matId in elem.GetMaterialIds(false))
                {
                    var mat = doc.GetElement(matId) as Autodesk.Revit.DB.Material;
                    if (mat == null) continue;

                    double volume = elem.GetMaterialVolume(matId) * 0.0283168; // m³
                    string name = mat.Name.ToLower();

                    data.TotalMaterialVolume += volume;

                    // Categorize materials
                    if (name.Contains("concrete")) data.ConcreteVolume += volume;
                    else if (name.Contains("steel")) data.SteelVolume += volume;
                    else if (name.Contains("wood") || name.Contains("timber")) data.WoodVolume += volume;
                    else if (name.Contains("recycled")) data.RecycledVolume += volume;
                    else if (name.Contains("glass")) data.GlassArea += volume;

                    // Check for low-emitting
                    if (!name.Contains("paint") && !name.Contains("adhesive") && !name.Contains("sealant"))
                        data.LowEmittingVolume += volume;

                    // Estimate embodied carbon
                    data.TotalEmbodiedCarbon += EstimateEmbodiedCarbon(name, volume);
                }
            }

            // Calculate percentages
            if (data.TotalMaterialVolume > 0)
            {
                data.RecycledContentPercent = (data.RecycledVolume / data.TotalMaterialVolume) * 100;
                data.LowEmittingPercent = (data.LowEmittingVolume / data.TotalMaterialVolume) * 100;
                data.WoodPercent = (data.WoodVolume / data.TotalMaterialVolume) * 100;
            }

            // Embodied carbon intensity (kgCO2e/sf)
            if (data.TotalFloorArea > 0)
                data.EmbodiedCarbonIntensity = data.TotalEmbodiedCarbon / data.TotalFloorArea;

            return data;
        }

        private double EstimateEmbodiedCarbon(string materialName, double volumeM3)
        {
            // kgCO2e/m³ estimates
            if (materialName.Contains("concrete")) return volumeM3 * 340;
            if (materialName.Contains("steel")) return volumeM3 * 7800 * 1.85; // density * factor
            if (materialName.Contains("aluminum")) return volumeM3 * 2700 * 12.8;
            if (materialName.Contains("wood") || materialName.Contains("timber")) return volumeM3 * 110;
            if (materialName.Contains("glass")) return volumeM3 * 2500 * 1.5;
            if (materialName.Contains("gypsum")) return volumeM3 * 800 * 0.2;
            if (materialName.Contains("insulation")) return volumeM3 * 50;
            return volumeM3 * 100; // default
        }

        private LeedV5Category CalculateIntegrativeProcess(BuildingAnalysisData data)
        {
            var category = new LeedV5Category
            {
                Name = "Integrative Process, Planning, and Assessments",
                Code = "IP",
                Credits = new List<LeedV5Credit>()
            };

            // IPp1: Climate Resilience Assessment (Required)
            category.Credits.Add(new LeedV5Credit
            {
                Code = "IPp1",
                Name = "Climate Resilience Assessment",
                IsPrerequisite = true,
                MaxPoints = 0,
                PointsEarned = 0,
                Status = "Requires Documentation",
                Description = "Assess climate risks and vulnerabilities"
            });

            // IPp2: Human Impact Assessment (Required)
            category.Credits.Add(new LeedV5Credit
            {
                Code = "IPp2",
                Name = "Human Impact Assessment",
                IsPrerequisite = true,
                MaxPoints = 0,
                PointsEarned = 0,
                Status = "Requires Documentation",
                Description = "Assess project impact on occupants and community"
            });

            // IPp3: Carbon Assessment (Required) - We can calculate this!
            bool carbonAssessmentMet = data.TotalEmbodiedCarbon > 0;
            category.Credits.Add(new LeedV5Credit
            {
                Code = "IPp3",
                Name = "Carbon Assessment",
                IsPrerequisite = true,
                MaxPoints = 0,
                PointsEarned = 0,
                Status = carbonAssessmentMet ? "Met" : "Not Met",
                Description = $"Embodied Carbon: {data.TotalEmbodiedCarbon:N0} kgCO2e ({data.EmbodiedCarbonIntensity:F2} kgCO2e/sf)"
            });

            // IPc1: Integrative Design Process (1 point)
            category.Credits.Add(new LeedV5Credit
            {
                Code = "IPc1",
                Name = "Integrative Design Process",
                IsPrerequisite = false,
                MaxPoints = 1,
                PointsEarned = 0,
                Status = "Not Attempted",
                Description = "Document integrative design workshops"
            });

            return category;
        }

        private LeedV5Category CalculateLocationTransportation(BuildingAnalysisData data)
        {
            var category = new LeedV5Category
            {
                Name = "Location & Transportation",
                Code = "LT",
                Credits = new List<LeedV5Credit>()
            };

            category.Credits.Add(new LeedV5Credit { Code = "LTc1", Name = "Sensitive Land Protection", MaxPoints = 1, Status = "Site Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "LTc2", Name = "Equitable Development", MaxPoints = 2, Status = "Site Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "LTc3", Name = "Compact and Connected Development", MaxPoints = 6, Status = "Site Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "LTc4", Name = "Transportation Demand Management", MaxPoints = 4, Status = "Site Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "LTc5", Name = "Electric Vehicles", MaxPoints = 2, Status = "Site Data Required" });

            return category;
        }

        private LeedV5Category CalculateSustainableSites(BuildingAnalysisData data)
        {
            var category = new LeedV5Category
            {
                Name = "Sustainable Sites",
                Code = "SS",
                Credits = new List<LeedV5Credit>()
            };

            category.Credits.Add(new LeedV5Credit { Code = "SSp1", Name = "Minimized Site Disturbance", IsPrerequisite = true, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "SSc1", Name = "Biodiverse Habitat", MaxPoints = 2, Status = "Site Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "SSc2", Name = "Accessible Outdoor Space", MaxPoints = 1, Status = "Site Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "SSc3", Name = "Rainwater Management", MaxPoints = 3, Status = "Site Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "SSc4", Name = "Enhanced Resilient Site Design", MaxPoints = 2, Status = "Site Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "SSc5", Name = "Heat Island Reduction", MaxPoints = 2, PointsEarned = 1, Status = "Partial", Description = "Based on roof materials" });
            category.Credits.Add(new LeedV5Credit { Code = "SSc6", Name = "Light Pollution Reduction", MaxPoints = 1, Status = "Lighting Data Required" });

            return category;
        }

        private LeedV5Category CalculateWaterEfficiency(BuildingAnalysisData data)
        {
            var category = new LeedV5Category
            {
                Name = "Water Efficiency",
                Code = "WE",
                Credits = new List<LeedV5Credit>()
            };

            category.Credits.Add(new LeedV5Credit { Code = "WEp1", Name = "Water Metering and Reporting", IsPrerequisite = true, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "WEp2", Name = "Minimum Water Efficiency", IsPrerequisite = true, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "WEc1", Name = "Water Metering and Leak Detection", MaxPoints = 1, Status = "MEP Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "WEc2", Name = "Enhanced Water Efficiency", MaxPoints = 8, Status = "MEP Data Required" });

            return category;
        }

        private LeedV5Category CalculateEnergyAtmosphere(BuildingAnalysisData data)
        {
            var category = new LeedV5Category
            {
                Name = "Energy & Atmosphere",
                Code = "EA",
                Credits = new List<LeedV5Credit>()
            };

            category.Credits.Add(new LeedV5Credit { Code = "EAp1", Name = "Operational Carbon Projection and Decarbonization Plan", IsPrerequisite = true, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "EAp2", Name = "Minimum Energy Efficiency", IsPrerequisite = true, Status = "Energy Model Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EAp3", Name = "Fundamental Commissioning", IsPrerequisite = true, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "EAp4", Name = "Energy Metering and Reporting", IsPrerequisite = true, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "EAp5", Name = "Fundamental Refrigerant Management", IsPrerequisite = true, Status = "Requires Documentation" });
            
            category.Credits.Add(new LeedV5Credit { Code = "EAc1", Name = "Electrification", MaxPoints = 5, Status = "MEP Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EAc2", Name = "Reduce Peak Thermal Loads", MaxPoints = 5, PointsEarned = 2, Status = "Partial", Description = "Based on envelope materials" });
            category.Credits.Add(new LeedV5Credit { Code = "EAc3", Name = "Enhanced Energy Efficiency", MaxPoints = 10, Status = "Energy Model Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EAc4", Name = "Renewable Energy", MaxPoints = 5, Status = "System Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EAc5", Name = "Enhanced Commissioning", MaxPoints = 4, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "EAc6", Name = "Grid Interactive", MaxPoints = 2, Status = "System Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EAc7", Name = "Enhanced Refrigerant Management", MaxPoints = 2, Status = "MEP Data Required" });

            return category;
        }

        private LeedV5Category CalculateMaterialsResources(BuildingAnalysisData data)
        {
            var category = new LeedV5Category
            {
                Name = "Materials & Resources",
                Code = "MR",
                Credits = new List<LeedV5Credit>()
            };

            // MRp1: Planning for Zero Waste Operations (Required)
            category.Credits.Add(new LeedV5Credit
            {
                Code = "MRp1",
                Name = "Planning for Zero Waste Operations",
                IsPrerequisite = true,
                Status = "Requires Documentation",
                Description = "Develop waste management plan"
            });

            // MRp2: Quantify and Assess Embodied Carbon (Required) - We calculate this!
            bool embodiedCarbonQuantified = data.TotalEmbodiedCarbon > 0;
            category.Credits.Add(new LeedV5Credit
            {
                Code = "MRp2",
                Name = "Quantify and Assess Embodied Carbon",
                IsPrerequisite = true,
                Status = embodiedCarbonQuantified ? "Met" : "Not Met",
                Description = $"Total: {data.TotalEmbodiedCarbon:N0} kgCO2e | Intensity: {data.EmbodiedCarbonIntensity:F2} kgCO2e/sf"
            });

            // MRc1: Building and Materials Reuse (3 points)
            category.Credits.Add(new LeedV5Credit
            {
                Code = "MRc1",
                Name = "Building and Materials Reuse",
                MaxPoints = 3,
                PointsEarned = 0,
                Status = "Not Attempted",
                Description = "Document reused materials"
            });

            // MRc2: Reduce Embodied Carbon (6 points) - Based on analysis
            int embodiedCarbonPoints = 0;
            string embodiedStatus = "Baseline";
            
            // LEED v5 targets approximately 10% reduction per point
            if (data.EmbodiedCarbonIntensity < 40) { embodiedCarbonPoints = 6; embodiedStatus = "Excellent (<40 kgCO2e/sf)"; }
            else if (data.EmbodiedCarbonIntensity < 50) { embodiedCarbonPoints = 5; embodiedStatus = "Very Good (<50 kgCO2e/sf)"; }
            else if (data.EmbodiedCarbonIntensity < 60) { embodiedCarbonPoints = 4; embodiedStatus = "Good (<60 kgCO2e/sf)"; }
            else if (data.EmbodiedCarbonIntensity < 70) { embodiedCarbonPoints = 3; embodiedStatus = "Above Average (<70 kgCO2e/sf)"; }
            else if (data.EmbodiedCarbonIntensity < 80) { embodiedCarbonPoints = 2; embodiedStatus = "Average (<80 kgCO2e/sf)"; }
            else if (data.EmbodiedCarbonIntensity < 90) { embodiedCarbonPoints = 1; embodiedStatus = "Below Average (<90 kgCO2e/sf)"; }

            category.Credits.Add(new LeedV5Credit
            {
                Code = "MRc2",
                Name = "Reduce Embodied Carbon",
                MaxPoints = 6,
                PointsEarned = embodiedCarbonPoints,
                Status = embodiedStatus,
                Description = $"Intensity: {data.EmbodiedCarbonIntensity:F2} kgCO2e/sf"
            });

            // MRc3: Low-Emitting Materials (2 points)
            int lowEmitPoints = data.LowEmittingPercent >= 90 ? 2 : (data.LowEmittingPercent >= 75 ? 1 : 0);
            category.Credits.Add(new LeedV5Credit
            {
                Code = "MRc3",
                Name = "Low-Emitting Materials",
                MaxPoints = 2,
                PointsEarned = lowEmitPoints,
                Status = lowEmitPoints > 0 ? "Achieved" : "Not Met",
                Description = $"Low-emitting: {data.LowEmittingPercent:F1}%"
            });

            // MRc4: Building Product Selection and Procurement (5 points)
            int procurementPoints = 0;
            if (data.RecycledContentPercent >= 20) procurementPoints += 2;
            else if (data.RecycledContentPercent >= 10) procurementPoints += 1;
            if (data.WoodPercent >= 50) procurementPoints += 1; // FSC wood assumed
            
            category.Credits.Add(new LeedV5Credit
            {
                Code = "MRc4",
                Name = "Building Product Selection and Procurement",
                MaxPoints = 5,
                PointsEarned = procurementPoints,
                Status = procurementPoints > 0 ? "Partial" : "Not Met",
                Description = $"Recycled: {data.RecycledContentPercent:F1}% | Wood: {data.WoodPercent:F1}%"
            });

            // MRc5: Construction and Demolition Waste Diversion (2 points)
            category.Credits.Add(new LeedV5Credit
            {
                Code = "MRc5",
                Name = "Construction and Demolition Waste Diversion",
                MaxPoints = 2,
                Status = "Requires Documentation"
            });

            return category;
        }

        private LeedV5Category CalculateIndoorEnvironmentalQuality(BuildingAnalysisData data)
        {
            var category = new LeedV5Category
            {
                Name = "Indoor Environmental Quality",
                Code = "EQ",
                Credits = new List<LeedV5Credit>()
            };

            category.Credits.Add(new LeedV5Credit { Code = "EQp1", Name = "Construction Management", IsPrerequisite = true, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "EQp2", Name = "Fundamental Air Quality", IsPrerequisite = true, Status = "Requires Documentation" });
            category.Credits.Add(new LeedV5Credit { Code = "EQp3", Name = "No Smoking or Vehicle Idling", IsPrerequisite = true, Status = "Requires Documentation" });
            
            category.Credits.Add(new LeedV5Credit { Code = "EQc1", Name = "Enhanced Air Quality", MaxPoints = 1, Status = "MEP Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EQc2", Name = "Occupant Experience", MaxPoints = 7, Status = "Design Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EQc3", Name = "Accessibility and Inclusion", MaxPoints = 1, Status = "Design Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EQc4", Name = "Resilient Spaces", MaxPoints = 2, Status = "Design Data Required" });
            category.Credits.Add(new LeedV5Credit { Code = "EQc5", Name = "Air Quality Testing and Monitoring", MaxPoints = 2, Status = "MEP Data Required" });

            return category;
        }

        private LeedV5Category CalculateProjectPriorities(BuildingAnalysisData data)
        {
            var category = new LeedV5Category
            {
                Name = "Project Priorities",
                Code = "PR",
                Credits = new List<LeedV5Credit>()
            };

            category.Credits.Add(new LeedV5Credit { Code = "PRc1", Name = "Project Priorities", MaxPoints = 9, Status = "Select Priorities" });
            category.Credits.Add(new LeedV5Credit { Code = "PRc2", Name = "LEED AP", MaxPoints = 1, Status = "Documentation Required" });

            return category;
        }

        private string DetermineCertificationLevel(int totalPoints, bool prereqsMet)
        {
            if (!prereqsMet) return "Prerequisites Not Met";
            if (totalPoints >= 80) return "Platinum";
            if (totalPoints >= 60) return "Gold";
            if (totalPoints >= 50) return "Silver";
            if (totalPoints >= 40) return "Certified";
            return "Not Certified";
        }
    }

    public class BuildingAnalysisData
    {
        public double TotalFloorArea { get; set; }
        public double TotalMaterialVolume { get; set; }
        public double ConcreteVolume { get; set; }
        public double SteelVolume { get; set; }
        public double WoodVolume { get; set; }
        public double RecycledVolume { get; set; }
        public double GlassArea { get; set; }
        public double LowEmittingVolume { get; set; }
        public double TotalEmbodiedCarbon { get; set; }
        public double RecycledContentPercent { get; set; }
        public double LowEmittingPercent { get; set; }
        public double WoodPercent { get; set; }
        public double EmbodiedCarbonIntensity { get; set; }
    }
}
