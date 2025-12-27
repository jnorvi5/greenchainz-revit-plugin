using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Calculates enterprise-grade sustainability scorecard with regional adjustments
    /// </summary>
    public class ScorecardService
    {
        private readonly LocationService _locationService;

        public ScorecardService()
        {
            _locationService = new LocationService();
        }

        public SustainabilityScorecard GenerateScorecard(Document doc)
        {
            var scorecard = new SustainabilityScorecard
            {
                ProjectName = doc.Title,
                GeneratedDate = DateTime.Now
            };

            // Get project location
            var projectLocation = _locationService.GetProjectLocation(doc);
            scorecard.Location = new ProjectLocationInfo
            {
                Address = projectLocation.Address,
                State = projectLocation.State,
                Region = projectLocation.Region,
                ClimateZone = projectLocation.ClimateZone,
                Latitude = projectLocation.Latitude,
                Longitude = projectLocation.Longitude,
                GridCarbonIntensity = projectLocation.GridCarbonIntensity,
                RegionalMultiplier = RegionalCarbonFactors.GetRegionalMultiplier(projectLocation.State)
            };

            // Extract all materials with regional adjustments
            var materialScores = ExtractMaterialScores(doc, projectLocation);
            scorecard.Materials = materialScores;

            // Calculate EPD Coverage
            scorecard.EpdScore = CalculateEpdCoverage(materialScores);

            // Calculate GWP Score with regional adjustments
            scorecard.GwpScore = CalculateGwpScore(materialScores, doc, projectLocation);

            // Calculate Verification Tier
            scorecard.VerificationScore = CalculateVerificationTier(materialScores);

            // Calculate Overall Score and Grade
            CalculateOverallScore(scorecard);

            // Compliance checks
            scorecard.LeedCompliant = scorecard.EpdScore.CoveragePercent >= 20;
            
            // Buy Clean compliance
            var buyClean = RegionalCarbonFactors.GetBuyCleanRequirements(projectLocation.State);
            scorecard.BuyCleanInfo = CalculateBuyCleanCompliance(materialScores, buyClean);
            scorecard.BuyCleanCompliant = !buyClean.HasRequirements || 
                (scorecard.BuyCleanInfo.ConcreteCompliant && scorecard.BuyCleanInfo.SteelCompliant);

            // LEED points including regional
            scorecard.RegionalPriorityCredits = RegionalCarbonFactors.GetRegionalPriorityCredits(projectLocation.State);
            scorecard.RegionalBonusPoints = Math.Min(4, scorecard.RegionalPriorityCredits.Count);
            scorecard.EstimatedLeedPoints = EstimateLeedPoints(scorecard) + scorecard.RegionalBonusPoints;

            return scorecard;
        }

        private List<MaterialScore> ExtractMaterialScores(Document doc, ProjectLocation location)
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
                    double volume = elem.GetMaterialVolume(matId) * 0.0283168;

                    if (volume < 0.001) continue;

                    if (!scores.ContainsKey(name))
                    {
                        var category = DetermineCategory(name);
                        var gwp = GetRegionalGwp(name, category, location.State);
                        var baseline = GetBaseline(category);
                        var tier = DetermineVerificationTier(name);

                        // Check Buy Clean compliance
                        var buyClean = RegionalCarbonFactors.GetBuyCleanRequirements(location.State);
                        bool meetsBuyClean = !buyClean.HasRequirements;
                        if (buyClean.HasRequirements)
                        {
                            if (category == "Concrete") meetsBuyClean = gwp <= buyClean.ConcreteGwpLimit;
                            else if (category == "Steel") meetsBuyClean = gwp <= buyClean.SteelGwpLimit;
                            else meetsBuyClean = true;
                        }

                        scores[name] = new MaterialScore
                        {
                            Name = name,
                            Category = category,
                            Quantity = 0,
                            Unit = "m³",
                            HasEpd = HasEpdAvailable(name),
                            Gwp = gwp,
                            RegionalGwp = gwp,
                            GwpBaseline = baseline,
                            VerificationTier = tier,
                            Certifications = GetCertifications(name),
                            MeetsBuyClean = meetsBuyClean
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

        private double GetRegionalGwp(string name, string category, string state)
        {
            string lower = name.ToLower();

            switch (category)
            {
                case "Concrete":
                    string strength = "4000psi";
                    if (lower.Contains("5000")) strength = "5000psi";
                    else if (lower.Contains("6000")) strength = "6000psi";
                    else if (lower.Contains("3000")) strength = "3000psi";
                    return RegionalCarbonFactors.GetConcreteGwp(state, strength);

                case "Steel":
                    string steelType = "structural";
                    if (lower.Contains("rebar")) steelType = "rebar";
                    else if (lower.Contains("deck")) steelType = "decking";
                    else if (lower.Contains("plate")) steelType = "plate";
                    return RegionalCarbonFactors.GetSteelGwp(state, steelType);

                case "Wood":
                    string woodType = "lumber";
                    if (lower.Contains("clt") || lower.Contains("cross")) woodType = "clt";
                    else if (lower.Contains("glulam") || lower.Contains("glue")) woodType = "glulam";
                    else if (lower.Contains("plywood")) woodType = "plywood";
                    return RegionalCarbonFactors.GetWoodGwp(state, woodType);

                default:
                    double baseGwp = GetBaseline(category);
                    return baseGwp * RegionalCarbonFactors.GetRegionalMultiplier(state);
            }
        }

        private BuyCleanComplianceInfo CalculateBuyCleanCompliance(List<MaterialScore> materials, BuyCleanRequirements buyClean)
        {
            var info = new BuyCleanComplianceInfo
            {
                HasRequirements = buyClean.HasRequirements,
                PolicyName = buyClean.Name,
                ConcreteLimit = buyClean.ConcreteGwpLimit,
                SteelLimit = buyClean.SteelGwpLimit
            };

            if (!buyClean.HasRequirements) return info;

            // Calculate average GWP for concrete and steel
            var concrete = materials.Where(m => m.Category == "Concrete").ToList();
            var steel = materials.Where(m => m.Category == "Steel").ToList();

            if (concrete.Count > 0)
            {
                double totalVol = concrete.Sum(m => m.Quantity);
                info.ActualConcreteGwp = totalVol > 0 ? concrete.Sum(m => m.TotalGwp) / totalVol : 0;
                info.ConcreteCompliant = info.ActualConcreteGwp <= buyClean.ConcreteGwpLimit;
            }
            else
            {
                info.ConcreteCompliant = true;
            }

            if (steel.Count > 0)
            {
                double totalVol = steel.Sum(m => m.Quantity);
                info.ActualSteelGwp = totalVol > 0 ? steel.Sum(m => m.TotalGwp) / totalVol : 0;
                info.SteelCompliant = info.ActualSteelGwp <= buyClean.SteelGwpLimit;
            }
            else
            {
                info.SteelCompliant = true;
            }

            return info;
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

        private EmbodiedCarbonScore CalculateGwpScore(List<MaterialScore> materials, Document doc, ProjectLocation location)
        {
            double totalGwp = materials.Sum(m => m.TotalGwp);
            double totalBaseline = materials.Sum(m => m.Quantity * m.GwpBaseline);

            double floorArea = 0;
            var floors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType();
            foreach (var floor in floors)
            {
                var areaParam = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                if (areaParam != null)
                    floorArea += areaParam.AsDouble() * 0.0929;
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

            double regionalMultiplier = RegionalCarbonFactors.GetRegionalMultiplier(location.State);

            return new EmbodiedCarbonScore
            {
                TotalGwp = totalGwp,
                GwpPerSqM = gwpPerSqM,
                GwpPerSqFt = gwpPerSqFt,
                BaselineGwp = totalBaseline,
                ReductionPercent = reduction,
                Grade = grade,
                RegionalAdjustedGwp = totalGwp * regionalMultiplier
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
            if (platinum > gold && platinum > silver) { tier = "Platinum"; score = 3; }
            else if (gold >= platinum && gold > silver) { tier = "Gold"; score = 2; }
            else if (silver > 0) { tier = "Silver"; score = 1; }
            else { tier = "Unverified"; score = 0; }

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

        private int GradeToScore(string grade) => grade switch
        {
            "A" => 100, "B" => 85, "C" => 70, "D" => 55, _ => 40
        };

        private int EstimateLeedPoints(SustainabilityScorecard scorecard)
        {
            int points = 0;
            if (scorecard.EpdScore.CoveragePercent >= 20) points += 1;
            if (scorecard.EpdScore.CoveragePercent >= 40) points += 1;
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

        private double GetBaseline(string category) => category switch
        {
            "Concrete" => 340, "Steel" => 12740, "Aluminum" => 34560,
            "Wood" => 110, "Glass" => 3750, "Gypsum" => 160,
            "Insulation" => 50, "Brick" => 200, _ => 150
        };

        private bool HasEpdAvailable(string name)
        {
            string lower = name.ToLower();
            return lower.Contains("concrete") || lower.Contains("steel") || 
                   lower.Contains("gypsum") || lower.Contains("insulation") ||
                   lower.Contains("glass") || lower.Contains("aluminum");
        }

        private string DetermineVerificationTier(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("fsc") || lower.Contains("iscc") || lower.Contains("mass timber")) return "Platinum";
            if (HasEpdAvailable(name)) return "Gold";
            if (lower.Contains("recycled") || lower.Contains("green") || lower.Contains("eco")) return "Silver";
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
            return certs;
        }
    }
}
