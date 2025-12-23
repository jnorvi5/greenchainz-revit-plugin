using System;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

namespace GreenChainz.Revit.Utils
{
    public static class SharedParameterHelper
    {
        // Define the parameters we want to inject into the Architect's model
        public static void CreateSharedParameters(Document doc, Application app)
        {
            // 1. Ensure shared parameter file exists
            DefinitionFile sharedParamFile = app.OpenSharedParameterFile();
            if (sharedParamFile == null)
            {
                string path = Path.Combine(Path.GetTempPath(), "GreenChainzSharedParams.txt");
                if (!File.Exists(path)) File.WriteAllText(path, "");
                app.SharedParametersFilename = path;
                sharedParamFile = app.OpenSharedParameterFile();
            }

            // 2. Create Group
            DefinitionGroup group = sharedParamFile.Groups.get_Item("GreenChainzData");
            if (group == null)
            {
                group = sharedParamFile.Groups.Create("GreenChainzData");
            }

            // 3. Create & Bind Parameters (Trojan Horse Data)
            // We use built-in SpecTypeIds for Revit 2022+ compatibility
            CreateAndBindParam(doc, app, group, "GC_CarbonScore", SpecTypeId.Number); // GWP value
            CreateAndBindParam(doc, app, group, "GC_Supplier", SpecTypeId.String.Text); // Supplier Name
            CreateAndBindParam(doc, app, group, "GC_Certifications", SpecTypeId.String.Text); // EPD/FSC Links
        }

        private static void CreateAndBindParam(Document doc, Application app, DefinitionGroup group, string paramName, ForgeTypeId paramType)
        {
            BindingMap bindingMap = doc.ParameterBindings;

            // Get or Create Definition
            Definition definition = group.Definitions.get_Item(paramName);
            if (definition == null)
            {
                ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(paramName, paramType);
                definition = group.Definitions.Create(options);
            }

            // Bind to common categories (Walls, Floors, Roofs, etc.)
            CategorySet catSet = app.Create.NewCategorySet();
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Walls));
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Floors));
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Roofs));
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_StructuralColumns));
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_StructuralFraming));

            InstanceBinding binding = app.Create.NewInstanceBinding(catSet);

            // Insert binding if not exists
            if (!bindingMap.Contains(definition))
            {
                // PG_GREEN_BUILDING puts it in the "Green Building" section of the properties panel
                bindingMap.Insert(definition, binding, BuiltInParameterGroup.PG_GREEN_BUILDING);
            }
        }
    }
}
