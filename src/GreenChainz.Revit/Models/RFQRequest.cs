using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    public class RFQRequest
    {
        public string ProjectName { get; set; }
        public string ProjectAddress { get; set; }
        public List<RFQItem> Materials { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string SpecialInstructions { get; set; }

        public RFQRequest()
        {
            Materials = new List<RFQItem>();
        }
    }

    public class RFQItem
    {
        public string MaterialName { get; set; }
        public double Quantity { get; set; }
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
