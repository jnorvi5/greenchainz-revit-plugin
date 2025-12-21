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
                        // This is a simplified extraction. In reality, we might query materials of the element.
                        // For now, we use the element name as the material name placeholder if no material is found.

                        // Check if element has materials
                        foreach (ElementId matId in elem.GetMaterialIds(false))
                        {
                            Material mat = doc.GetElement(matId) as Material;
                            if (mat != null)
                            {
                                materialList.Add(new RFQItem
                                {
                                    MaterialName = mat.Name,
                                    Quantity = 1.0, // Default quantity
                                    Unit = "ea"
                                });
                            }
                        }

                        // If no materials directly, just add element name
                        if (materialList.Count == 0 || !elem.GetMaterialIds(false).Any())
                        {
                            materialList.Add(new RFQItem
                            {
                                MaterialName = elem.Name,
                                Quantity = 1.0,
                                Unit = "ea"
                            });
                        }
                    }
                }
                else
                {
                    // No selection, start with empty list or example
                    // materialList.Add(new RFQItem("Example Material", 10, "m2"));
                }

                // Show Dialog
                CreateRFQDialog dialog = new CreateRFQDialog(materialList);

                // We need to use ShowDialog() from WPF.
                // Since Revit is the parent, we might want to set the owner handle, but usually ShowDialog works.
                // To ensure it is modal to Revit, we can use a helper or just ShowDialog.

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    // Success handled inside dialog (API call)
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
