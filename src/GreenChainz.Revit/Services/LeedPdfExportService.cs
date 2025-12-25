using System;
using System.IO;
using GreenChainz.Revit.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace GreenChainz.Revit.Services
{
    public class LeedPdfExportService
    {
        public void GenerateLeedReport(LeedResult result, string outputPath)
        {
            try
            {
                using (PdfWriter writer = new PdfWriter(outputPath))
                using (PdfDocument pdf = new PdfDocument(writer))
                using (Document doc = new Document(pdf))
                {
                    // Header
                    doc.Add(new Paragraph("GreenChainz")
                        .SetFontSize(16)
                        .SetBold()
                        .SetFontColor(new DeviceRgb(34, 139, 34)));

                    doc.Add(new Paragraph("LEED Certification Analysis Report")
                        .SetFontSize(24)
                        .SetBold()
                        .SetTextAlignment(TextAlignment.CENTER));

                    doc.Add(new Paragraph($"Project: {result.ProjectName}")
                        .SetFontSize(14));
                    
                    doc.Add(new Paragraph($"Date: {result.CalculationDate.ToShortDateString()}")
                        .SetFontSize(12));

                    doc.Add(new Paragraph("\n"));

                    // Certification Summary
                    Color certColor = GetCertificationColor(result.CertificationLevel);
                    
                    doc.Add(new Paragraph($"Certification Level: {result.CertificationLevel.ToUpper()}")
                        .SetFontSize(28)
                        .SetBold()
                        .SetFontColor(certColor)
                        .SetTextAlignment(TextAlignment.CENTER));

                    doc.Add(new Paragraph($"Score: {result.TotalPoints}/{result.MaxPossiblePoints} points ({result.PercentageScore:F0}%)")
                        .SetFontSize(18)
                        .SetTextAlignment(TextAlignment.CENTER));

                    doc.Add(new Paragraph("\n"));

                    // Credit Breakdown Table
                    doc.Add(new Paragraph("Credit Breakdown")
                        .SetFontSize(18)
                        .SetBold());

                    Table table = new Table(5).UseAllAvailableWidth();
                    
                    // Headers
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Category").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Credit").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Points").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Status").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Details").SetBold()));

                    // Data rows
                    if (result.Credits != null)
                    {
                        foreach (var credit in result.Credits)
                        {
                            table.AddCell(credit.Category ?? "");
                            table.AddCell(credit.CreditName ?? "");
                            table.AddCell(credit.PointsDisplay ?? "0/0");
                            
                            Cell statusCell = new Cell().Add(new Paragraph(credit.Status ?? ""));
                            if (credit.Status == "Achieved")
                                statusCell.SetFontColor(new DeviceRgb(0, 128, 0));
                            else if (credit.Status == "Not Met")
                                statusCell.SetFontColor(new DeviceRgb(255, 0, 0));
                            else
                                statusCell.SetFontColor(new DeviceRgb(255, 165, 0));
                            table.AddCell(statusCell);
                            
                            table.AddCell(credit.Description ?? "");
                        }
                    }

                    doc.Add(table);
                    doc.Add(new Paragraph("\n"));

                    // Recommendations
                    doc.Add(new Paragraph("Recommendations to Improve Score")
                        .SetFontSize(18)
                        .SetBold());

                    List recommendations = new List()
                        .SetSymbolIndent(12)
                        .SetListSymbol("\u2022");

                    recommendations.Add(new ListItem("Use more recycled content materials (target 20%+)"));
                    recommendations.Add(new ListItem("Source materials regionally within 500 miles"));
                    recommendations.Add(new ListItem("Specify low-VOC paints, adhesives, and sealants"));
                    recommendations.Add(new ListItem("Include rapidly renewable materials like bamboo or cork"));
                    recommendations.Add(new ListItem("Use FSC-certified wood products"));
                    recommendations.Add(new ListItem("Consider reclaimed/salvaged materials"));

                    doc.Add(recommendations);

                    doc.Add(new Paragraph("\n\n"));

                    // Footer
                    doc.Add(new Paragraph("Generated by GreenChainz Revit Plugin")
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.CENTER));
                    
                    doc.Add(new Paragraph("This report provides estimates based on material analysis. Consult a LEED AP for official certification.")
                        .SetFontSize(8)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontColor(ColorConstants.GRAY));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating LEED PDF: " + ex.Message, ex);
            }
        }

        private DeviceRgb GetCertificationColor(string level)
        {
            switch (level)
            {
                case "Platinum": return new DeviceRgb(169, 169, 169);
                case "Gold": return new DeviceRgb(255, 215, 0);
                case "Silver": return new DeviceRgb(192, 192, 192);
                case "Certified": return new DeviceRgb(76, 175, 80);
                default: return new DeviceRgb(128, 128, 128);
            }
        }
    }
}
