using System;
using System.Collections.Generic;
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

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("Building Runtime Asset Database");

            Dictionary<Type, SortedDictionary<GUID, RuntimeAsset>> perType = new();
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
                    if (!perType.TryGetValue(assetType, out var set))
                    {
                        perType[assetType] = set = new();
                    }
                    set.Add(assetGuid, asset);
                }
            }

            SortedDictionary<string, SortedDictionary<GUID, RuntimeAsset>> stringTable = new();
            foreach (var pair in perType)
            {
                stringTable[$"{pair.Key.FullName}, {pair.Key.Assembly.FullName}"] = pair.Value;
            }

            RuntimeAssetDatabase database = null;
            string databasePath = string.Empty;
            foreach (var assetGuidStr in AssetDatabase.FindAssets($"t:{typeof(RuntimeAssetDatabase).Name}"))
            {
                databasePath = AssetDatabase.GUIDToAssetPath(assetGuidStr);
                database = AssetDatabase.LoadAssetAtPath<RuntimeAssetDatabase>(databasePath);
                if (database)
                {
                    break;
                }
            }

            if (database)
            {
                WriteData(database, stringTable);
            }
            else
            {
                throw new BuildFailedException("Failed to find runtime asset database");
            }
            AssetDatabase.ImportAsset(databasePath);
        }
        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {

        }

        private void WriteData(RuntimeAssetDatabase database, SortedDictionary<string, SortedDictionary<GUID, RuntimeAsset>> data)
        {
            SerializedObject scriptableObject = new(database);
            database.data = new RuntimeAssetDatabase.TypeCollection[data.Count];
            int collectionIdx = 0;
            foreach (var value in data)
            {
                RuntimeAssetDatabase.TypeCollection collection = new()
                {
                    assets = new RuntimeAssetDatabase.TypeCollection.Asset[value.Value.Count]
                };
                int objIdx = 0;
                foreach (var obj in value.Value)
                {
                    collection.assets[objIdx++] = new()
                    {
                        guid = obj.Key,
                        asset = new(obj.Value),
                    };
                }
                collection.typeName = value.Key;
                database.data[collectionIdx++] = collection;
            }
            EditorUtility.SetDirty(database);
            scriptableObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}