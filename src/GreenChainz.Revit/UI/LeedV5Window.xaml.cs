using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.UI
{
    public partial class LeedV5Window : Window
    {
        private readonly LeedV5Result _result;

        public LeedV5Window(LeedV5Result result)
        {
            InitializeComponent();
            _result = result;
            DisplayResults();
        }

        private void DisplayResults()
        {
            if (_result == null) return;

            ProjectNameText.Text = $"{_result.ProjectName} | {_result.CalculationDate.ToShortDateString()}";
            CertLevelText.Text = _result.CertificationLevel.ToUpper();
            PointsText.Text = _result.PointsDisplay;
            PrereqText.Text = _result.PrereqDisplay;
            ScoreText.Text = $"{_result.PercentScore:F0}%";

            // Set certification badge color
            switch (_result.CertificationLevel)
            {
                case "Platinum":
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(96, 125, 139));
                    break;
                case "Gold":
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    CertLevelText.Foreground = new SolidColorBrush(Colors.Black);
                    break;
                case "Silver":
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                    break;
                case "Certified":
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    break;
                default:
                    CertBadge.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    break;
            }

            // Flatten all credits for the DataGrid
            var allCredits = new List<LeedV5Credit>();
            foreach (var category in _result.Categories)
            {
                allCredits.AddRange(category.Credits);
            }
            CreditsDataGrid.ItemsSource = allCredits;

            // Category TreeView
            CategoryTreeView.ItemsSource = _result.Categories;

            // Carbon Focus Tab
            var ipCategory = _result.Categories.FirstOrDefault(c => c.Code == "IP");
            var mrCategory = _result.Categories.FirstOrDefault(c => c.Code == "MR");

            if (ipCategory != null)
            {
                var carbonAssessment = ipCategory.Credits.FirstOrDefault(c => c.Code == "IPp3");
                if (carbonAssessment != null)
                    CarbonAssessmentStatus.Text = $"Status: {carbonAssessment.Status}\n{carbonAssessment.Description}";
            }

            if (mrCategory != null)
            {
                var mrp2 = mrCategory.Credits.FirstOrDefault(c => c.Code == "MRp2");
                if (mrp2 != null)
                    EmbodiedCarbonStatus.Text = $"Status: {mrp2.Status}\n{mrp2.Description}";

                var mrc2 = mrCategory.Credits.FirstOrDefault(c => c.Code == "MRc2");
                if (mrc2 != null)
                    ReduceEmbodiedStatus.Text = $"Points: {mrc2.PointsEarned}/{mrc2.MaxPoints} | {mrc2.Status}\n{mrc2.Description}";
            }
        }

        private void OpenUSGBC_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.usgbc.org/leed/v5",
                UseShellExecute = true
            });
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = $"LEEDv5_{_result.ProjectName}_{DateTime.Now:yyyyMMdd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new System.IO.StreamWriter(saveDialog.FileName))
                    {
                        writer.WriteLine("LEED v5 BD+C Analysis Report");
                        writer.WriteLine($"Project:,{_result.ProjectName}");
                        writer.WriteLine($"Date:,{_result.CalculationDate}");
                        writer.WriteLine($"Certification Level:,{_result.CertificationLevel}");
                        writer.WriteLine($"Total Points:,{_result.TotalPoints}/{_result.MaxPoints}");
                        writer.WriteLine($"Prerequisites:,{_result.PrerequisitesMet}/{_result.PrerequisitesTotal}");
                        writer.WriteLine();
                        
                        writer.WriteLine("CREDIT BREAKDOWN");
                        writer.WriteLine("Category,Code,Credit,Type,Points,Status,Description");
                        
                        foreach (var category in _result.Categories)
                        {
                            foreach (var credit in category.Credits)
                            {
                                string type = credit.IsPrerequisite ? "Prerequisite" : "Credit";
                                writer.WriteLine($"{category.Name},{credit.Code},{credit.Name},{type},{credit.PointsDisplay},{credit.Status},\"{credit.Description}\"");
                            }
                        }
                    }

                    MessageBox.Show($"Report exported!\n\n{saveDialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
