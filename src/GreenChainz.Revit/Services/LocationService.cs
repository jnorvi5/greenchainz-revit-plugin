using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Service for extracting and using project location data
    /// </summary>
    public class LocationService
    {
        /// <summary>
        /// Get project location from Revit document
        /// </summary>
        public ProjectLocation GetProjectLocation(Document doc)
        {
            var location = new ProjectLocation();

            try
            {
                // Get project info
                var projectInfo = doc.ProjectInformation;
                if (projectInfo != null)
                {
                    location.ProjectName = projectInfo.Name ?? doc.Title;
                    location.Address = projectInfo.Address ?? "";
                    location.ClientName = projectInfo.ClientName ?? "";
                }

                // Get site location from Revit
                var siteLocation = doc.SiteLocation;
                if (siteLocation != null)
                {
                    // Convert radians to degrees
                    location.Latitude = siteLocation.Latitude * (180.0 / Math.PI);
                    location.Longitude = siteLocation.Longitude * (180.0 / Math.PI);
                    location.TimeZone = siteLocation.TimeZone;
                }

                // Determine region from coordinates or address
                location.Region = DetermineRegion(location.Latitude, location.Longitude, location.Address);
                location.State = DetermineState(location.Latitude, location.Longitude, location.Address);
                location.Country = DetermineCountry(location.Latitude, location.Longitude);
                location.ClimateZone = DetermineClimateZone(location.Latitude);

                // Get grid carbon intensity for the region
                location.GridCarbonIntensity = GetGridCarbonIntensity(location.State);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting location: {ex.Message}");
            }

            return location;
        }

        /// <summary>
        /// Calculate distance between two coordinates (Haversine formula)
        /// </summary>
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 3958.8; // Earth's radius in miles

            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        private string DetermineRegion(double lat, double lon, string address)
        {
            // Check address first
            string addr = address?.ToLower() ?? "";
            
            // West Coast
            if (addr.Contains("california") || addr.Contains("oregon") || addr.Contains("washington") ||
                addr.Contains("ca") || addr.Contains("or") || addr.Contains("wa") ||
                (lon < -115 && lon > -125 && lat > 32 && lat < 49))
                return "West Coast";

            // Northeast
            if (addr.Contains("new york") || addr.Contains("massachusetts") || addr.Contains("connecticut") ||
                addr.Contains("new jersey") || addr.Contains("pennsylvania") ||
                (lon > -80 && lon < -70 && lat > 38 && lat < 45))
                return "Northeast";

            // Southeast
            if (addr.Contains("florida") || addr.Contains("georgia") || addr.Contains("texas") ||
                addr.Contains("north carolina") || addr.Contains("south carolina") ||
                (lon > -100 && lon < -75 && lat > 25 && lat < 37))
                return "Southeast";

            // Midwest
            if (addr.Contains("illinois") || addr.Contains("ohio") || addr.Contains("michigan") ||
                addr.Contains("indiana") || addr.Contains("wisconsin") ||
                (lon > -100 && lon < -80 && lat > 37 && lat < 49))
                return "Midwest";

            // Mountain
            if (addr.Contains("colorado") || addr.Contains("utah") || addr.Contains("arizona") ||
                addr.Contains("nevada") || addr.Contains("new mexico") ||
                (lon > -115 && lon < -100 && lat > 31 && lat < 49))
                return "Mountain";

            return "Unknown";
        }

        private string DetermineState(double lat, double lon, string address)
        {
            string addr = address?.ToLower() ?? "";

            // Common state mappings
            var statePatterns = new Dictionary<string, string>
            {
                { "california", "CA" }, { " ca ", "CA" }, { ", ca", "CA" },
                { "new york", "NY" }, { " ny ", "NY" }, { ", ny", "NY" },
                { "texas", "TX" }, { " tx ", "TX" }, { ", tx", "TX" },
                { "florida", "FL" }, { " fl ", "FL" }, { ", fl", "FL" },
                { "washington", "WA" }, { " wa ", "WA" },
                { "oregon", "OR" }, { " or ", "OR" },
                { "colorado", "CO" }, { " co ", "CO" },
                { "illinois", "IL" }, { " il ", "IL" },
                { "massachusetts", "MA" }, { " ma ", "MA" },
                { "arizona", "AZ" }, { " az ", "AZ" },
                { "georgia", "GA" }, { " ga ", "GA" },
                { "north carolina", "NC" }, { " nc ", "NC" },
                { "virginia", "VA" }, { " va ", "VA" },
                { "new jersey", "NJ" }, { " nj ", "NJ" },
                { "pennsylvania", "PA" }, { " pa ", "PA" },
                { "ohio", "OH" }, { " oh ", "OH" },
                { "michigan", "MI" }, { " mi ", "MI" }
            };

            foreach (var kvp in statePatterns)
            {
                if (addr.Contains(kvp.Key))
                    return kvp.Value;
            }

            // Fallback to coordinate-based estimation
            if (lat > 32 && lat < 42 && lon > -124 && lon < -114) return "CA";
            if (lat > 40 && lat < 45 && lon > -80 && lon < -72) return "NY";
            if (lat > 25 && lat < 31 && lon > -100 && lon < -80) return "TX";

            return "Unknown";
        }

        private string DetermineCountry(double lat, double lon)
        {
            // Simple US/Canada/Mexico check
            if (lat > 24 && lat < 50 && lon > -125 && lon < -66)
                return "USA";
            if (lat > 41 && lat < 84 && lon > -141 && lon < -52)
                return "Canada";
            if (lat > 14 && lat < 33 && lon > -118 && lon < -86)
                return "Mexico";
            return "Unknown";
        }

        private string DetermineClimateZone(double lat)
        {
            // ASHRAE Climate Zones (simplified)
            if (lat < 25) return "1 - Very Hot";
            if (lat < 30) return "2 - Hot";
            if (lat < 35) return "3 - Warm";
            if (lat < 40) return "4 - Mixed";
            if (lat < 45) return "5 - Cool";
            if (lat < 50) return "6 - Cold";
            return "7 - Very Cold";
        }

        /// <summary>
        /// Get grid carbon intensity by state (lbs CO2/MWh)
        /// Source: EPA eGRID 2021
        /// </summary>
        private double GetGridCarbonIntensity(string state)
        {
            var gridIntensity = new Dictionary<string, double>
            {
                // Clean grids (hydro, nuclear, renewables)
                { "WA", 287 },  // Washington - Hydro
                { "OR", 350 },  // Oregon - Hydro
                { "VT", 195 },  // Vermont - Nuclear + Hydro
                { "ID", 380 },  // Idaho - Hydro
                { "CA", 530 },  // California - Renewables
                
                // Mixed grids
                { "NY", 520 },
                { "CT", 490 },
                { "MA", 620 },
                { "NJ", 580 },
                { "PA", 690 },
                { "IL", 650 },
                { "OH", 950 },
                { "MI", 870 },
                { "MN", 720 },
                { "CO", 870 },
                { "AZ", 720 },
                { "NV", 780 },
                { "TX", 820 },
                { "FL", 820 },
                { "GA", 830 },
                { "NC", 680 },
                { "VA", 640 },
                
                // Coal-heavy grids
                { "WV", 1650 },
                { "KY", 1520 },
                { "WY", 1580 },
                { "ND", 1420 },
                { "MO", 1380 },
                { "IN", 1350 },
                { "UT", 1180 }
            };

            return gridIntensity.ContainsKey(state) ? gridIntensity[state] : 800; // US average
        }
    }

    /// <summary>
    /// Project location data
    /// </summary>
    public class ProjectLocation
    {
        public string ProjectName { get; set; } = "";
        public string Address { get; set; } = "";
        public string ClientName { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double TimeZone { get; set; }
        public string Region { get; set; } = "Unknown";
        public string State { get; set; } = "Unknown";
        public string Country { get; set; } = "USA";
        public string ClimateZone { get; set; } = "";
        public double GridCarbonIntensity { get; set; } = 800; // lbs CO2/MWh
        
        public bool HasValidCoordinates => Latitude != 0 || Longitude != 0;
        public string CoordinatesDisplay => HasValidCoordinates 
            ? $"{Latitude:F4}°, {Longitude:F4}°" 
            : "Not Set";
        public string LocationDisplay => !string.IsNullOrEmpty(State) && State != "Unknown"
            ? $"{State}, {Country}"
            : Region;
        public string GridIntensityDisplay => $"{GridCarbonIntensity:N0} lbs CO2/MWh";
    }
}
