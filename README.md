# Runtime Asset System

Lightweight asset database for Unity to load assets by GUID. Serves as a middle ground between loading assets from Resources.Load() and addressables.

Example:
```csharp
[CreateAssetMenu(menuName = "TestAsset")]
public class TestAsset : RuntimeAsset
{
    // Some test asset
}


[SerializeField]
public TestAsset referenceToAsset;


public void Test()
{
    GUID guid = referenceToAsset.GetGUID();

    // Load directly by GUID
    TestAsset referenceToSameAsset = RuntimeAssetDatabase.LoadAsset<TestAsset>(guid);

    // Load all assets of particular type
    TestAsset[] arrayContainingSameAsset = RuntimeAssetDatabase.LoadAllAssetsOfType<TestAsset>();
}
```

Assets are stored by LazyLoadReference<>, so they only get loaded by Unity when a match is found.