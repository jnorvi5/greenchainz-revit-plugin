using System;
using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    public class MaterialBreakdown
    {
        public string MaterialName { get; set; }
        public string Quantity { get; set; }
        public double CarbonFactor { get; set; }
        public double TotalCarbon { get; set; }
    }
}
