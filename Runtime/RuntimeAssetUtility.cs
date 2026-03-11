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

            if (!asset.IncludeInDatabase)
            {
                Debug.LogError($"Attempted to reference a runtime asset that was excluded from the database: {asset}", asset);
                return false;
            }

            if (!asset.IncludeInDatabase || !asset.HasValidGUID())
            {
                Debug.LogError($"Attempted to reference a runtime asset with an invalid GUID: {asset}", asset);
                return false;
            }

            return true;
        }
    }
}
