using System.IO;

namespace GreenChainz.Revit.Utils
{
    public static class SharedParameterHelper
    {
        public static void CreateSharedParameters(Document doc, Application app)
        {
            // Ensure shared parameter file exists
            DefinitionFile sharedParamFile = app.OpenSharedParameterFile();
            if (sharedParamFile == null)
            {
                // Create a temporary one if not set
                string path = Path.Combine(Path.GetTempPath(), "GreenChainzSharedParams.txt");
                File.WriteAllText(path, ""); // Empty file
                app.SharedParametersFilename = path;
                sharedParamFile = app.OpenSharedParameterFile();
            }

            // Create Group
            DefinitionGroup group = sharedParamFile.Groups.get_Item("GreenChainzData");
            if (group == null)
            {
                group = sharedParamFile.Groups.Create("GreenChainzData");
            }

            // Create Definitions and Bindings
            CreateAndBindParam(doc, app, group, "GC_CarbonScore", SpecTypeId.Number);
            CreateAndBindParam(doc, app, group, "GC_Supplier", SpecTypeId.String.Text); // Updated for 2024+ API syntax (approx) or use built-in types
            CreateAndBindParam(doc, app, group, "GC_Certifications", SpecTypeId.String.Text);
        }

        private static void CreateAndBindParam(Document doc, Application app, DefinitionGroup group, string paramName, ForgeTypeId paramType)
        {
            // 1. Check if parameter already exists in project
            // This is a simplified check; reliable check requires iterating BindingMap
            BindingMap bindingMap = doc.ParameterBindings;

            // 2. Get Definition
            Definition definition = group.Definitions.get_Item(paramName);
            if (definition == null)
            {
                ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(paramName, paramType);
                definition = group.Definitions.Create(options);
            }

            // 3. Bind to Materials Category
            CategorySet catSet = app.Create.NewCategorySet();
            Category materialCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Materials);
            catSet.Insert(materialCat);

            InstanceBinding binding = app.Create.NewInstanceBinding(catSet);

            // 4. Add Binding if not exists
            // Note: In a real robust app, we carefully check if it's already bound to avoid errors
            if (!bindingMap.Contains(definition))
            {
                bindingMap.Insert(definition, binding, BuiltInParameterGroup.PG_GREEN_BUILDING);
            }
        }
    }
}
