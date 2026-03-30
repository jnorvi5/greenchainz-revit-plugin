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
    public class PdfExportService
    {
        public void GenerateAuditReport(AuditResult audit, string outputPath)
        {
            if (audit == null)
                throw new ArgumentNullException(nameof(audit), "Audit result cannot be null.");

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path must not be empty.", nameof(outputPath));

            // Ensure the output directory exists
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception dirEx)
                {
                    throw new InvalidOperationException(
                        $"Cannot create output directory '{outputDir}': {dirEx.Message}", dirEx);
                }
            }

            // Resolve safe values for nullable / potentially null fields
            string projectName = !string.IsNullOrWhiteSpace(audit.ProjectName)
                ? audit.ProjectName
                : "Untitled Project";

            string summary = !string.IsNullOrWhiteSpace(audit.Summary)
                ? audit.Summary
                : "No summary available.";

            string dateDisplay = audit.Date != default
                ? audit.Date.ToShortDateString()
                : DateTime.Now.ToShortDateString();

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

                    doc.Add(new Paragraph($"Project: {projectName}")
                        .SetFontSize(18)
                        .SetBold());

                    doc.Add(new Paragraph($"Date: {dateDisplay}")
                        .SetFontSize(14));

                    doc.Add(new Paragraph("\n"));

                    // Carbon Score
                    doc.Add(new Paragraph("Overall Carbon Score")
                        .SetFontSize(14)
                        .SetBold()
                        .SetTextAlignment(TextAlignment.CENTER));

                    doc.Add(new Paragraph($"{audit.OverallScore:N0} kgCO2e")
                        .SetFontSize(36)
                        .SetBold()
                        .SetFontColor(new DeviceRgb(34, 139, 34))
                        .SetTextAlignment(TextAlignment.CENTER));

                    doc.Add(new Paragraph("\n"));

                    // Summary
                    doc.Add(new Paragraph("Summary").SetFontSize(18).SetBold());
                    doc.Add(new Paragraph(summary));
                    doc.Add(new Paragraph("\n"));

                    // Material Breakdown Table
                    doc.Add(new Paragraph("Material Breakdown").SetFontSize(18).SetBold());

                    Table table = new Table(4).UseAllAvailableWidth();

                    // Headers
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Material").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Quantity").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Factor").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Total (kgCO2e)").SetBold()));

                    // Data rows — guard against null collection and null row fields
                    if (audit.Materials != null)
                    {
                        foreach (var material in audit.Materials)
                        {
                            if (material == null) continue;
                            table.AddCell(material.MaterialName ?? "Unknown");
                            table.AddCell(material.Quantity ?? "0");
                            table.AddCell(material.CarbonFactor.ToString("N2"));
                            table.AddCell(material.TotalCarbon.ToString("N0"));
                        }
                    }
                    else
                    {
                        // Placeholder row so the table is never empty
                        table.AddCell(new Cell(1, 4).Add(
                            new Paragraph("No material data available.").SetItalic()));
                    }

                    doc.Add(table);
                    doc.Add(new Paragraph("\n"));

                    // Recommendations
                    doc.Add(new Paragraph("Recommendations").SetFontSize(18).SetBold());

                    if (audit.Recommendations != null && audit.Recommendations.Count > 0)
                    {
                        List list = new List().SetSymbolIndent(12).SetListSymbol("\u2022");
                        foreach (var rec in audit.Recommendations)
                        {
                            if (rec == null) continue;
                            string description = rec.Description ?? "No description provided";
                            list.Add(new ListItem(
                                $"{description} (Potential Savings: {rec.PotentialSavings:N0} kgCO2e)"));
                        }
                        doc.Add(list);
                    }
                    else
                    {
                        doc.Add(new Paragraph("No recommendations available.").SetItalic());
                    }

                    doc.Add(new Paragraph("\n\n"));

                    // Footer
                    doc.Add(new Paragraph(
                            "Generated by GreenChainz.\nDisclaimer: This report is an estimate based on available data.")
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.CENTER));

                    doc.Add(new Paragraph("Contact: support@greenchainz.com")
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.CENTER));
                }
            }
            catch (IOException ioEx)
            {
                throw new InvalidOperationException(
                    $"Failed to write PDF to '{outputPath}'. Check that the path is writable and not locked by another process. Details: {ioEx.Message}",
                    ioEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"An unexpected error occurred while generating the audit PDF for project '{projectName}'. Details: {ex.Message}",
                    ex);
            }
        }
    }
}
