using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;
using System.Collections.Generic;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to generate a sample IFC file with carbon data.
    /// Great for demonstrating IFC/openBIM interoperability.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class GenerateSampleIfcCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Ask user for output location
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "IFC Files (*.ifc)|*.ifc|IFC JSON (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "ifc",
                    FileName = $"GreenChainz_Sample_{DateTime.Now:yyyyMMdd}",
                    Title = "Save Sample IFC File"
                };

                if (saveDialog.ShowDialog() != true)
                    return Result.Cancelled;

                // Generate sample data
                var sampleAudit = CreateSampleAuditResult();

                // Export
                var ifcService = new IfcExportService();
                
                if (saveDialog.FileName.EndsWith(".json"))
                {
                    ifcService.SaveIfcMapping(sampleAudit, saveDialog.FileName);
                }
                else
                {
                    ifcService.SaveIfcSpf(sampleAudit, saveDialog.FileName);
                }

                // Also generate BCF issues
                string bcfPath = saveDialog.FileName.Replace(".ifc", ".bcf").Replace(".json", ".bcf");
                var bcfService = new BcfExportService();
                bcfService.ExportHighCarbonIssues(sampleAudit, bcfPath);

                TaskDialog.Show("Sample Files Generated", 
                    $"IFC and BCF sample files created!\n\n" +
                    $"IFC File:\n{saveDialog.FileName}\n\n" +
                    $"BCF File:\n{bcfPath}\n\n" +
                    $"Contents:\n" +
                    $"- 8 materials with GWP data\n" +
                    $"- Pset_EnvironmentalImpactIndicators\n" +
                    $"- IFC GUIDs for cross-software tracking\n" +
                    $"- 3 high-carbon BCF issues\n\n" +
                    $"Open these files in:\n" +
                    $"- BlenderBIM (free)\n" +
                    $"- BIMcollab ZOOM (free)\n" +
                    $"- Solibri\n" +
                    $"- Navisworks");

                // Open folder
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Path.GetDirectoryName(saveDialog.FileName),
                    UseShellExecute = true
                });

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private AuditResult CreateSampleAuditResult()
        {
            return new AuditResult
            {
                ProjectName = "GreenChainz Demo Building",
                Date = DateTime.Now,
                OverallScore = 185000,
                Summary = "Sample carbon audit for IFC demonstration",
                DataSource = "GreenChainz + CLF v2021",
                IfcProjectGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                IfcSchema = "IFC4",
                Materials = new List<MaterialBreakdown>
                {
                    // HIGH CARBON - Will be flagged in BCF
                    new MaterialBreakdown
                    {
                        MaterialName = "Cast-in-Place Concrete 4000psi",
                        Quantity = "250 m3",
                        CarbonFactor = 340,
                        TotalCarbon = 85000,
                        DataSource = "CLF v2021 Baseline",
                        Ec3Category = "Concrete",
                        IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                        IfcCategory = "IfcConcrete",
                        IfcExportAs = "IfcMaterial",
                        VolumeM3 = 250,
                        MassKg = 600000
                    },
                    new MaterialBreakdown
                    {
                        MaterialName = "Structural Steel W-Shapes",
                        Quantity = "45 tons",
                        CarbonFactor = 1850,
                        TotalCarbon = 83250,
                        DataSource = "Industry Average",
                        Ec3Category = "Steel",
                        IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                        IfcCategory = "IfcSteel",
                        IfcExportAs = "IfcMaterial",
                        VolumeM3 = 5.7,
                        MassKg = 45000
                    },
                    new MaterialBreakdown
                    {
                        MaterialName = "Aluminum Curtain Wall Framing",
                        Quantity = "2.5 tons",
                        CarbonFactor = 8000,
                        TotalCarbon = 20000,
                        DataSource = "Industry Average",
                        Ec3Category = "Aluminum",
                        IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                        IfcCategory = "IfcAluminium",
                        IfcExportAs = "IfcMaterial",
                        VolumeM3 = 0.93,
                        MassKg = 2500
                    },
                    
                    // LOW CARBON - Good examples
                    new MaterialBreakdown
                    {
                        MaterialName = "CarbonCure Ready-Mix (GreenChainz Verified)",
                        Quantity = "100 m3",
                        CarbonFactor = 238,
                        TotalCarbon = 23800,
                        DataSource = "EC3 - EPD Verified",
                        Ec3Category = "Concrete",
                        IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                        IfcCategory = "IfcConcrete",
                        IfcExportAs = "IfcMaterial",
                        VolumeM3 = 100,
                        MassKg = 240000
                    },
                    new MaterialBreakdown
                    {
                        MaterialName = "Nucor EAF Recycled Steel",
                        Quantity = "20 tons",
                        CarbonFactor = 690,
                        TotalCarbon = 13800,
                        DataSource = "EC3 - EPD Verified",
                        Ec3Category = "Steel",
                        IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                        IfcCategory = "IfcSteel",
                        IfcExportAs = "IfcMaterial",
                        VolumeM3 = 2.5,
                        MassKg = 20000
                    },
                    new MaterialBreakdown
                    {
                        MaterialName = "Structurlam CLT Panels",
                        Quantity = "80 m3",
                        CarbonFactor = -500,
                        TotalCarbon = -40000,
                        DataSource = "EC3 - EPD Verified",
                        Ec3Category = "Wood",
                        IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                        IfcCategory = "IfcWood",
                        IfcExportAs = "IfcMaterial",
                        VolumeM3 = 80,
                        MassKg = 40000
                    },
                    new MaterialBreakdown
                    {
                        MaterialName = "Rockwool Stone Wool Insulation",
                        Quantity = "150 m3",
                        CarbonFactor = 28,
                        TotalCarbon = 4200,
                        DataSource = "EC3 - EPD Verified",
                        Ec3Category = "Insulation",
                        IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                        IfcCategory = "IfcInsulation",
                        IfcExportAs = "IfcMaterial",
                        VolumeM3 = 150,
                        MassKg = 4500
                    },
                    new MaterialBreakdown
                    {
                        MaterialName = "Guardian SunGuard Glass",
                        Quantity = "500 m2",
                        CarbonFactor = 1150,
                        TotalCarbon = 5750,
                        DataSource = "EC3 - EPD Verified",
                        Ec3Category = "Glass",
                        IfcGuid = MaterialBreakdown.ConvertToIfcGuid(Guid.NewGuid()),
                        IfcCategory = "IfcGlass",
                        IfcExportAs = "IfcMaterial",
                        VolumeM3 = 50,
                        MassKg = 125000
                    }
                },
                Recommendations = new List<Recommendation>
                {
                    new Recommendation
                    {
                        Description = "Replace standard concrete with CarbonCure to save 30% carbon",
                        PotentialSavings = 25500,
                        Ec3Link = "https://buildingtransparency.org/ec3/material-search?category=Concrete"
                    },
                    new Recommendation
                    {
                        Description = "Specify Nucor EAF steel instead of conventional steel",
                        PotentialSavings = 52200,
                        Ec3Link = "https://buildingtransparency.org/ec3/material-search?category=Steel"
                    },
                    new Recommendation
                    {
                        Description = "Consider Novelis recycled aluminum for curtain wall",
                        PotentialSavings = 15000,
                        Ec3Link = "https://buildingtransparency.org/ec3/material-search?category=Aluminum"
                    }
                }
            };
        }
    }
}
