using AggroBird.UnityExtend;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("AggroBird.RuntimeAssetSystem.Editor")]

namespace AggroBird.RuntimeAssetSystem
{
    public class RuntimeAssetDatabase : ScriptableObject
    {
        public const string ResourceName = "RuntimeAssetDatabase";

        private static RuntimeAssetDatabase instance = null;

        [Serializable]
        internal struct TypeCollection
        {
            [Serializable]
            internal struct Asset
            {
                public GUID guid;
                public LazyLoadReference<RuntimeAsset> asset;
            }

            public Asset[] assets;
            public string typeName;
        }

        [SerializeField]
        internal TypeCollection[] data;

        private struct AssetReference
        {
            public LazyLoadReference<RuntimeAsset> asset;
            public Type type;
        }

        private readonly Dictionary<GUID, AssetReference> guidLookup = new();
        private readonly Dictionary<Type, AssetReference[]> typeLookup = new();

        private static class AssetBuffer<T> where T : RuntimeAsset
        {
            private static readonly List<T> instance = new();

            public static List<T> GetInstance()
            {
                instance.Clear();
                return instance;
            }
        }


        private static Type GetRootType(Type type)
        {
            if (type == null)
            {
                throw new NullReferenceException("Type cannot be null");
            }

            if (type.Equals(typeof(RuntimeAsset)))
            {
                throw new ArgumentException("Provided type cannot be RuntimeAsset");
            }

            while (true)
            {
                if (type == null || type.Equals(typeof(object)))
                {
                    throw new ArgumentException("Provided type is not a RuntimeAsset");
                }

                if (type.BaseType.Equals(typeof(RuntimeAsset)))
                {
                    return type;
                }

                type = type.BaseType;
            }
        }

        private void BuildLookup()
        {
            Dictionary<Type, List<AssetReference>> buildTypeLookup = new();
            for (int i = 0; i < data.Length; i++)
            {
                Type assetType = Type.GetType(data[i].typeName);
                Type rootType = GetRootType(assetType);

                if (!buildTypeLookup.TryGetValue(rootType, out List<AssetReference> list))
                {
                    buildTypeLookup[rootType] = list = new List<AssetReference>();
                }

                foreach (var entry in data[i].assets)
                {
                    var assetReference = new AssetReference()
                    {
                        asset = entry.asset,
                        type = assetType,
                    };

                    guidLookup.Add(entry.guid, assetReference);
                    list.Add(assetReference);
                }
            }
            foreach (var pair in buildTypeLookup)
            {
                typeLookup.Add(pair.Key, pair.Value.ToArray());
            }
        }

        private static void EnsureInstance()
        {
            if (!instance)
            {
                instance = Resources.Load<RuntimeAssetDatabase>(ResourceName);
                if (!instance) throw new Exception("Failed to load runtime asset database");
                instance.BuildLookup();
            }
        }

        public static T LoadAsset<T>(GUID guid) where T : RuntimeAsset
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid.ToString());
                if (!string.IsNullOrEmpty(path) && UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAsset>(path) is T result && result.IncludeInDatabase)
                {
                    return result;
                }
            }
            else
#endif
            {
                EnsureInstance();

                if (instance.guidLookup.TryGetValue(guid, out var item) && typeof(T).IsAssignableFrom(item.type))
                {
                    return item.asset.asset as T;
                }
            }

            return null;
        }
        public static T[] LoadAllAssetsOfType<T>() where T : RuntimeAsset
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                if (guids.Length > 0)
                {
                    List<T> result = AssetBuffer<T>.GetInstance();
                    foreach (string guid in guids)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path) is T asset && asset.IncludeInDatabase)
                            {
                                result.Add(asset);
                            }
                        }
                    }
                    return result.ToArray();
                }
            }
            else
#endif
            {
                EnsureInstance();

                Type targetType = typeof(T);
                if (targetType.Equals(typeof(RuntimeAsset)))
                {
                    // Load all assets
                    List<T> result = AssetBuffer<T>.GetInstance();
                    foreach (var assetReference in instance.guidLookup.Values)
                    {
                        var asset = assetReference.asset.asset as T;
                        if (asset)
                        {
                            result.Add(asset);
                        }
                    }
                    return result.ToArray();
                }
                else
                {
                    // Filter on root type
                    if (instance.typeLookup.TryGetValue(GetRootType(targetType), out AssetReference[] assetReferences))
                    {
                        List<T> result = AssetBuffer<T>.GetInstance();
                        for (int i = 0; i < assetReferences.Length; i++)
                        {
                            var assetReference = assetReferences[i];
                            if (typeof(T).IsAssignableFrom(assetReference.type))
                            {
                                var asset = assetReference.asset.asset as T;
                                if (asset)
                                {
                                    result.Add(asset);
                                }
                            }
                        }
                        return result.ToArray();
                    }
                }
            }

            return Array.Empty<T>();
        }
        public static bool TryLoadAsset<T>(GUID guid, out T asset) where T : RuntimeAsset
        {
            asset = LoadAsset<T>(guid);
            return asset;
        }
    }
}