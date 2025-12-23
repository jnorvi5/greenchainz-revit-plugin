using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Utils;

namespace GreenChainz.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class MaterialBrowserCmd : IExternalCommand
    {
        private static MaterialBrowserCmd _instance;
        private UIApplication _uiApp;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _uiApp = commandData.Application;
            _instance = this;
            try
            {
                // Show the UI
                DockablePaneId dpid = new DockablePaneId(App.MaterialBrowserPaneId);
                commandData.Application.GetDockablePane(dpid).Show();

                // Init Parameters (The Trojan Horse)
                var doc = commandData.Application.ActiveUIDocument.Document;
                using (Transaction t = new Transaction(doc, "Init GreenChainz"))
                {
                    t.Start();
                    SharedParameterHelper.CreateSharedParameters(doc, commandData.Application.Application);
                    t.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        // Call this from your UI to inject data
        public void ApplyMaterial(MaterialData data)
        {
            if (_uiApp == null) return;
            var uidoc = _uiApp.ActiveUIDocument;
            var doc = uidoc.Document;
            
            using (Transaction t = new Transaction(doc, "GreenChainz: Apply Spec"))
            {
                t.Start();
                foreach (ElementId id in uidoc.Selection.GetElementIds())
                {
                    Element e = doc.GetElement(id);
                    e.LookupParameter("GC_CarbonScore")?.Set(data.GwpValue);
                    e.LookupParameter("GC_Supplier")?.Set(data.SupplierName);
                }
                t.Commit();
            }
        }
        
        public static MaterialBrowserCmd Instance => _instance;
    }
}
