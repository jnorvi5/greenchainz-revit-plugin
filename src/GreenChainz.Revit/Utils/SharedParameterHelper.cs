using System;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace GreenChainz.Revit.Utils
{
    public static class SharedParameterHelper
    {
        private const string GC_AGENT_TAG = "GC_AgentTag";
        private const string SHARED_PARAM_GROUP = "GreenChainzData";

        public static void CreateSharedParameters(Document doc, Application app)
        {
            DefinitionFile sharedParamFile = app.OpenSharedParameterFile();
            if (sharedParamFile == null)
            {
                string path = Path.Combine(Path.GetTempPath(), "GreenChainzSharedParams.txt");
                if (!File.Exists(path)) File.WriteAllText(path, "");
                app.SharedParametersFilename = path;
                sharedParamFile = app.OpenSharedParameterFile();
            }

            DefinitionGroup group = sharedParamFile.Groups.get_Item("GreenChainzData");
            if (group == null)
            {
                group = sharedParamFile.Groups.Create("GreenChainzData");
            }

            CategorySet catSet = app.Create.NewCategorySet();
            Category materialCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Materials);
            catSet.Insert(materialCat);

            CreateAndBindParam(doc, app, group, "GC_CarbonScore", SpecTypeId.Number, catSet);
            CreateAndBindParam(doc, app, group, "GC_Supplier", SpecTypeId.String.Text, catSet);
            CreateAndBindParam(doc, app, group, "GC_Certifications", SpecTypeId.String.Text, catSet);
        }

        /// <summary>
        /// Ensures GC_AgentTag parameter exists for all model categories.
        /// Called on document open to support AI Agent tagging workflow.
        /// </summary>
        public static void EnsureGcAgentTag(Application app, Document doc)
        {
            if (doc == null || doc.IsFamilyDocument)
                return;

            // Check if parameter already exists
            if (HasProjectParameter(doc, GC_AGENT_TAG))
                return;

            // Backup current shared parameter file
            string originalFile = app.SharedParametersFilename;

            try
            {
                // Create/use temp shared parameter file
                string tempFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "GreenChainz_SharedParameters.txt");

                if (!File.Exists(tempFilePath))
                {
                    File.WriteAllText(tempFilePath, string.Empty);
                }

                app.SharedParametersFilename = tempFilePath;

                DefinitionFile defFile = app.OpenSharedParameterFile();
                if (defFile == null)
                    return;

                DefinitionGroup group = defFile.Groups.get_Item(SHARED_PARAM_GROUP) ??
                                        defFile.Groups.Create(SHARED_PARAM_GROUP);

                ExternalDefinition externalDef = group.Definitions.get_Item(GC_AGENT_TAG) as ExternalDefinition;
                if (externalDef == null)
                {
                    ExternalDefinitionCreationOptions options =
                        new ExternalDefinitionCreationOptions(GC_AGENT_TAG, SpecTypeId.String.Text)
                        {
                            Visible = true,
                            Description = "GreenChainz AI Agent carbon impact tag"
                        };

                    externalDef = group.Definitions.Create(options) as ExternalDefinition;
                }

                // Build category set for all model categories that allow bound parameters
                CategorySet catSet = app.Create.NewCategorySet();

                foreach (Category cat in doc.Settings.Categories)
                {
                    if (cat == null) continue;
                    if (!cat.AllowsBoundParameters) continue;

                    // Include model categories (walls, floors, roofs, etc.)
                    if (cat.CategoryType == CategoryType.Model)
                    {
                        catSet.Insert(cat);
                    }
                }

                if (catSet.Size == 0)
                    return;

                using (Transaction tx = new Transaction(doc, "Add GC_AgentTag parameter"))
                {
                    tx.Start();

                    InstanceBinding binding = app.Create.NewInstanceBinding(catSet);
                    BindingMap map = doc.ParameterBindings;

                    map.Insert(externalDef, binding, GroupTypeId.GreenBuilding);

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GreenChainz: Failed to create GC_AgentTag parameter: {ex.Message}");
            }
            finally
            {
                // Restore original shared parameter file path
                if (!string.IsNullOrEmpty(originalFile))
                {
                    app.SharedParametersFilename = originalFile;
                }
            }
        }

        /// <summary>
        /// Checks if a project parameter with the given name already exists.
        /// </summary>
        private static bool HasProjectParameter(Document doc, string paramName)
        {
            BindingMap map = doc.ParameterBindings;
            DefinitionBindingMapIterator it = map.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                Definition def = it.Key;
                if (def != null && def.Name == paramName)
                    return true;
            }
            return false;
        }

        private static void CreateAndBindParam(Document doc, Application app, DefinitionGroup group, string paramName, ForgeTypeId paramType, CategorySet catSet)
        {
            BindingMap bindingMap = doc.ParameterBindings;

            Definition definition = group.Definitions.get_Item(paramName);
            if (definition == null)
            {
                ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(paramName, paramType);
                definition = group.Definitions.Create(options);
            }

            InstanceBinding binding = app.Create.NewInstanceBinding(catSet);

            if (!bindingMap.Contains(definition))
            {
                bindingMap.Insert(definition, binding, GroupTypeId.GreenBuilding);
            }
        }
    }
}
