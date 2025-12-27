using System.Collections.Generic;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Regional carbon factors based on CLF, EPA, and local benchmarks
    /// </summary>
    public static class RegionalCarbonFactors
    {
        /// <summary>
        /// Get regional adjustment multiplier for embodied carbon
        /// Some regions have stricter standards = lower carbon materials available
        /// </summary>
        public static double GetRegionalMultiplier(string state)
        {
            var multipliers = new Dictionary<string, double>
            {
                // Progressive states with Buy Clean policies = lower carbon materials
                { "CA", 0.85 },  // California Buy Clean Act
                { "WA", 0.88 },  // Washington state standards
                { "OR", 0.90 },  // Oregon standards
                { "CO", 0.92 },  // Colorado standards
                { "NY", 0.90 },  // NYC Local Law 97
                { "MA", 0.92 },  // Massachusetts standards
                { "MN", 0.93 },  // Minnesota standards
                { "NJ", 0.93 },  // New Jersey standards
                
                // Standard states
                { "IL", 1.00 },
                { "PA", 1.00 },
                { "OH", 1.02 },
                { "MI", 1.00 },
                { "VA", 0.98 },
                { "NC", 1.00 },
                { "GA", 1.02 },
                { "FL", 1.02 },
                { "TX", 1.05 },
                { "AZ", 1.03 },
                
                // Higher carbon regions (more coal, fewer regulations)
                { "WV", 1.15 },
                { "KY", 1.12 },
                { "WY", 1.10 },
                { "ND", 1.08 },
                { "IN", 1.05 }
            };

            return multipliers.ContainsKey(state) ? multipliers[state] : 1.0;
        }

        /// <summary>
        /// Get concrete carbon factor by region (kgCO2e/m³)
        /// Based on CLF regional benchmarks
        /// </summary>
        public static double GetConcreteGwp(string state, string strength = "4000psi")
        {
            // Base values from CLF North America database
            double baseGwp = strength switch
            {
                "3000psi" => 280,
                "4000psi" => 340,
                "5000psi" => 400,
                "6000psi" => 460,
                "8000psi" => 550,
                _ => 340
            };

            // Regional adjustments
            var regionalBase = new Dictionary<string, double>
            {
                // West Coast - more SCMs available, stricter codes
                { "CA", 0.82 },
                { "WA", 0.85 },
                { "OR", 0.87 },
                
                // Northeast - good SCM availability
                { "NY", 0.90 },
                { "MA", 0.92 },
                { "CT", 0.93 },
                { "NJ", 0.92 },
                
                // Midwest - coal fly ash available
                { "IL", 0.95 },
                { "OH", 0.98 },
                { "MI", 0.97 },
                { "IN", 1.00 },
                
                // Southeast
                { "FL", 1.00 },
                { "GA", 1.02 },
                { "TX", 1.05 },
                { "NC", 0.98 },
                
                // Mountain
                { "CO", 0.95 },
                { "AZ", 1.02 },
                { "UT", 1.05 }
            };

            double multiplier = regionalBase.ContainsKey(state) ? regionalBase[state] : 1.0;
            return baseGwp * multiplier;
        }

        /// <summary>
        /// Get steel carbon factor by region (kgCO2e/ton)
        /// EAF vs BOF availability varies by region
        /// </summary>
        public static double GetSteelGwp(string state, string type = "structural")
        {
            // Base values
            double baseGwp = type switch
            {
                "rebar" => 690,       // EAF typical
                "structural" => 1240, // Mix of EAF/BOF
                "plate" => 1450,      // More BOF
                "decking" => 780,
                _ => 1240
            };

            // Regional adjustments (EAF mills location)
            var regionalBase = new Dictionary<string, double>
            {
                // Nucor/CMC territory - more EAF
                { "NC", 0.85 },  // Nucor HQ
                { "SC", 0.87 },
                { "TX", 0.90 },  // CMC mills
                { "IN", 0.88 },  // Steel mills
                { "AL", 0.90 },
                
                // Import-heavy regions
                { "CA", 1.05 },
                { "NY", 1.02 },
                { "FL", 1.05 },
                
                // Standard
                { "OH", 0.95 },
                { "PA", 0.98 },
                { "IL", 0.97 }
            };

            double multiplier = regionalBase.ContainsKey(state) ? regionalBase[state] : 1.0;
            return baseGwp * multiplier;
        }

        /// <summary>
        /// Get wood/timber carbon factor by region (kgCO2e/m³)
        /// Transportation and local sourcing matters
        /// </summary>
        public static double GetWoodGwp(string state, string type = "lumber")
        {
            // Base values (negative = carbon storage)
            double baseGwp = type switch
            {
                "lumber" => -450,   // Sequesters carbon
                "plywood" => -350,
                "clt" => -500,      // Cross-laminated timber
                "glulam" => -420,
                _ => -400
            };

            // Regions near forests = lower transport emissions
            var regionalBase = new Dictionary<string, double>
            {
                // Pacific Northwest - timber country
                { "WA", 0.90 },
                { "OR", 0.88 },
                { "ID", 0.92 },
                
                // Southeast timber
                { "GA", 0.93 },
                { "SC", 0.94 },
                { "NC", 0.95 },
                { "AL", 0.95 },
                
                // Mountain forests
                { "CO", 0.97 },
                { "MT", 0.93 },
                
                // Import regions
                { "CA", 1.08 },  // Far from forests
                { "NY", 1.10 },
                { "FL", 1.12 },
                { "TX", 1.05 }
            };

            // For wood, multiplier affects transport emissions portion
            double transportEmissions = 50; // kgCO2e/m³ baseline
            double multiplier = regionalBase.ContainsKey(state) ? regionalBase[state] : 1.0;
            
            return baseGwp + (transportEmissions * multiplier);
        }

        /// <summary>
        /// Get LEED regional priority credits available
        /// </summary>
        public static List<string> GetRegionalPriorityCredits(string state, string zipCode = "")
        {
            var credits = new List<string>();

            // Water efficiency (drought states)
            if (state == "CA" || state == "AZ" || state == "NV" || state == "CO" || state == "TX")
            {
                credits.Add("WE Credit: Water Efficiency - Regional Priority");
            }

            // Energy (high-carbon grid states)
            if (state == "WV" || state == "KY" || state == "IN" || state == "OH" || state == "WY")
            {
                credits.Add("EA Credit: Renewable Energy - Regional Priority");
            }

            // Transit (urban areas)
            if (state == "NY" || state == "CA" || state == "IL" || state == "MA" || state == "DC")
            {
                credits.Add("LT Credit: Access to Transit - Regional Priority");
            }

            // Habitat (biodiversity regions)
            if (state == "FL" || state == "WA" || state == "OR" || state == "HI")
            {
                credits.Add("SS Credit: Habitat Protection - Regional Priority");
            }

            // Heat island (hot climates)
            if (state == "AZ" || state == "NV" || state == "FL" || state == "TX" || state == "GA")
            {
                credits.Add("SS Credit: Heat Island Reduction - Regional Priority");
            }

            return credits;
        }

        /// <summary>
        /// Check if state has Buy Clean requirements
        /// </summary>
        public static BuyCleanRequirements GetBuyCleanRequirements(string state)
        {
            var requirements = new Dictionary<string, BuyCleanRequirements>
            {
                { "CA", new BuyCleanRequirements
                    {
                        HasRequirements = true,
                        Name = "California Buy Clean Act (AB 262)",
                        ConcreteGwpLimit = 380, // kgCO2e/m³ for 4000psi
                        SteelGwpLimit = 1100,   // kgCO2e/ton for structural
                        GlassGwpLimit = 0,
                        EffectiveDate = "2022",
                        Url = "https://www.dgs.ca.gov/PD/Resources/Page-Content/Procurement-Division-Resources-List-Folder/Buy-Clean-California-Act"
                    }
                },
                { "NY", new BuyCleanRequirements
                    {
                        HasRequirements = true,
                        Name = "NYC Local Law 97 + Buy Clean NY",
                        ConcreteGwpLimit = 400,
                        SteelGwpLimit = 1200,
                        GlassGwpLimit = 0,
                        EffectiveDate = "2024",
                        Url = "https://www.nyc.gov/site/buildings/codes/local-law-97.page"
                    }
                },
                { "CO", new BuyCleanRequirements
                    {
                        HasRequirements = true,
                        Name = "Colorado Buy Clean Colorado Act",
                        ConcreteGwpLimit = 400,
                        SteelGwpLimit = 1150,
                        GlassGwpLimit = 0,
                        EffectiveDate = "2024",
                        Url = "https://leg.colorado.gov/bills/hb21-1303"
                    }
                },
                { "WA", new BuyCleanRequirements
                    {
                        HasRequirements = true,
                        Name = "Washington Buy Clean Buy Fair Act",
                        ConcreteGwpLimit = 390,
                        SteelGwpLimit = 1100,
                        GlassGwpLimit = 0,
                        EffectiveDate = "2025",
                        Url = "https://ecology.wa.gov/Air-Climate/Climate-Commitment-Act/Buy-clean"
                    }
                }
            };

            return requirements.ContainsKey(state) 
                ? requirements[state] 
                : new BuyCleanRequirements { HasRequirements = false };
        }
    }

    public class BuyCleanRequirements
    {
        public bool HasRequirements { get; set; }
        public string Name { get; set; } = "";
        public double ConcreteGwpLimit { get; set; }
        public double SteelGwpLimit { get; set; }
        public double GlassGwpLimit { get; set; }
        public string EffectiveDate { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
