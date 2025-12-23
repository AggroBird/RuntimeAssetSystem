using UnityEditor;

namespace AggroBird.RuntimeAssetSystem.Editor
{
    [CustomEditor(typeof(RuntimeAssetDatabase))]
    public class RuntimeAssetDatabaseInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    serializedObject.Update();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("data"));
                }
            }
        }
    }
}
