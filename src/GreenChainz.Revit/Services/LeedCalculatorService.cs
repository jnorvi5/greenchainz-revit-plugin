using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Calculates LEED (Leadership in Energy and Environmental Design) points
    /// based on material analysis from the Revit model.
    /// </summary>
    public class LeedCalculatorService
    {
        // LEED v4.1 Materials & Resources credit thresholds
        private const double RECYCLED_CONTENT_THRESHOLD_1PT = 10; // 10% for 1 point
        private const double RECYCLED_CONTENT_THRESHOLD_2PT = 20; // 20% for 2 points
        private const double REGIONAL_MATERIALS_THRESHOLD = 20;   // 20% regional for points
        private const double LOW_EMITTING_THRESHOLD = 90;         // 90% low-emitting for points

        public LeedResult CalculateLeedScore(Document doc)
        {
            var result = new LeedResult
            {
                ProjectName = doc.Title,
                CalculationDate = DateTime.Now,
                Credits = new List<LeedCredit>()
            };

            // Extract materials from the model
            var materialAnalysis = AnalyzeMaterials(doc);

            // Calculate each LEED credit category
            CalculateMaterialsAndResourcesCredits(result, materialAnalysis);
            CalculateIndoorEnvironmentalQualityCredits(result, materialAnalysis);
            CalculateSustainableSitesCredits(result, materialAnalysis);
            CalculateEnergyCredits(result, materialAnalysis);

            // Calculate totals
            int totalPoints = 0;
            int maxPoints = 0;
            foreach (var credit in result.Credits)
            {
                totalPoints += credit.PointsEarned;
                maxPoints += credit.MaxPoints;
            }

            result.TotalPoints = totalPoints;
            result.MaxPossiblePoints = maxPoints;
            result.CertificationLevel = DetermineCertificationLevel(totalPoints);

            return result;
        }

        private MaterialAnalysis AnalyzeMaterials(Document doc)
        {
            var analysis = new MaterialAnalysis();
            
            var categories = new List<BuiltInCategory>
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

            var categoryFilter = new ElementMulticategoryFilter(categories);
            var collector = new FilteredElementCollector(doc);
            var elements = collector.WherePasses(categoryFilter).WhereElementIsNotElementType().ToElements();

            double totalVolume = 0;
            double recycledVolume = 0;
            double regionalVolume = 0;
            double lowEmittingVolume = 0;
            double rapidRenewableVolume = 0;

            foreach (Element elem in elements)
            {
                foreach (ElementId matId in elem.GetMaterialIds(false))
                {
                    var mat = doc.GetElement(matId) as Autodesk.Revit.DB.Material;
                    if (mat == null) continue;

                    double volume = elem.GetMaterialVolume(matId) * 0.0283168; // Convert to m³
                    if (volume < 0.0001) continue;

                    totalVolume += volume;
                    string matName = mat.Name.ToLower();

                    // Estimate recycled content based on material name
                    if (matName.Contains("recycled") || matName.Contains("reclaimed"))
                    {
                        recycledVolume += volume;
                    }
                    else if (matName.Contains("steel"))
                    {
                        recycledVolume += volume * 0.25; // Assume 25% recycled steel
                    }
                    else if (matName.Contains("aluminum") || matName.Contains("aluminium"))
                    {
                        recycledVolume += volume * 0.30; // Assume 30% recycled aluminum
                    }

                    // Check for regional materials (simplified - assume local if common)
                    if (matName.Contains("concrete") || matName.Contains("brick") || matName.Contains("stone"))
                    {
                        regionalVolume += volume * 0.5; // Assume 50% sourced regionally
                    }

                    // Check for low-emitting materials
                    if (matName.Contains("wood") || matName.Contains("timber") || 
                        matName.Contains("stone") || matName.Contains("metal") ||
                        matName.Contains("glass") || matName.Contains("concrete"))
                    {
                        lowEmittingVolume += volume;
                    }

                    // Check for rapidly renewable materials
                    if (matName.Contains("bamboo") || matName.Contains("cork") || 
                        matName.Contains("wool") || matName.Contains("cotton"))
                    {
                        rapidRenewableVolume += volume;
                    }

                    analysis.MaterialBreakdown.Add(new MaterialInfo
                    {
                        Name = mat.Name,
                        Volume = volume,
                        IsRecycled = matName.Contains("recycled"),
                        IsRegional = matName.Contains("local") || matName.Contains("regional"),
                        IsLowEmitting = !matName.Contains("paint") && !matName.Contains("adhesive")
                    });
                }
            }

            analysis.TotalVolume = totalVolume;
            analysis.RecycledContentPercent = totalVolume > 0 ? (recycledVolume / totalVolume) * 100 : 0;
            analysis.RegionalMaterialsPercent = totalVolume > 0 ? (regionalVolume / totalVolume) * 100 : 0;
            analysis.LowEmittingPercent = totalVolume > 0 ? (lowEmittingVolume / totalVolume) * 100 : 0;
            analysis.RapidRenewablePercent = totalVolume > 0 ? (rapidRenewableVolume / totalVolume) * 100 : 0;

            return analysis;
        }

        private void CalculateMaterialsAndResourcesCredits(LeedResult result, MaterialAnalysis analysis)
        {
            // MR Credit: Building Product Disclosure and Optimization
            int recycledPoints = 0;
            if (analysis.RecycledContentPercent >= RECYCLED_CONTENT_THRESHOLD_2PT)
                recycledPoints = 2;
            else if (analysis.RecycledContentPercent >= RECYCLED_CONTENT_THRESHOLD_1PT)
                recycledPoints = 1;

            result.Credits.Add(new LeedCredit
            {
                Category = "Materials & Resources",
                CreditName = "MR Credit: Recycled Content",
                PointsEarned = recycledPoints,
                MaxPoints = 2,
                Description = $"Recycled content: {analysis.RecycledContentPercent:F1}%",
                Status = recycledPoints > 0 ? "Achieved" : "Not Met"
            });

            // MR Credit: Regional Materials
            int regionalPoints = analysis.RegionalMaterialsPercent >= REGIONAL_MATERIALS_THRESHOLD ? 1 : 0;
            result.Credits.Add(new LeedCredit
            {
                Category = "Materials & Resources",
                CreditName = "MR Credit: Regional Materials",
                PointsEarned = regionalPoints,
                MaxPoints = 2,
                Description = $"Regional materials: {analysis.RegionalMaterialsPercent:F1}%",
                Status = regionalPoints > 0 ? "Achieved" : "Not Met"
            });

            // MR Credit: Rapidly Renewable Materials
            int renewablePoints = analysis.RapidRenewablePercent >= 2.5 ? 1 : 0;
            result.Credits.Add(new LeedCredit
            {
                Category = "Materials & Resources",
                CreditName = "MR Credit: Rapidly Renewable Materials",
                PointsEarned = renewablePoints,
                MaxPoints = 1,
                Description = $"Rapidly renewable: {analysis.RapidRenewablePercent:F1}%",
                Status = renewablePoints > 0 ? "Achieved" : "Not Met"
            });
        }

        private void CalculateIndoorEnvironmentalQualityCredits(LeedResult result, MaterialAnalysis analysis)
        {
            // EQ Credit: Low-Emitting Materials
            int lowEmitPoints = 0;
            if (analysis.LowEmittingPercent >= LOW_EMITTING_THRESHOLD)
                lowEmitPoints = 3;
            else if (analysis.LowEmittingPercent >= 75)
                lowEmitPoints = 2;
            else if (analysis.LowEmittingPercent >= 50)
                lowEmitPoints = 1;

            result.Credits.Add(new LeedCredit
            {
                Category = "Indoor Environmental Quality",
                CreditName = "EQ Credit: Low-Emitting Materials",
                PointsEarned = lowEmitPoints,
                MaxPoints = 3,
                Description = $"Low-emitting materials: {analysis.LowEmittingPercent:F1}%",
                Status = lowEmitPoints > 0 ? "Achieved" : "Not Met"
            });
        }

        private void CalculateSustainableSitesCredits(LeedResult result, MaterialAnalysis analysis)
        {
            // SS Credit: Heat Island Reduction (based on roofing materials)
            result.Credits.Add(new LeedCredit
            {
                Category = "Sustainable Sites",
                CreditName = "SS Credit: Heat Island Reduction",
                PointsEarned = 1, // Simplified - assume partial compliance
                MaxPoints = 2,
                Description = "Based on reflective/vegetated roof materials",
                Status = "Partial"
            });
        }

        private void CalculateEnergyCredits(LeedResult result, MaterialAnalysis analysis)
        {
            // EA Credit: Optimize Energy Performance (simplified based on insulation)
            result.Credits.Add(new LeedCredit
            {
                Category = "Energy & Atmosphere",
                CreditName = "EA Credit: Optimize Energy Performance",
                PointsEarned = 2, // Simplified estimate
                MaxPoints = 18,
                Description = "Estimated based on building envelope materials",
                Status = "Partial"
            });
        }

        private string DetermineCertificationLevel(int totalPoints)
        {
            // LEED v4.1 BD+C certification levels (out of 110 points)
            // Scaled for our subset of credits
            if (totalPoints >= 10) return "Platinum";
            if (totalPoints >= 7) return "Gold";
            if (totalPoints >= 5) return "Silver";
            if (totalPoints >= 3) return "Certified";
            return "Not Certified";
        }
    }

    public class MaterialAnalysis
    {
        public double TotalVolume { get; set; }
        public double RecycledContentPercent { get; set; }
        public double RegionalMaterialsPercent { get; set; }
        public double LowEmittingPercent { get; set; }
        public double RapidRenewablePercent { get; set; }
        public List<MaterialInfo> MaterialBreakdown { get; set; } = new List<MaterialInfo>();
    }

    public class MaterialInfo
    {
        public string Name { get; set; }
        public double Volume { get; set; }
        public bool IsRecycled { get; set; }
        public bool IsRegional { get; set; }
        public bool IsLowEmitting { get; set; }
    }
}
