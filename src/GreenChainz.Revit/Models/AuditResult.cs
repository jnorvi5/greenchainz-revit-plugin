using System.Collections.Generic;

namespace GreenChainz.Revit.Models
{
    public class AuditResult
    {
        public double CarbonScore { get; set; }
        public string Rating { get; set; } // e.g., "A", "B", "C"
        public List<string> Recommendations { get; set; }
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GreenChainz.Revit.Models
{
    public class AuditResult : INotifyPropertyChanged
    {
        private int _overallScore;
        private string _summary;
        private ObservableCollection<MaterialAuditItem> _materials;
        private ObservableCollection<string> _recommendations;

        public AuditResult()
        {
            Materials = new ObservableCollection<MaterialAuditItem>();
            Recommendations = new ObservableCollection<string>();
        }

        public int OverallScore
        {
            get => _overallScore;
            set
            {
                _overallScore = value;
                OnPropertyChanged();
            }
        }

        public string Summary
        {
            get => _summary;
            set
            {
                _summary = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MaterialAuditItem> Materials
        {
            get => _materials;
            set
            {
                _materials = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> Recommendations
        {
            get => _recommendations;
            set
            {
                _recommendations = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MaterialAuditItem : INotifyPropertyChanged
    {
        private string _name;
        private string _quantity;
        private string _carbon;
        private int _score;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }

        public string Carbon
        {
            get => _carbon;
            set
            {
                _carbon = value;
                OnPropertyChanged();
            }
        }

        public int Score
        {
            get => _score;
            set
            {
                _score = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
