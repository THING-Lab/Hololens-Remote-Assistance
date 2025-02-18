using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.XR.Management;


namespace UnityEditor.XR.Management
{
    public static class XRGeneralSettingsUpgrade
    {
        public static bool UpgradeSettingsToPerBuildTarget(string path)
        {
            var generalSettings = GetXRGeneralSettingsInstance(path);
            if (generalSettings == null)
                return false;

            if (!AssetDatabase.IsMainAsset(generalSettings))
                return false;

            XRGeneralSettings newSettings = ScriptableObject.CreateInstance<XRGeneralSettings>() as XRGeneralSettings;
            newSettings.Manager = generalSettings.Manager;
            generalSettings = null;

            AssetDatabase.DeleteAsset(path);

            XRGeneralSettingsPerBuildTarget buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>() as XRGeneralSettingsPerBuildTarget;            
            AssetDatabase.CreateAsset(buildTargetSettings, path);

            buildTargetSettings.SetSettingsForBuildTarget(EditorUserBuildSettings.selectedBuildTargetGroup, newSettings);
            AssetDatabase.AddObjectToAsset(newSettings, path);
            AssetDatabase.SaveAssets();

            Debug.LogWarningFormat("XR General Settings have been upgraded to be per-Build Target Group. Original settings were moved to Build Target Group {0}.", EditorUserBuildSettings.selectedBuildTargetGroup);
            return true;
        }

        private static XRGeneralSettings GetXRGeneralSettingsInstance(string pathToSettings)
        {
            XRGeneralSettings ret = null;
            if (pathToSettings.Length > 0)
            {
                ret = AssetDatabase.LoadAssetAtPath(pathToSettings, typeof(XRGeneralSettings)) as XRGeneralSettings;
            }

            return ret;
        }
    }

}
