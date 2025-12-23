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

            // Prepare CategorySet for OST_Materials
            CategorySet catSet = app.Create.NewCategorySet();
            Category materialCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Materials);
            catSet.Insert(materialCat);

            // Create Definitions and Bindings
            CreateAndBindParam(doc, app, group, "GC_CarbonScore", SpecTypeId.Number, catSet);
            CreateAndBindParam(doc, app, group, "GC_Supplier", SpecTypeId.String.Text, catSet);
            CreateAndBindParam(doc, app, group, "GC_Certifications", SpecTypeId.String.Text, catSet);
        }

        public static void CreateAndBindParam(Document doc, Application app, DefinitionGroup group, string paramName, ForgeTypeId paramType, CategorySet catSet)
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

            // 3. Create Instance Binding
            InstanceBinding binding = app.Create.NewInstanceBinding(catSet);

            // 4. Add Binding if not exists
            if (!bindingMap.Contains(definition))
            {
                bindingMap.Insert(definition, binding, BuiltInParameterGroup.PG_GREEN_BUILDING);
            }
            else
            {
                // If it exists, we might need to update the binding to include new categories
                // But re-inserting usually fails or requires re-insert.
                // For simplicity in this task, we check containment.
                // Advanced: Get existing binding, add categories, re-insert.
                // Assuming simple case where we don't change existing bindings of other categories drastically.

                // If we need to support adding categories to existing binding:
                // InstanceBinding existingBinding = bindingMap.get_Item(definition) as InstanceBinding;
                // if (existingBinding != null) {
                //    foreach (Category c in catSet) existingBinding.Categories.Insert(c);
                //    bindingMap.ReInsert(definition, existingBinding, BuiltInParameterGroup.PG_GREEN_BUILDING);
                // }
                // However, ReInsert returns bool.
                // For this task, we'll stick to basic insertion if missing.
                // But for "One-Click Specify", we might be adding a param to a category that didn't have it.
                // So let's try to update it.

                try
                {
                    InstanceBinding existingBinding = bindingMap.get_Item(definition) as InstanceBinding;
                    if (existingBinding != null)
                    {
                        // Check if all categories are present
                        bool modificationNeeded = false;
                        foreach (Category c in catSet)
                        {
                            if (!existingBinding.Categories.Contains(c))
                            {
                                existingBinding.Categories.Insert(c);
                                modificationNeeded = true;
                            }
                        }

                        if (modificationNeeded)
                        {
                             bindingMap.ReInsert(definition, existingBinding, BuiltInParameterGroup.PG_GREEN_BUILDING);
                        }
                    }
                }
                catch
                {
                    // Ignore errors during re-binding to prevent crash
                }
            }
        }
    }
}
