using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.Views
{
    public partial class AuditResultsWindow : Window
    {
        private AuditResult _auditResult;

        public AuditResultsWindow(AuditResult auditResult)
        {
            InitializeComponent();
            _auditResult = auditResult;
            LoadData();
        }

        private void LoadData()
        {
            if (_auditResult == null) return;

            ScoreText.Text = $"{_auditResult.OverallScore:N0} kgCO2e";
            SummaryText.Text = _auditResult.Summary;
            MaterialsGrid.ItemsSource = _auditResult.Materials;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "PDF file (*.pdf)|*.pdf";
                saveFileDialog.FileName = $"CarbonAudit_{_auditResult.ProjectName}_{DateTime.Now:yyyyMMdd}.pdf";

                if (saveFileDialog.ShowDialog() == true)
                {
                    string outputPath = saveFileDialog.FileName;
                    PdfExportService service = new PdfExportService();
                    service.GenerateAuditReport(_auditResult, outputPath);

                    // Open the PDF
                    Process.Start(outputPath);

                    MessageBox.Show("Report exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
