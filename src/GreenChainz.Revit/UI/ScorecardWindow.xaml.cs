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

            // Location
            if (_scorecard.Location != null)
            {
                LocationText.Text = _scorecard.Location.Display;
                GridIntensityText.Text = _scorecard.Location.GridDisplay;
                ClimateZoneText.Text = _scorecard.Location.ClimateZone;
            }

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
            TierDetailText.Text = _scorecard.VerificationScore.Breakdown;
            SetTierColor(TierText, _scorecard.VerificationScore.Tier);

            // Buy Clean Compliance Panel
            if (_scorecard.BuyCleanInfo?.HasRequirements == true)
            {
                BuyCleanPanel.Visibility = Visibility.Visible;
                BuyCleanTitleText.Text = _scorecard.BuyCleanInfo.PolicyName;
                
                bool compliant = _scorecard.BuyCleanInfo.ConcreteCompliant && _scorecard.BuyCleanInfo.SteelCompliant;
                BuyCleanStatusText.Text = compliant ? "COMPLIANT" : "NON-COMPLIANT";
                BuyCleanStatusText.Foreground = compliant 
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));

                ConcreteGwpText.Text = $"{_scorecard.BuyCleanInfo.ActualConcreteGwp:N0} / {_scorecard.BuyCleanInfo.ConcreteLimit:N0} kgCO2e/m³";
                ConcreteGwpText.Foreground = _scorecard.BuyCleanInfo.ConcreteCompliant
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));

                SteelGwpText.Text = $"{_scorecard.BuyCleanInfo.ActualSteelGwp:N0} / {_scorecard.BuyCleanInfo.SteelLimit:N0} kgCO2e/ton";
                SteelGwpText.Foreground = _scorecard.BuyCleanInfo.SteelCompliant
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            else
            {
                BuyCleanPanel.Visibility = Visibility.Collapsed;
            }

            // Materials
            MaterialsDataGrid.ItemsSource = _scorecard.Materials;

            // Footer
            int basePoints = _scorecard.EstimatedLeedPoints - _scorecard.RegionalBonusPoints;
            LeedPointsText.Text = $"Est. LEED Points: {basePoints}";
            
            if (_scorecard.RegionalBonusPoints > 0)
            {
                RegionalPointsBorder.Visibility = Visibility.Visible;
                RegionalPointsText.Text = $"Regional Bonus: +{_scorecard.RegionalBonusPoints}";
            }
            else
            {
                RegionalPointsBorder.Visibility = Visibility.Collapsed;
            }

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
            textBlock.Foreground = grade switch
            {
                "A" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "B" => new SolidColorBrush(Color.FromRgb(139, 195, 74)),
                "C" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                "D" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                _ => new SolidColorBrush(Color.FromRgb(244, 67, 54))
            };
        }

        private void SetGradeBorderColor(System.Windows.Controls.Border border, string grade)
        {
            border.Background = grade switch
            {
                "A" => new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                "B" => new SolidColorBrush(Color.FromRgb(241, 248, 233)),
                "C" => new SolidColorBrush(Color.FromRgb(255, 248, 225)),
                "D" => new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                _ => new SolidColorBrush(Color.FromRgb(255, 235, 238))
            };
        }

        private void SetTierColor(System.Windows.Controls.TextBlock textBlock, string tier)
        {
            textBlock.Foreground = tier switch
            {
                "Platinum" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),
                "Gold" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                "Silver" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                _ => new SolidColorBrush(Color.FromRgb(117, 117, 117))
            };
        }

        private string GetTierDescription(string tier) => tier switch
        {
            "Platinum" => "Third-Party Verified + CoC",
            "Gold" => "EPD Available",
            "Silver" => "Self-Declared Claims",
            _ => "No Verification"
        };

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
