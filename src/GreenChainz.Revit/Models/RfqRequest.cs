using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GreenChainz.Revit.Models
{
    public class RFQRequest
    {
        [JsonProperty("projectName")]
        public string ProjectName { get; set; }

        [JsonProperty("projectAddress")]
        public string ProjectAddress { get; set; }

        [JsonProperty("materials")]
        public List<RFQItem> Materials { get; set; }

        [JsonProperty("deliveryDate")]
        public DateTime DeliveryDate { get; set; }

        [JsonProperty("specialInstructions")]
        public string SpecialInstructions { get; set; }

        [JsonProperty("selectedSupplierIds")]
        public List<string> SelectedSupplierIds { get; set; }

        public RFQRequest()
        {
            Materials = new List<RFQItem>();
            SelectedSupplierIds = new List<string>();
        }
    }

    public class RFQItem
    {
        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        public RFQItem() { }

        public RFQItem(string name, double quantity, string unit)
        {
            MaterialName = name;
            Quantity = quantity;
            Unit = unit;
        }
    }
}
