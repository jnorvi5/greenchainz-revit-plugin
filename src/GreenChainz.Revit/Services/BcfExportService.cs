using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Service for exporting high-carbon issues to BCF (Building Collaboration Format).
    /// BCF is an open standard for issue tracking in BIM workflows.
    /// </summary>
    public class BcfExportService
    {
        private const string BCF_VERSION = "2.1";
        private const double HIGH_CARBON_THRESHOLD = 5000; // kgCO2e

        /// <summary>
        /// Exports high-carbon materials as BCF issues
        /// </summary>
        public void ExportHighCarbonIssues(AuditResult audit, string outputPath)
        {
            var issues = CreateCarbonIssues(audit);
            
            if (outputPath.EndsWith(".bcf") || outputPath.EndsWith(".bcfzip"))
            {
                ExportAsBcfZip(audit, issues, outputPath);
            }
            else
            {
                ExportAsXml(audit, issues, outputPath);
            }
        }

        /// <summary>
        /// Creates BCF issues from high-carbon materials
        /// </summary>
        private List<BcfIssue> CreateCarbonIssues(AuditResult audit)
        {
            var issues = new List<BcfIssue>();
            int priority = 1;

            foreach (var material in audit.Materials)
            {
                if (material.TotalCarbon > HIGH_CARBON_THRESHOLD)
                {
                    issues.Add(new BcfIssue
                    {
                        Guid = Guid.NewGuid().ToString(),
                        Title = $"High Carbon: {material.MaterialName}",
                        Description = $"This material has {material.TotalCarbon:N0} kgCO2e of embodied carbon.\n\n" +
                                     $"Category: {material.Ec3Category}\n" +
                                     $"Carbon Factor: {material.CarbonFactor:N2} kgCO2e/m3\n" +
                                     $"Volume: {material.Quantity}\n\n" +
                                     $"Recommendation: Consider low-carbon alternatives with EPD certification.",
                        TopicType = "Carbon Issue",
                        TopicStatus = "Open",
                        Priority = priority <= 3 ? "Critical" : "Normal",
                        CreationDate = DateTime.Now,
                        CreationAuthor = "GreenChainz",
                        AssignedTo = "Design Team",
                        IfcGuid = material.IfcGuid,
                        RelatedMaterial = material.MaterialName,
                        CarbonImpact = material.TotalCarbon
                    });
                    priority++;
                }
            }

            // Add summary issue
            if (issues.Count > 0)
            {
                issues.Insert(0, new BcfIssue
                {
                    Guid = Guid.NewGuid().ToString(),
                    Title = $"Carbon Audit Summary: {audit.OverallScore:N0} kgCO2e Total",
                    Description = $"Project: {audit.ProjectName}\n" +
                                 $"Date: {audit.Date:yyyy-MM-dd}\n" +
                                 $"Total Embodied Carbon: {audit.OverallScore:N0} kgCO2e\n" +
                                 $"High-Carbon Issues: {issues.Count}\n\n" +
                                 $"Data Source: {audit.DataSource}",
                    TopicType = "Carbon Summary",
                    TopicStatus = "Active",
                    Priority = "Critical",
                    CreationDate = DateTime.Now,
                    CreationAuthor = "GreenChainz",
                    CarbonImpact = audit.OverallScore
                });
            }

            return issues;
        }

        /// <summary>
        /// Exports as BCF ZIP archive (standard format)
        /// </summary>
        private void ExportAsBcfZip(AuditResult audit, List<BcfIssue> issues, string outputPath)
        {
            // Create temp directory for BCF structure
            string tempDir = Path.Combine(Path.GetTempPath(), "bcf_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // bcf.version
                File.WriteAllText(Path.Combine(tempDir, "bcf.version"), CreateBcfVersion());

                // Create topic folders
                foreach (var issue in issues)
                {
                    string topicDir = Path.Combine(tempDir, issue.Guid);
                    Directory.CreateDirectory(topicDir);

                    // markup.bcf
                    File.WriteAllText(Path.Combine(topicDir, "markup.bcf"), CreateMarkupXml(issue));

                    // viewpoint.bcfv (optional, but good to have)
                    File.WriteAllText(Path.Combine(topicDir, "viewpoint.bcfv"), CreateViewpointXml(issue));
                }

                // Create ZIP
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                
                ZipFile.CreateFromDirectory(tempDir, outputPath);
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Exports as simple XML (for debugging/preview)
        /// </summary>
        private void ExportAsXml(AuditResult audit, List<BcfIssue> issues, string outputPath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8
            };

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("BCF");
                writer.WriteAttributeString("Version", BCF_VERSION);

                writer.WriteStartElement("Project");
                writer.WriteElementString("Name", audit.ProjectName);
                writer.WriteElementString("TotalCarbon", audit.OverallScore.ToString("N0"));
                writer.WriteElementString("Date", audit.Date.ToString("o"));
                writer.WriteEndElement();

                writer.WriteStartElement("Topics");
                foreach (var issue in issues)
                {
                    WriteTopicXml(writer, issue);
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private string CreateBcfVersion()
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Version VersionId=""{BCF_VERSION}"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <DetailedVersion>{BCF_VERSION}</DetailedVersion>
</Version>";
        }

        private string CreateMarkupXml(BcfIssue issue)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<Markup xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">");
            sb.AppendLine(@"  <Header>");
            sb.AppendLine($@"    <File IfcProject=""{issue.Guid}"" />");
            sb.AppendLine(@"  </Header>");
            sb.AppendLine(@"  <Topic Guid=""" + issue.Guid + @""" TopicType=""" + issue.TopicType + @""" TopicStatus=""" + issue.TopicStatus + @""">");
            sb.AppendLine($@"    <Title>{EscapeXml(issue.Title)}</Title>");
            sb.AppendLine($@"    <Priority>{issue.Priority}</Priority>");
            sb.AppendLine($@"    <CreationDate>{issue.CreationDate:o}</CreationDate>");
            sb.AppendLine($@"    <CreationAuthor>{issue.CreationAuthor}</CreationAuthor>");
            if (!string.IsNullOrEmpty(issue.AssignedTo))
                sb.AppendLine($@"    <AssignedTo>{issue.AssignedTo}</AssignedTo>");
            sb.AppendLine($@"    <Description>{EscapeXml(issue.Description)}</Description>");
            sb.AppendLine(@"  </Topic>");
            
            // Comments
            sb.AppendLine(@"  <Comment Guid=""" + Guid.NewGuid() + @""">");
            sb.AppendLine($@"    <Date>{DateTime.Now:o}</Date>");
            sb.AppendLine(@"    <Author>GreenChainz</Author>");
            sb.AppendLine($@"    <Comment>Carbon Impact: {issue.CarbonImpact:N0} kgCO2e</Comment>");
            sb.AppendLine(@"  </Comment>");
            
            sb.AppendLine(@"</Markup>");
            return sb.ToString();
        }

        private string CreateViewpointXml(BcfIssue issue)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<VisualizationInfo Guid=""" + Guid.NewGuid() + @""">");
            
            // Component selection (if IFC GUID available)
            if (!string.IsNullOrEmpty(issue.IfcGuid))
            {
                sb.AppendLine(@"  <Components>");
                sb.AppendLine(@"    <Selection>");
                sb.AppendLine($@"      <Component IfcGuid=""{issue.IfcGuid}"" />");
                sb.AppendLine(@"    </Selection>");
                sb.AppendLine(@"  </Components>");
            }
            
            sb.AppendLine(@"</VisualizationInfo>");
            return sb.ToString();
        }

        private void WriteTopicXml(XmlWriter writer, BcfIssue issue)
        {
            writer.WriteStartElement("Topic");
            writer.WriteAttributeString("Guid", issue.Guid);
            writer.WriteAttributeString("TopicType", issue.TopicType);
            writer.WriteAttributeString("TopicStatus", issue.TopicStatus);

            writer.WriteElementString("Title", issue.Title);
            writer.WriteElementString("Description", issue.Description);
            writer.WriteElementString("Priority", issue.Priority);
            writer.WriteElementString("CreationDate", issue.CreationDate.ToString("o"));
            writer.WriteElementString("CreationAuthor", issue.CreationAuthor);
            writer.WriteElementString("CarbonImpact", issue.CarbonImpact.ToString("N0"));

            if (!string.IsNullOrEmpty(issue.IfcGuid))
                writer.WriteElementString("IfcGuid", issue.IfcGuid);

            if (!string.IsNullOrEmpty(issue.RelatedMaterial))
                writer.WriteElementString("RelatedMaterial", issue.RelatedMaterial);

            writer.WriteEndElement();
        }

        private string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }

    /// <summary>
    /// Represents a BCF issue/topic
    /// </summary>
    public class BcfIssue
    {
        public string Guid { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TopicType { get; set; }
        public string TopicStatus { get; set; }
        public string Priority { get; set; }
        public DateTime CreationDate { get; set; }
        public string CreationAuthor { get; set; }
        public string AssignedTo { get; set; }
        public string IfcGuid { get; set; }
        public string RelatedMaterial { get; set; }
        public double CarbonImpact { get; set; }
    }
}
