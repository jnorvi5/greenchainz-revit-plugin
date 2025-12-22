using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.Utils;

namespace GreenChainz.Revit.UI
{
    public partial class MaterialBrowserPanel : UserControl, IDockablePaneProvider
    {
        private readonly MaterialService _service;
        private ObservableCollection<Material> _allMaterials;
        private ICollectionView _filteredView;

        // External Event Handling
        private ExternalEvent _externalEvent;
        private MaterialCreationHandler _handler;

        /// <summary>
        /// Creates a MaterialBrowserPanel with an optional MaterialService.
        /// If no service is provided, a default one with mock data will be created.
        /// </summary>
        public MaterialBrowserPanel(MaterialService service = null)
        {
            InitializeComponent();
            _service = service ?? new MaterialService();
            _allMaterials = new ObservableCollection<Material>();

            // Setup External Event
            _handler = new MaterialCreationHandler();
            _externalEvent = ExternalEvent.Create(_handler);

            // Setup loading
            this.Loaded += MaterialBrowserPanel_Loaded;
        }

        private async void MaterialBrowserPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (_allMaterials.Count > 0) return; // Already loaded

            try
            {
                // Show loading indicator
                SetLoadingState(true);

                var data = await _service.GetMaterialsAsync();
                foreach (var item in data)
                {
                    _allMaterials.Add(item);
                }

                _filteredView = CollectionViewSource.GetDefaultView(_allMaterials);
                _filteredView.Filter = FilterMaterials;
                MaterialsGrid.ItemsSource = _filteredView;

                // Update status to show data source
                UpdateDataSourceStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading materials: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            // Disable controls while loading
            SearchBox.IsEnabled = !isLoading;
            CategoryFilter.IsEnabled = !isLoading;
            AddToProjectBtn.IsEnabled = !isLoading && MaterialsGrid.SelectedItem != null;
        }

        private void UpdateDataSourceStatus()
        {
            // Could add a status bar or indicator showing if using mock or live data
            string source = _service.IsUsingMockData ? "Mock Data" : "SDA Live";
            System.Diagnostics.Debug.WriteLine($"Material Browser loaded with: {source}");
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
        }

        private bool FilterMaterials(object obj)
        {
            if (obj is Material material)
            {
                // 1. Text Search
                string searchText = SearchBox.Text?.ToLower() ?? "";
                bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                                     material.Name.ToLower().Contains(searchText) ||
                                     material.Manufacturer.ToLower().Contains(searchText);

                // 2. Category Filter
                string selectedCategory = (CategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
                bool matchesCategory = selectedCategory == "All" || material.Category == selectedCategory;

                return matchesSearch && matchesCategory;
            }
            return false;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filteredView?.Refresh();
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _filteredView?.Refresh();
        }

        private void MaterialsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = MaterialsGrid.SelectedItem != null;
            AddToProjectBtn.IsEnabled = hasSelection;
        }

        private void AddToProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialsGrid.SelectedItem is Material selectedMaterial)
            {
                // Pass data to handler
                _handler.MaterialToCreate = selectedMaterial;

                // Raise the event to run entirely inside Revit's API context
                _externalEvent.Raise();
            }
        }

        /// <summary>
        /// Refreshes the material list from the data source.
        /// </summary>
        public async Task RefreshMaterialsAsync()
        {
            _allMaterials.Clear();

            try
            {
                SetLoadingState(true);
                var data = await _service.GetMaterialsAsync();
                foreach (var item in data)
                {
                    _allMaterials.Add(item);
                }
                _filteredView?.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing materials: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }
    }
}
