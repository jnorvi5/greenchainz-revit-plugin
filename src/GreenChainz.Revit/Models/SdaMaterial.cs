using Newtonsoft.Json;

namespace GreenChainz.Revit.Models
{
    public class SdaMaterial
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("embodied_carbon_kgco2e")]
        public double EmbodiedCarbon { get; set; }

        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonProperty("epd_url")]
        public string EpdUrl { get; set; }

        [JsonProperty("certifications")]
        public string Certifications { get; set; }
    }

    public class SdaMaterialDetail : SdaMaterial
    {
        [JsonProperty("technical_specs")]
        public string TechnicalSpecs { get; set; }

        [JsonProperty("lca_data")]
        public string LcaData { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("density")]
        public double? Density { get; set; }

        [JsonProperty("thermal_conductivity")]
        public double? ThermalConductivity { get; set; }
    }

    public class SdaMaterialsResponse
    {
        [JsonProperty("materials")]
        public SdaMaterial[] Materials { get; set; }

        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("page_size")]
        public int PageSize { get; set; }
    }
}
