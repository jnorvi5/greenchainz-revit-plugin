using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Extracts BIM context from current selection for AI agents.
    /// </summary>
    public class ContextService
    {
        public static string GetSelectionContext(UIDocument uidoc)
        {
            if (uidoc == null) return "No active document.";

            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            if (selectedIds.Count == 0) return "No elements selected.";

            Document doc = uidoc.Document;
            Element firstElem = doc.GetElement(selectedIds.First());

            if (firstElem == null) return "Selection empty.";

            string category = firstElem.Category?.Name ?? "Unknown Category";
            string name = firstElem.Name;
            
            // Try to get material info
            var materialIds = firstElem.GetMaterialIds(false);
            string materialInfo = "";
            if (materialIds.Count > 0)
            {
                var mat = doc.GetElement(materialIds.First()) as Autodesk.Revit.DB.Material;
                if (mat != null)
                {
                    materialInfo = $", Material: {mat.Name}";
                }
            }

            return $"Element: {name}, Category: {category}{materialInfo}";
        }
    }
}
