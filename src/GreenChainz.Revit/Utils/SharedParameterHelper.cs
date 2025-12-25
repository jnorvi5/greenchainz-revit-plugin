using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace GreenChainz.Revit.Utils
{
    public static class SharedParameterHelper
    {
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
