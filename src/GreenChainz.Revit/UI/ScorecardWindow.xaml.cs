using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.UI
{
    public partial class ScorecardWindow : Window
    {
        private readonly SustainabilityScorecard _scorecard;

        public ScorecardWindow(SustainabilityScorecard scorecard)
        {
            InitializeComponent();
            _scorecard = scorecard;
            DisplayScorecard();
        }

        private void DisplayScorecard()
        {
            if (_scorecard == null) return;

            // Header
            ProjectNameText.Text = $"{_scorecard.ProjectName} | {_scorecard.GeneratedDate:MMM dd, yyyy}";

            // Overall Grade
            OverallGradeText.Text = _scorecard.OverallGrade;
            OverallScoreText.Text = $"Score: {_scorecard.OverallScore}/100";
            SetGradeColor(OverallGradeText, _scorecard.OverallGrade);
            SetGradeBorderColor(OverallGradeBorder, _scorecard.OverallGrade);

            // EPD Coverage
            EpdGradeText.Text = _scorecard.EpdScore.Grade;
            EpdValueText.Text = $"{_scorecard.EpdScore.CoveragePercent:F0}% Coverage";
            EpdDetailText.Text = $"{_scorecard.EpdScore.MaterialsWithEpd} of {_scorecard.EpdScore.TotalMaterials} materials";
            EpdStatusText.Text = _scorecard.EpdScore.CoveragePercent >= 20 ? "LEED Compliant" : "Below LEED Threshold";
            EpdStatusText.Foreground = _scorecard.EpdScore.CoveragePercent >= 20 
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) 
                : new SolidColorBrush(Color.FromRgb(244, 67, 54));
            SetGradeColor(EpdGradeText, _scorecard.EpdScore.Grade);

            // GWP Score
            GwpGradeText.Text = _scorecard.GwpScore.Grade;
            GwpValueText.Text = $"{_scorecard.GwpScore.TotalGwp:N0} kgCO2e";
            GwpIntensityText.Text = $"{_scorecard.GwpScore.GwpPerSqM:F1} kgCO2e/m²";
            GwpReductionText.Text = _scorecard.GwpScore.ReductionDisplay;
            GwpReductionText.Foreground = _scorecard.GwpScore.ReductionPercent >= 0
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                : new SolidColorBrush(Color.FromRgb(244, 67, 54));
            SetGradeColor(GwpGradeText, _scorecard.GwpScore.Grade);

            // Verification Tier
            TierText.Text = _scorecard.VerificationScore.Tier.ToUpper();
            TierBreakdownText.Text = GetTierDescription(_scorecard.VerificationScore.Tier);
            TierDetailText.Text = $"P:{_scorecard.VerificationScore.PlatinumCount} | G:{_scorecard.VerificationScore.GoldCount} | S:{_scorecard.VerificationScore.SilverCount} | N:{_scorecard.VerificationScore.UnverifiedCount}";
            SetTierColor(TierText, _scorecard.VerificationScore.Tier);

            // Materials
            MaterialsDataGrid.ItemsSource = _scorecard.Materials;

            // Footer
            LeedPointsText.Text = $"Est. LEED Points: {_scorecard.EstimatedLeedPoints}";
            BuyCleanText.Text = _scorecard.BuyCleanCompliant ? "Buy Clean: Compliant" : "Buy Clean: Non-Compliant";
            BuyCleanBorder.Background = _scorecard.BuyCleanCompliant
                ? new SolidColorBrush(Color.FromRgb(232, 245, 233))
                : new SolidColorBrush(Color.FromRgb(255, 235, 238));
            BuyCleanText.Foreground = _scorecard.BuyCleanCompliant
                ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                : new SolidColorBrush(Color.FromRgb(198, 40, 40));
        }

        private void SetGradeColor(System.Windows.Controls.TextBlock textBlock, string grade)
        {
            switch (grade)
            {
                case "A":
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    break;
                case "B":
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(139, 195, 74)); // Light Green
                    break;
                case "C":
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Amber
                    break;
                case "D":
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    break;
                default:
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                    break;
            }
        }

        private void SetGradeBorderColor(System.Windows.Controls.Border border, string grade)
        {
            switch (grade)
            {
                case "A":
                    border.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Light Green
                    break;
                case "B":
                    border.Background = new SolidColorBrush(Color.FromRgb(241, 248, 233)); // Lighter Green
                    break;
                case "C":
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 248, 225)); // Light Amber
                    break;
                case "D":
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)); // Light Orange
                    break;
                default:
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light Red
                    break;
            }
        }

        private void SetTierColor(System.Windows.Controls.TextBlock textBlock, string tier)
        {
            switch (tier)
            {
                case "Platinum":
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(96, 125, 139)); // Blue Grey
                    break;
                case "Gold":
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Gold
                    break;
                case "Silver":
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Silver
                    break;
                default:
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)); // Grey
                    break;
            }
        }

        private string GetTierDescription(string tier)
        {
            switch (tier)
            {
                case "Platinum": return "Third-Party Verified + CoC";
                case "Gold": return "EPD Available";
                case "Silver": return "Self-Declared Claims";
                default: return "No Verification";
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = $"Scorecard_{_scorecard.ProjectName}_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var pdfService = new ScorecardPdfService();
                    pdfService.ExportScorecard(_scorecard, saveDialog.FileName);
                    
                    MessageBox.Show($"Scorecard exported!\n\n{saveDialog.FileName}", "Export Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed:\n\n{ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
