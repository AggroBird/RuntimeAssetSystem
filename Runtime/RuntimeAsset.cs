using AggroBird.UnityExtend;
using UnityEngine;

namespace AggroBird.RuntimeAssetSystem
{
    public abstract class RuntimeAsset : ScriptableObject
    {
        [Header("Runtime Asset")]
        [SerializeField, ReadOnly]
        private GUID guid;
        [SerializeField]
        private bool includeInDatabase = true;


        public GUID GetGUID()
        {
            return guid;
        }

        public bool HasValidGUID() => guid != GUID.zero;

        public bool IncludeInDatabase => includeInDatabase;


#if UNITY_EDITOR
        private void OnValidateDelayed()
        {
            UnityEditor.EditorApplication.delayCall -= OnValidateDelayed;
            if (this && UnityEditor.AssetDatabase.Contains(this))
            {
                if (!includeInDatabase)
                {
                    // Clear the guid if this is not a database asset
                    if (guid != GUID.zero)
                    {
                        guid = GUID.zero;
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                }
                else if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out string assetGuidStr, out long _))
                {
                    GUID assetGuid = new(assetGuidStr);
                    if (guid != assetGuid)
                    {
                        guid = assetGuid;
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                }
            }
        }
        protected virtual void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall -= OnValidateDelayed;
            UnityEditor.EditorApplication.delayCall += OnValidateDelayed;
        }
#endif
    }
}
