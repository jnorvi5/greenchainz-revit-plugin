using System;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;

namespace GreenChainz.Revit.Utils
{
    public class MaterialCreationHandler : IExternalEventHandler
    {
        public Material MaterialToCreate { get; set; }

        public MaterialCreationHandler()
        {
        }

        public void Execute(UIApplication app)
        {
            if (MaterialToCreate == null) return;

            UIDocument uidoc = app.ActiveUIDocument;
            if (uidoc == null) return;
            Document doc = uidoc.Document;

            try
            {
                // Use the shared MaterialService from App
                var service = App.MaterialService ?? new MaterialService();
                service.CreateRevitMaterial(doc, app.Application, MaterialToCreate);

                TaskDialog.Show("Success", $"Created material: {MaterialToCreate.Name}");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to create material: {ex.Message}");
            }
        }

        public string GetName()
        {
            return "GreenChainz Material Creator";
        }
    }
}
