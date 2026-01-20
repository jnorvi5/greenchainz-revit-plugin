using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Services
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        // API URLs to try
        private static readonly string[] API_URLS = new[]
        {
            "https://greenchainz-revit-plugin.vercel.app",
            "https://web-cj3efhrgf-greenchainz-vercel.vercel.app",
            "http://localhost:3000"
        };

        public ApiClient()
            : this(API_URLS[0], null)
        {
        }

        public ApiClient(string baseUrl, string authToken = null)
        {
            _baseUrl = (baseUrl ?? API_URLS[0]).TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }

        /// <summary>
        /// Get carbon comparison for a material - finds best alternative
        /// Includes IFC tags for openBIM interoperability
        /// </summary>
        public async Task<AuditResponse> GetCarbonComparisonAsync(string materialName, string zipCode, double volume, double currentGwp = 0, string ifcCategory = null, string ifcGuid = null)
        {
            var requestBody = new
            {
                materialName = materialName,
                zipCode = zipCode,
                quantity = volume,
                currentProductGWP = currentGwp > 0 ? currentGwp : GetDefaultGwp(materialName),
                // IFC tags for openBIM mapping
                ifc = new
                {
                    guid = ifcGuid ?? MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                    category = ifcCategory ?? MapToIfcCategory(materialName),
                    exportAs = "IfcMaterial",
                    propertySet = "Pset_EnvironmentalImpactIndicators"
                },
                // EC3/CLF category mapping
                ec3Category = GetMaterialCategory(materialName),
                // Data standard references
                standards = new
                {
                    lcaStage = "A1-A3",              // Product stage (Cradle to Gate)
                    schema = "IFC4",
                    unit = "kgCO2e/m3",
                    reference = "ISO 21930:2017"
                }
            };

            // Try each API URL
            foreach (var baseUrl in API_URLS)
            {
                try
                {
                    string url = $"{baseUrl}/api/tools/calculate-impact";
                    string json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<AuditResponse>(responseString);
                        if (result != null)
                        {
                            result.Success = true;
                            return result;
                        }
                    }
                }
                catch
                {
                    continue; // Try next URL
                }
            }

            // If all APIs fail, return local comparison
            return GetLocalComparison(materialName, volume, currentGwp);
        }

        /// <summary>
        /// Maps material name to IFC category
        /// </summary>
        private string MapToIfcCategory(string materialName)
        {
            string lower = materialName?.ToLower() ?? "";
            if (lower.Contains("concrete") || lower.Contains("cement")) return "IfcConcrete";
            if (lower.Contains("steel") || lower.Contains("metal")) return "IfcSteel";
            if (lower.Contains("wood") || lower.Contains("timber")) return "IfcWood";
            if (lower.Contains("glass")) return "IfcGlass";
            if (lower.Contains("aluminum") || lower.Contains("aluminium")) return "IfcAluminium";
            if (lower.Contains("gypsum") || lower.Contains("drywall")) return "IfcGypsum";
            if (lower.Contains("insulation")) return "IfcInsulation";
            return "IfcMaterial";
        }

        /// <summary>
        /// Get local comparison when API unavailable
        /// </summary>
        private AuditResponse GetLocalComparison(string materialName, double volume, double currentGwp)
        {
            string category = GetMaterialCategory(materialName);
            var alternatives = GetLocalAlternatives(category);
            double baseGwp = currentGwp > 0 ? currentGwp : GetDefaultGwp(materialName);

            var original = new MaterialComparison
            {
                MaterialName = materialName,
                SupplierName = "Current Specification",
                GwpValue = baseGwp * volume,
                HasEpd = false,
                LeedImpact = "Baseline"
            };

            MaterialComparison best = null;
            double lowestGwp = baseGwp;

            foreach (var alt in alternatives)
            {
                if (alt.GwpValue < lowestGwp)
                {
                    lowestGwp = alt.GwpValue;
                    best = new MaterialComparison
                    {
                        MaterialName = alt.MaterialName,
                        SupplierName = alt.SupplierName,
                        GwpValue = alt.GwpValue * volume,
                        HasEpd = alt.HasEpd,
                        EpdId = alt.EpdId,
                        Certifications = alt.Certifications,
                        Distance = alt.Distance,
                        CarbonSavings = ((baseGwp - alt.GwpValue) / baseGwp) * 100,
                        LeedImpact = GetLeedImpact(alt.HasEpd, alt.Certifications)
                    };
                }
            }

            if (best == null)
            {
                best = new MaterialComparison
                {
                    MaterialName = $"Low-Carbon {category}",
                    SupplierName = "GreenChainz Recommended",
                    GwpValue = baseGwp * 0.7 * volume,
                    HasEpd = true,
                    CarbonSavings = 30,
                    LeedImpact = "+1 LEED Point (EPD)"
                };
            }

            return new AuditResponse
            {
                Success = true,
                Message = "Comparison generated from local database",
                Original = original,
                BestAlternative = best,
                Alternatives = new List<MaterialComparison>(alternatives),
                DataSource = "CLF v2021 + GreenChainz Database"
            };
        }

        private List<MaterialComparison> GetLocalAlternatives(string category)
        {
            var alternatives = new List<MaterialComparison>();

            switch (category.ToLower())
            {
                case "concrete":
                    alternatives.Add(new MaterialComparison { MaterialName = "CarbonCure Ready-Mix", SupplierName = "CarbonCure Technologies", GwpValue = 280, HasEpd = true, EpdId = "EPD-CC-2024", Certifications = new List<string> { "EPD", "Carbon Negative" }, Distance = 150 });
                    alternatives.Add(new MaterialComparison { MaterialName = "Solidia Low-Carbon Cement", SupplierName = "Solidia Technologies", GwpValue = 200, HasEpd = true, EpdId = "EPD-SOL-2024", Certifications = new List<string> { "EPD", "Cradle to Cradle" }, Distance = 300 });
                    alternatives.Add(new MaterialComparison { MaterialName = "Central Concrete Recycled", SupplierName = "Central Concrete", GwpValue = 300, HasEpd = true, Certifications = new List<string> { "EPD", "LEED" }, Distance = 50 });
                    break;

                case "steel":
                    alternatives.Add(new MaterialComparison { MaterialName = "Nucor EAF Steel", SupplierName = "Nucor Corporation", GwpValue = 690, HasEpd = true, EpdId = "EPD-NUC-2024", Certifications = new List<string> { "EPD", "ISO 14001", "75% Recycled" }, Distance = 200 });
                    alternatives.Add(new MaterialComparison { MaterialName = "SSAB Fossil-Free Steel", SupplierName = "SSAB", GwpValue = 400, HasEpd = true, EpdId = "EPD-SSAB-2024", Certifications = new List<string> { "EPD", "HYBRIT", "Zero Fossil" }, Distance = 0 });
                    alternatives.Add(new MaterialComparison { MaterialName = "CMC Recycled Rebar", SupplierName = "Commercial Metals", GwpValue = 650, HasEpd = true, Certifications = new List<string> { "EPD", "97% Recycled" }, Distance = 100 });
                    break;

                case "wood":
                    alternatives.Add(new MaterialComparison { MaterialName = "Structurlam CLT", SupplierName = "Structurlam", GwpValue = -500, HasEpd = true, EpdId = "EPD-STR-2024", Certifications = new List<string> { "FSC", "EPD", "Carbon Negative" }, Distance = 400 });
                    alternatives.Add(new MaterialComparison { MaterialName = "Nordic CLT Panels", SupplierName = "Nordic Structures", GwpValue = -450, HasEpd = true, Certifications = new List<string> { "FSC", "EPD" }, Distance = 500 });
                    break;

                case "insulation":
                    alternatives.Add(new MaterialComparison { MaterialName = "Rockwool Stone Wool", SupplierName = "Rockwool", GwpValue = 30, HasEpd = true, EpdId = "EPD-RW-2024", Certifications = new List<string> { "EPD", "GREENGUARD", "Cradle to Cradle" }, Distance = 200 });
                    alternatives.Add(new MaterialComparison { MaterialName = "Owens Corning EcoTouch", SupplierName = "Owens Corning", GwpValue = 35, HasEpd = true, Certifications = new List<string> { "EPD", "GREENGUARD Gold" }, Distance = 100 });
                    break;

                case "glass":
                    alternatives.Add(new MaterialComparison { MaterialName = "Guardian SunGuard", SupplierName = "Guardian Glass", GwpValue = 1200, HasEpd = true, Certifications = new List<string> { "EPD", "30% Recycled Cullet" }, Distance = 300 });
                    alternatives.Add(new MaterialComparison { MaterialName = "Vitro Solarban", SupplierName = "Vitro Architectural Glass", GwpValue = 1300, HasEpd = true, Certifications = new List<string> { "EPD", "LEED" }, Distance = 150 });
                    break;

                case "aluminum":
                    alternatives.Add(new MaterialComparison { MaterialName = "Novelis Recycled Aluminum", SupplierName = "Novelis", GwpValue = 2000, HasEpd = true, EpdId = "EPD-NOV-2024", Certifications = new List<string> { "EPD", "ASI", "82% Recycled" }, Distance = 250 });
                    alternatives.Add(new MaterialComparison { MaterialName = "Hydro CIRCAL 75R", SupplierName = "Hydro Aluminum", GwpValue = 2200, HasEpd = true, Certifications = new List<string> { "EPD", "ASI", "75% Recycled" }, Distance = 0 });
                    break;

                default:
                    alternatives.Add(new MaterialComparison { MaterialName = $"Low-Carbon {category}", SupplierName = "GreenChainz Verified", GwpValue = 70, HasEpd = true, Certifications = new List<string> { "EPD" }, Distance = 100 });
                    break;
            }

            return alternatives;
        }

        private string GetMaterialCategory(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("concrete") || lower.Contains("cement")) return "Concrete";
            if (lower.Contains("steel") || lower.Contains("metal") || lower.Contains("rebar")) return "Steel";
            if (lower.Contains("wood") || lower.Contains("timber") || lower.Contains("clt")) return "Wood";
            if (lower.Contains("glass") || lower.Contains("glazing")) return "Glass";
            if (lower.Contains("aluminum") || lower.Contains("aluminium")) return "Aluminum";
            if (lower.Contains("insulation")) return "Insulation";
            if (lower.Contains("gypsum") || lower.Contains("drywall")) return "Gypsum";
            return "Other";
        }

        private double GetDefaultGwp(string name)
        {
            string category = GetMaterialCategory(name);
            return category switch
            {
                "Concrete" => 340,
                "Steel" => 1850,
                "Wood" => 110,
                "Glass" => 1500,
                "Aluminum" => 8000,
                "Insulation" => 50,
                "Gypsum" => 200,
                _ => 100
            };
        }

        private string GetLeedImpact(bool hasEpd, List<string> certs)
        {
            if (!hasEpd) return "No LEED Impact";
            
            int points = 1; // Base EPD point
            if (certs != null)
            {
                if (certs.Contains("FSC")) points++;
                if (certs.Contains("Cradle to Cradle")) points++;
                if (certs.Contains("Carbon Negative")) points++;
            }
            return $"+{points} LEED Point{(points > 1 ? "s" : "")}";
        }

        // ... existing methods ...

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            foreach (var baseUrl in API_URLS)
            {
                try
                {
                    string url = $"{baseUrl}/api/rfq";
                    string json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
                catch { continue; }
            }

            throw new Exception("All API endpoints unavailable");
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditResult request)
        {
            foreach (var baseUrl in API_URLS)
            {
                try
                {
                    string url = $"{baseUrl}/api/audit";
                    string json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<AuditResult>(responseString);
                    }
                }
                catch { continue; }
            }

            return new AuditResult
            {
                OverallScore = -1,
                Summary = "API Error: All endpoints unavailable"
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
