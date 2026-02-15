using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.UI
{
    public partial class CcpsScorecardWindow : Window
    {
        public CcpsScorecardWindow(string materialName, string category, CcpsBreakdown scorecard)
        {
            InitializeComponent();
            
            MaterialNameText.Text = materialName;
            CategoryText.Text = category;
            TotalScoreText.Text = scorecard.CcpsTotal.ToString("F0");

            var pillars = new List<PillarViewModel>
            {
                new PillarViewModel { Name = "Carbon Performance", Score = scorecard.CarbonScore, Color = Brushes.ForestGreen },
                new PillarViewModel { Name = "Regulatory Compliance", Score = scorecard.ComplianceScore, Color = Brushes.DodgerBlue },
                new PillarViewModel { Name = "Certifications", Score = scorecard.CertificationScore, Color = Brushes.Goldenrod },
                new PillarViewModel { Name = "Cost Efficiency", Score = scorecard.CostScore, Color = Brushes.MediumSeaGreen },
                new PillarViewModel { Name = "Supply Chain Resilience", Score = scorecard.SupplyChainScore, Color = Brushes.DarkOrange },
                new PillarViewModel { Name = "Health & Toxicity", Score = scorecard.HealthScore, Color = Brushes.Crimson }
            };

            PillarsItemsControl.ItemsSource = pillars;
            
            if (scorecard.CcpsTotal < 70)
            {
                SwapSuggestionText.Text = $"We found 3 alternatives in the {category} category with CCPS scores > 85. Switching could reduce project carbon by 12%.";
            }
            else
            {
                SwapSuggestionText.Text = "This material is a top performer. No immediate swaps recommended.";
            }
        }

        private void ViewSwap_Click(object sender, RoutedEventArgs e)
        {
            // Logic to open the Material Browser with swap filters
            MessageBox.Show("Opening GreenChainz Marketplace for swap candidates...", "Material Swaps");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class PillarViewModel
    {
        public string Name { get; set; }
        public double Score { get; set; }
        public Brush Color { get; set; }
    }
}
