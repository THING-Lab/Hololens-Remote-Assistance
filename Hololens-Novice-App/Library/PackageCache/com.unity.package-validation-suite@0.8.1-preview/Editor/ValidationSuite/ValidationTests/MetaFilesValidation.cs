using System;
using System.IO;
using System.Linq;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class MetaFilesValidation : BaseValidation
    {
        public MetaFilesValidation()
        {
            TestName = "Meta Files Validation";
            TestDescription = "Validate that metafiles are present for all package files.";
            TestCategory = TestCategory.ContentScan;
            SupportedValidations = new[] { ValidationType.CI, ValidationType.LocalDevelopment, ValidationType.LocalDevelopmentInternal, ValidationType.Publishing, ValidationType.VerifiedSet };
        }

        bool ShouldIgnore(string name)
        {
            //Names starting with a "." are being ignored by AssetDB.
            //Names finishing with ".meta" are considered meta files in Editor Code.
            if (Path.GetFileName(name).StartsWith(".") || name.EndsWith(".meta"))
                return true;

            // Honor the Unity tilde skipping of import
            if (Path.GetDirectoryName(name).EndsWith("~") || name.EndsWith("~"))
                return true;

            // Ignore node_modules folder as it is created inside the tested directory when production dependencies exist
            if (Path.GetDirectoryName(name).EndsWith("node_modules") || name.Contains("node_modules"))
                return true;

            return false;
        }

        void CheckMeta(string toCheck)
        {
            if (ShouldIgnore(toCheck))
                return;

            if (System.IO.File.Exists(toCheck + ".meta"))
                return;

            AddError("Did not find meta file for " + toCheck);
        }

        void CheckMetaInFolderRecursively(string folder)
        {
            try
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    CheckMeta(file);
                }

                foreach (string dir in Directory.GetDirectories(folder))
                {
                    if (ShouldIgnore(dir))
                        continue;

                    CheckMeta(dir);
                    CheckMetaInFolderRecursively(dir);
                }
            }
            catch (Exception e)
            {
                AddError("Exception " + e.Message);
            }
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            //check if each file/folder has its .meta counter-part
            CheckMetaInFolderRecursively(Context.PublishPackageInfo.path);
        }
    }
}
