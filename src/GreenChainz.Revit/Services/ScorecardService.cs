using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Calculates enterprise-grade sustainability scorecard
    /// Focus: EPD Coverage, Embodied Carbon (GWP), Verification Tier
    /// </summary>
    public class ScorecardService
    {
        // CLF 2021 Baseline GWP values (kgCO2e per m³ or per unit)
        private static readonly Dictionary<string, double> GWP_BASELINES = new Dictionary<string, double>
        {
            { "concrete", 340 },      // kgCO2e/m³
            { "steel", 12740 },       // kgCO2e/m³ (density adjusted)
            { "aluminum", 34560 },    // kgCO2e/m³
            { "wood", 110 },          // kgCO2e/m³
            { "glass", 3750 },        // kgCO2e/m³
            { "gypsum", 160 },        // kgCO2e/m³
            { "insulation", 50 },     // kgCO2e/m³
            { "brick", 200 },         // kgCO2e/m³
            { "default", 150 }
        };

        public SustainabilityScorecard GenerateScorecard(Document doc)
        {
            var scorecard = new SustainabilityScorecard
            {
                ProjectName = doc.Title,
                GeneratedDate = DateTime.Now
            };

            // Extract all materials from model
            var materialScores = ExtractMaterialScores(doc);
            scorecard.Materials = materialScores;

            // Calculate EPD Coverage
            scorecard.EpdScore = CalculateEpdCoverage(materialScores);

            // Calculate GWP Score
            scorecard.GwpScore = CalculateGwpScore(materialScores, doc);

            // Calculate Verification Tier
            scorecard.VerificationScore = CalculateVerificationTier(materialScores);

            // Calculate Overall Score and Grade
            CalculateOverallScore(scorecard);

            // Compliance checks
            scorecard.LeedCompliant = scorecard.EpdScore.CoveragePercent >= 20;
            scorecard.BuyCleanCompliant = scorecard.GwpScore.ReductionPercent >= 0;
            scorecard.EstimatedLeedPoints = EstimateLeedPoints(scorecard);

            return scorecard;
        }

        private List<MaterialScore> ExtractMaterialScores(Document doc)
        {
            var scores = new Dictionary<string, MaterialScore>();

            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls, BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Roofs, BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming, BuiltInCategory.OST_Ceilings
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

                    string name = mat.Name;
                    double volume = elem.GetMaterialVolume(matId) * 0.0283168; // Convert to m³

                    if (volume < 0.001) continue;

                    if (!scores.ContainsKey(name))
                    {
                        var category = DetermineCategory(name);
                        var baseline = GetBaseline(category);
                        var gwp = EstimateGwp(name, category);
                        var tier = DetermineVerificationTier(name);

                        scores[name] = new MaterialScore
                        {
                            Name = name,
                            Category = category,
                            Quantity = 0,
                            Unit = "m³",
                            HasEpd = HasEpdAvailable(name),
                            Gwp = gwp,
                            GwpBaseline = baseline,
                            VerificationTier = tier,
                            Certifications = GetCertifications(name)
                        };
                    }

                    scores[name].Quantity += volume;
                    scores[name].TotalGwp = scores[name].Quantity * scores[name].Gwp;
                    scores[name].GwpReduction = scores[name].GwpBaseline > 0 
                        ? ((scores[name].GwpBaseline - scores[name].Gwp) / scores[name].GwpBaseline) * 100 
                        : 0;
                }
            }

            return scores.Values.ToList();
        }

        private EpdCoverage CalculateEpdCoverage(List<MaterialScore> materials)
        {
            int total = materials.Count;
            int withEpd = materials.Count(m => m.HasEpd);
            double percent = total > 0 ? (double)withEpd / total * 100 : 0;

            string grade;
            if (percent >= 80) grade = "A";
            else if (percent >= 60) grade = "B";
            else if (percent >= 40) grade = "C";
            else if (percent >= 20) grade = "D";
            else grade = "F";

            return new EpdCoverage
            {
                TotalMaterials = total,
                MaterialsWithEpd = withEpd,
                CoveragePercent = percent,
                Grade = grade
            };
        }

        private EmbodiedCarbonScore CalculateGwpScore(List<MaterialScore> materials, Document doc)
        {
            double totalGwp = materials.Sum(m => m.TotalGwp);
            double totalBaseline = materials.Sum(m => m.Quantity * m.GwpBaseline);

            // Get floor area for intensity calculation
            double floorArea = 0;
            var floors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType();
            foreach (var floor in floors)
            {
                var areaParam = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                if (areaParam != null)
                    floorArea += areaParam.AsDouble() * 0.0929; // Convert sf to m²
            }

            double gwpPerSqM = floorArea > 0 ? totalGwp / floorArea : 0;
            double gwpPerSqFt = gwpPerSqM * 0.0929;
            double reduction = totalBaseline > 0 ? ((totalBaseline - totalGwp) / totalBaseline) * 100 : 0;

            string grade;
            if (reduction >= 40) grade = "A";
            else if (reduction >= 20) grade = "B";
            else if (reduction >= 0) grade = "C";
            else if (reduction >= -20) grade = "D";
            else grade = "F";

            return new EmbodiedCarbonScore
            {
                TotalGwp = totalGwp,
                GwpPerSqM = gwpPerSqM,
                GwpPerSqFt = gwpPerSqFt,
                BaselineGwp = totalBaseline,
                ReductionPercent = reduction,
                Grade = grade
            };
        }

        private VerificationTier CalculateVerificationTier(List<MaterialScore> materials)
        {
            int platinum = materials.Count(m => m.VerificationTier == "Platinum");
            int gold = materials.Count(m => m.VerificationTier == "Gold");
            int silver = materials.Count(m => m.VerificationTier == "Silver");
            int none = materials.Count(m => m.VerificationTier == "None");

            string tier;
            int score;
            if (platinum > gold && platinum > silver)
            {
                tier = "Platinum";
                score = 3;
            }
            else if (gold >= platinum && gold > silver)
            {
                tier = "Gold";
                score = 2;
            }
            else if (silver > 0)
            {
                tier = "Silver";
                score = 1;
            }
            else
            {
                tier = "Unverified";
                score = 0;
            }

            return new VerificationTier
            {
                Tier = tier,
                TierScore = score,
                PlatinumCount = platinum,
                GoldCount = gold,
                SilverCount = silver,
                UnverifiedCount = none
            };
        }

        private void CalculateOverallScore(SustainabilityScorecard scorecard)
        {
            // Weight: EPD 30%, GWP 50%, Verification 20%
            int epdScore = GradeToScore(scorecard.EpdScore.Grade);
            int gwpScore = GradeToScore(scorecard.GwpScore.Grade);
            int verScore = scorecard.VerificationScore.TierScore * 25;

            scorecard.OverallScore = (int)(epdScore * 0.30 + gwpScore * 0.50 + verScore * 0.20);

            if (scorecard.OverallScore >= 90) scorecard.OverallGrade = "A";
            else if (scorecard.OverallScore >= 80) scorecard.OverallGrade = "B";
            else if (scorecard.OverallScore >= 70) scorecard.OverallGrade = "C";
            else if (scorecard.OverallScore >= 60) scorecard.OverallGrade = "D";
            else scorecard.OverallGrade = "F";
        }

        private int GradeToScore(string grade)
        {
            switch (grade)
            {
                case "A": return 100;
                case "B": return 85;
                case "C": return 70;
                case "D": return 55;
                default: return 40;
            }
        }

        private int EstimateLeedPoints(SustainabilityScorecard scorecard)
        {
            int points = 0;

            // MRc2: Building Product Disclosure (EPD)
            if (scorecard.EpdScore.CoveragePercent >= 20) points += 1;
            if (scorecard.EpdScore.CoveragePercent >= 40) points += 1;

            // MRc2: Embodied Carbon Reduction
            if (scorecard.GwpScore.ReductionPercent >= 5) points += 1;
            if (scorecard.GwpScore.ReductionPercent >= 10) points += 1;
            if (scorecard.GwpScore.ReductionPercent >= 20) points += 1;
            if (scorecard.GwpScore.ReductionPercent >= 30) points += 1;

            return points;
        }

        private string DetermineCategory(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("concrete") || lower.Contains("cement")) return "Concrete";
            if (lower.Contains("steel") || lower.Contains("metal")) return "Steel";
            if (lower.Contains("aluminum") || lower.Contains("aluminium")) return "Aluminum";
            if (lower.Contains("wood") || lower.Contains("timber")) return "Wood";
            if (lower.Contains("glass") || lower.Contains("glazing")) return "Glass";
            if (lower.Contains("gypsum") || lower.Contains("drywall")) return "Gypsum";
            if (lower.Contains("insulation")) return "Insulation";
            if (lower.Contains("brick") || lower.Contains("masonry")) return "Brick";
            return "Other";
        }

        private double GetBaseline(string category)
        {
            string key = category.ToLower();
            return GWP_BASELINES.ContainsKey(key) ? GWP_BASELINES[key] : GWP_BASELINES["default"];
        }

        private double EstimateGwp(string name, string category)
        {
            // Lower GWP for materials with "low carbon", "recycled", "green" in name
            string lower = name.ToLower();
            double baseline = GetBaseline(category);

            if (lower.Contains("low carbon") || lower.Contains("low-carbon")) return baseline * 0.7;
            if (lower.Contains("recycled")) return baseline * 0.6;
            if (lower.Contains("green") || lower.Contains("eco")) return baseline * 0.8;
            if (lower.Contains("mass timber") || lower.Contains("clt")) return -500; // Carbon negative

            return baseline;
        }

        private bool HasEpdAvailable(string name)
        {
            // Common materials with EPDs
            string lower = name.ToLower();
            return lower.Contains("concrete") || lower.Contains("steel") || 
                   lower.Contains("gypsum") || lower.Contains("insulation") ||
                   lower.Contains("glass") || lower.Contains("aluminum");
        }

        private string DetermineVerificationTier(string name)
        {
            string lower = name.ToLower();
            
            // Platinum: Third-party verified + chain of custody (FSC, ISCC, etc.)
            if (lower.Contains("fsc") || lower.Contains("iscc") || lower.Contains("mass timber"))
                return "Platinum";
            
            // Gold: Has EPD
            if (HasEpdAvailable(name))
                return "Gold";
            
            // Silver: Self-declared
            if (lower.Contains("recycled") || lower.Contains("green") || lower.Contains("eco"))
                return "Silver";
            
            return "None";
        }

        private List<string> GetCertifications(string name)
        {
            var certs = new List<string>();
            string lower = name.ToLower();

            if (HasEpdAvailable(name)) certs.Add("EPD");
            if (lower.Contains("fsc")) certs.Add("FSC");
            if (lower.Contains("leed")) certs.Add("LEED");
            if (lower.Contains("greenguard")) certs.Add("GREENGUARD");
            if (lower.Contains("iso")) certs.Add("ISO 14001");

            return certs;
        }
    }
}
