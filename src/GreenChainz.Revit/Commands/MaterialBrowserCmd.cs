using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using GreenChainz.Revit.Utils;
using MaterialData = GreenChainz.Revit.Models.Material;

namespace GreenChainz.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class MaterialBrowserCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DockablePaneId dpid = new DockablePaneId(App.MaterialBrowserPaneId);
                DockablePane dp = commandData.Application.GetDockablePane(dpid);
                dp.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        public static void ApplyMaterialToSelection(MaterialData data, UIDocument uidoc)
        {
            Document doc = uidoc.Document;
            var selection = uidoc.Selection.GetElementIds();

            if (selection.Count == 0)
            {
                TaskDialog.Show("GreenChainz", "Please select elements to specify material.");
                return;
            }

            using (Transaction t = new Transaction(doc, "GreenChainz Specification"))
            {
                t.Start();

                try
                {
                    // 1. Identify Categories from selection
                    CategorySet categories = doc.Application.Create.NewCategorySet();
                    foreach (ElementId id in selection)
                    {
                        Element elem = doc.GetElement(id);
                        if (elem != null && elem.Category != null)
                        {
                            categories.Insert(elem.Category);
                        }
                    }

                    if (categories.IsEmpty)
                    {
                        t.RollBack();
                        return;
                    }

                    // 2. Ensure "GreenChainz_GWP" parameter exists and is bound to these categories
                    DefinitionFile sharedParamFile = doc.Application.OpenSharedParameterFile();
                    if (sharedParamFile == null)
                    {
                         // If we can't access shared params, we can't create.
                         // Try standard helper initialization if needed, but assuming App startup did it.
                         // But we might need to point to the file again if it wasn't set.
                         // For now, rely on Utils.
                         string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GreenChainzSharedParams.txt");
                         if (System.IO.File.Exists(path))
                             doc.Application.SharedParametersFilename = path;

                         sharedParamFile = doc.Application.OpenSharedParameterFile();
                    }

                    if (sharedParamFile != null)
                    {
                        DefinitionGroup group = sharedParamFile.Groups.get_Item("GreenChainzData");
                        if (group == null) group = sharedParamFile.Groups.Create("GreenChainzData");

                        // Check/Create "GreenChainz_GWP"
                        SharedParameterHelper.CreateAndBindParam(doc, doc.Application, group, "GreenChainz_GWP", SpecTypeId.Number, categories);
                    }

                    // 3. Set the value
                    foreach (ElementId id in selection)
                    {
                        Element elem = doc.GetElement(id);
                        Parameter param = elem.LookupParameter("GreenChainz_GWP");
                        if (param != null && !param.IsReadOnly)
                        {
                             param.Set(data.EmbodiedCarbon);
                        }
                    }

                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    TaskDialog.Show("Error", "Failed to apply material: " + ex.Message);
                }
            }
        }
    }
}
