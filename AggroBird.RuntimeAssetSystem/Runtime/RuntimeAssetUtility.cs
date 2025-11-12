using AggroBird.UnityExtend;
using UnityEngine;

namespace AggroBird.RuntimeAssetSystem
{
    public static class RuntimeAssetUtility
    {
        public static bool ValidateAsset(RuntimeAsset asset)
        {
            if (!asset)
            {
                return false;
            }

            if (asset.GetGUID() == GUID.zero)
            {
                Debug.LogError($"Attempted to reference an asset with an invalid GUID: {asset}", asset);
                return false;
            }

            return true;
        }
    }
}