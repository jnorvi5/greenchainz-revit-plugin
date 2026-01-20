using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Service for exporting carbon data to IFC-compliant formats.
    /// Supports Pset_EnvironmentalImpactIndicators and other standard property sets.
    /// </summary>
    public class IfcExportService
    {
        /// <summary>
        /// IFC Property Set for Environmental Impact Indicators (ISO 21930)
        /// </summary>
        public class Pset_EnvironmentalImpactIndicators
        {
            // Life Cycle Assessment Indicators
            public double GlobalWarmingPotential { get; set; }      // GWP in kgCO2e
            public double OzoneDepletionPotential { get; set; }     // ODP in kg CFC-11e
            public double AcidificationPotential { get; set; }      // AP in kg SO2e
            public double EutrophicationPotential { get; set; }     // EP in kg PO4e
            public double PhotochemicalOzoneCreation { get; set; }  // POCP in kg C2H4e
            
            // Resource Use
            public double TotalPrimaryEnergyConsumption { get; set; }     // MJ
            public double NonRenewablePrimaryEnergy { get; set; }         // MJ
            public double RenewablePrimaryEnergy { get; set; }            // MJ
            
            // Metadata
            public string LifeCycleStage { get; set; }              // A1-A3, A4, A5, B1-B7, C1-C4, D
            public string FunctionalUnit { get; set; }              // e.g., "1 m3", "1 kg"
            public string DataSource { get; set; }                  // EPD reference or database
            public DateTime ValidUntil { get; set; }
        }

        /// <summary>
        /// IFC Property Set for Material Common properties
        /// </summary>
        public class Pset_MaterialCommon
        {
            public string Porosity { get; set; }
            public double MassDensity { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
        }

        /// <summary>
        /// Converts audit result to IFC property set mapping for export
        /// </summary>
        public Dictionary<string, object> ExportToIfcMapping(AuditResult audit)
        {
            var ifcMapping = new Dictionary<string, object>
            {
                ["IfcProject"] = new
                {
                    GlobalId = audit.IfcProjectGuid,
                    Name = audit.ProjectName,
                    Description = "Carbon Audit Export from GreenChainz",
                    CreationDate = audit.Date.ToString("o"),
                    Schema = audit.IfcSchema
                },
                ["PropertySets"] = new List<object>()
            };

            var propertySets = (List<object>)ifcMapping["PropertySets"];

            // Add project-level environmental indicators
            propertySets.Add(new
            {
                Name = "Pset_EnvironmentalImpactIndicators",
                AppliesTo = "IfcProject",
                Properties = new
                {
                    GlobalWarmingPotential = audit.OverallScore,
                    GlobalWarmingPotentialUnit = "kgCO2e",
                    LifeCycleStage = "A1-A3",
                    DataSource = audit.DataSource,
                    Reference = "GreenChainz Carbon Audit",
                    ValidUntil = DateTime.Now.AddYears(5).ToString("yyyy-MM-dd")
                }
            });

            // Add material-level mappings
            foreach (var material in audit.Materials)
            {
                var materialMapping = CreateMaterialIfcMapping(material);
                propertySets.Add(materialMapping);
            }

            return ifcMapping;
        }

        /// <summary>
        /// Creates IFC mapping for a single material with Pset_EnvironmentalImpactIndicators
        /// </summary>
        public object CreateMaterialIfcMapping(MaterialBreakdown material)
        {
            return new
            {
                IfcEntity = new
                {
                    Type = material.IfcExportAs ?? "IfcMaterial",
                    GlobalId = material.IfcGuid,
                    Name = material.MaterialName,
                    Category = material.IfcCategory ?? MapToIfcCategory(material.Ec3Category),
                    RevitElementId = material.RevitElementId
                },
                Pset_EnvironmentalImpactIndicators = new
                {
                    GlobalWarmingPotential = material.TotalCarbon,
                    GlobalWarmingPotentialUnit = "kgCO2e",
                    GlobalWarmingPotentialPerUnit = material.CarbonFactor,
                    GlobalWarmingPotentialPerUnitUnit = "kgCO2e/m3",
                    LifeCycleStage = "A1-A3",           // Product stage (Cradle to Gate)
                    FunctionalUnit = "1 m3",
                    DataSource = material.DataSource ?? "CLF v2021",
                    Ec3Category = material.Ec3Category
                },
                Pset_MaterialCommon = new
                {
                    Name = material.MaterialName,
                    Category = material.Ec3Category,
                    MassDensity = EstimateDensity(material.Ec3Category)
                },
                Qto_MaterialBaseQuantities = new
                {
                    NetVolume = material.VolumeM3,
                    NetMass = material.MassKg,
                    VolumeUnit = "m3",
                    MassUnit = "kg"
                }
            };
        }

        /// <summary>
        /// Exports audit to IFC-compatible JSON format
        /// </summary>
        public string ExportToIfcJson(AuditResult audit)
        {
            var mapping = ExportToIfcMapping(audit);
            return Newtonsoft.Json.JsonConvert.SerializeObject(mapping, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Exports to a simplified IFC-SPF text format for demonstration
        /// </summary>
        public string ExportToIfcSpfFormat(AuditResult audit)
        {
            var sb = new StringBuilder();
            
            // IFC Header
            sb.AppendLine("ISO-10303-21;");
            sb.AppendLine("HEADER;");
            sb.AppendLine($"FILE_DESCRIPTION(('GreenChainz Carbon Audit'),'2;1');");
            sb.AppendLine($"FILE_NAME('{audit.ProjectName}_CarbonAudit.ifc','{DateTime.Now:yyyy-MM-ddTHH:mm:ss}',('GreenChainz'),(''),'.NET','GreenChainz Revit Plugin','');");
            sb.AppendLine("FILE_SCHEMA(('IFC4'));");
            sb.AppendLine("ENDSEC;");
            sb.AppendLine();
            sb.AppendLine("DATA;");
            
            int entityId = 1;
            
            // Project
            sb.AppendLine($"#{entityId++}=IFCPROJECT('{audit.IfcProjectGuid}',$,'{audit.ProjectName}',$,$,$,$,$,$);");
            
            // Property Set for Project GWP
            int psetId = entityId++;
            sb.AppendLine($"#{psetId}=IFCPROPERTYSET('{MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid())}',$,'Pset_EnvironmentalImpactIndicators',$,(#{entityId}));");
            sb.AppendLine($"#{entityId++}=IFCPROPERTYSINGLEVALUE('GlobalWarmingPotential',$,IFCREAL({audit.OverallScore}),$);");
            
            // Materials with their carbon data
            foreach (var mat in audit.Materials)
            {
                int matId = entityId++;
                sb.AppendLine($"#{matId}=IFCMATERIAL('{mat.MaterialName}',$,'{mat.IfcCategory ?? "NOTDEFINED"}');");
                
                // Environmental impact property set for material
                int matPsetId = entityId++;
                int gwpPropId = entityId++;
                int lcStagePropId = entityId++;
                int sourcePropId = entityId++;
                
                sb.AppendLine($"#{gwpPropId}=IFCPROPERTYSINGLEVALUE('GlobalWarmingPotential',$,IFCREAL({mat.TotalCarbon}),$);");
                sb.AppendLine($"#{lcStagePropId}=IFCPROPERTYSINGLEVALUE('LifeCycleStage',$,IFCTEXT('A1-A3'),$);");
                sb.AppendLine($"#{sourcePropId}=IFCPROPERTYSINGLEVALUE('DataSource',$,IFCTEXT('{mat.DataSource ?? "CLF v2021"}'),$);");
                sb.AppendLine($"#{matPsetId}=IFCPROPERTYSET('{mat.IfcGuid}',$,'Pset_EnvironmentalImpactIndicators',$,(#{gwpPropId},#{lcStagePropId},#{sourcePropId}));");
            }
            
            sb.AppendLine("ENDSEC;");
            sb.AppendLine("END-ISO-10303-21;");
            
            return sb.ToString();
        }

        /// <summary>
        /// Maps EC3/Revit categories to IFC entity types
        /// </summary>
        public string MapToIfcCategory(string ec3Category)
        {
            if (string.IsNullOrEmpty(ec3Category)) return "IfcBuildingElementProxy";

            return ec3Category.ToLower() switch
            {
                "concrete" => "IfcConcrete",
                "steel" => "IfcSteel",
                "wood" => "IfcWood",
                "timber" => "IfcWood",
                "glass" => "IfcGlass",
                "aluminum" => "IfcAluminium",
                "aluminium" => "IfcAluminium",
                "gypsum" => "IfcGypsum",
                "gypsum board" => "IfcGypsum",
                "insulation" => "IfcInsulation",
                "brick" => "IfcCite",
                "brick/masonry" => "IfcCite",
                "stone" => "IfcStite",
                "roofing" => "IfcRoofing",
                "cladding" => "IfcCladding",
                _ => "IfcMaterial"
            };
        }

        /// <summary>
        /// Maps Revit categories to IFC element types
        /// </summary>
        public string MapRevitCategoryToIfc(string revitCategory)
        {
            return revitCategory?.ToLower() switch
            {
                "walls" => "IfcWall",
                "floors" => "IfcSlab",
                "roofs" => "IfcRoof",
                "ceilings" => "IfcCovering",
                "structural columns" => "IfcColumn",
                "structural framing" => "IfcBeam",
                "structural foundation" => "IfcFooting",
                "windows" => "IfcWindow",
                "doors" => "IfcDoor",
                "curtain panels" => "IfcPlate",
                "stairs" => "IfcStair",
                "railings" => "IfcRailing",
                "ramps" => "IfcRamp",
                _ => "IfcBuildingElementProxy"
            };
        }

        /// <summary>
        /// Estimates material density for mass calculations
        /// </summary>
        private double EstimateDensity(string category)
        {
            return category?.ToLower() switch
            {
                "concrete" => 2400,      // kg/m3
                "steel" => 7850,
                "aluminum" => 2700,
                "aluminium" => 2700,
                "wood" => 500,
                "timber" => 500,
                "glass" => 2500,
                "gypsum" => 800,
                "gypsum board" => 800,
                "insulation" => 30,
                "brick" => 1800,
                "brick/masonry" => 1800,
                "stone" => 2500,
                _ => 1000
            };
        }

        /// <summary>
        /// Saves IFC mapping to file
        /// </summary>
        public void SaveIfcMapping(AuditResult audit, string outputPath)
        {
            string json = ExportToIfcJson(audit);
            File.WriteAllText(outputPath, json);
        }

        /// <summary>
        /// Saves IFC-SPF format to file
        /// </summary>
        public void SaveIfcSpf(AuditResult audit, string outputPath)
        {
            string ifc = ExportToIfcSpfFormat(audit);
            File.WriteAllText(outputPath, ifc);
        }
    }
}
