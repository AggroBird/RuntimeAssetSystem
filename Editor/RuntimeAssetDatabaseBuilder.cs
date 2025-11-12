using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using GUID = AggroBird.UnityExtend.GUID;

namespace AggroBird.RuntimeAssetSystem.Editor
{
    internal class DatabaseBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        int IOrderedCallback.callbackOrder => 0;

        private string ResourceFolder => "Assets/Resources";
        private string ResourcePath => $"{ResourceFolder}/{RuntimeAssetDatabase.ResourceName}.asset";

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("Building Runtime Asset Database");

            Dictionary<Type, Dictionary<GUID, RuntimeAsset>> data = new();
            foreach (var assetGuidStr in AssetDatabase.FindAssets($"t:{typeof(RuntimeAsset).Name}"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuidStr);
                RuntimeAsset asset = AssetDatabase.LoadAssetAtPath<RuntimeAsset>(assetPath);
                if (asset.IncludeInDatabase)
                {
                    Type assetType = asset.GetType();
                    GUID assetGuid = new(assetGuidStr);
                    if (assetGuid != asset.GetGUID())
                    {
                        throw new BuildFailedException($"Asset '<a href=\"{assetPath}\">{asset.name}</a>' contains an invalid guid");
                    }
                    if (!data.TryGetValue(assetType, out var set))
                    {
                        data[assetType] = set = new();
                    }
                    set.Add(assetGuid, asset);
                }
            }

            string resourceFolder = ResourceFolder;
            if (!Directory.Exists(resourceFolder)) Directory.CreateDirectory(resourceFolder);
            string resourcePath = ResourcePath;
            if (File.Exists(resourcePath))
            {
                RuntimeAssetDatabase database = AssetDatabase.LoadAssetAtPath<RuntimeAssetDatabase>(resourcePath);
                WriteData(database, data);
            }
            else
            {
                RuntimeAssetDatabase database = ScriptableObject.CreateInstance<RuntimeAssetDatabase>();
                WriteData(database, data);
                AssetDatabase.CreateAsset(database, resourcePath);
            }
            AssetDatabase.ImportAsset(resourcePath);
        }
        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {

        }

        private void WriteData(RuntimeAssetDatabase database, Dictionary<Type, Dictionary<GUID, RuntimeAsset>> data)
        {
            SerializedObject scriptableObject = new(database);
            database.data = new RuntimeAssetDatabase.TypeCollection[data.Count];
            int collectionIdx = 0;
            foreach (var value in data)
            {
                RuntimeAssetDatabase.TypeCollection collection = new();
                collection.assets = new RuntimeAssetDatabase.TypeCollection.Asset[value.Value.Count];
                int objIdx = 0;
                foreach (var obj in value.Value)
                {
                    collection.assets[objIdx++] = new()
                    {
                        guid = obj.Key,
                        asset = new(obj.Value),
                    };
                }
                collection.typeName = $"{value.Key.FullName}, {value.Key.Assembly.FullName}";
                database.data[collectionIdx++] = collection;
            }
            EditorUtility.SetDirty(database);
            scriptableObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}