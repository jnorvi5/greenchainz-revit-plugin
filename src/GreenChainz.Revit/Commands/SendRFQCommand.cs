using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    /// <summary>
    /// Command to send Request for Quotation (RFQ) to suppliers.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class SendRFQCommand : IExternalCommand
    {
        /// <summary>
        /// Execute the Send RFQ command.
        /// </summary>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Get selected elements
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

                List<RFQItem> materialList = new List<RFQItem>();

                if (selectedIds.Count > 0)
                {
                    foreach (ElementId id in selectedIds)
                    {
                        Element elem = doc.GetElement(id);
                        if (elem == null) continue;

                        // Try to get material information
                        foreach (ElementId matId in elem.GetMaterialIds(false))
                        {
                            Autodesk.Revit.DB.Material mat = doc.GetElement(matId) as Autodesk.Revit.DB.Material;
                            if (mat != null)
                            {
                                materialList.Add(new RFQItem(mat.Name, 1.0, "ea"));
                            }
                        }

                        // If no materials directly, just add element name
                        if (!elem.GetMaterialIds(false).Any())
                        {
                            materialList.Add(new RFQItem(elem.Name, 1.0, "ea"));
                        }
                    }
                }

                // Show Dialog
                CreateRFQDialog dialog = new CreateRFQDialog(materialList);
                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    return Result.Succeeded;
                }

                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
