#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[InitializeOnLoad]
public static class FixUrpBuildSettings
{
    static FixUrpBuildSettings()
    {
        Apply();
    }

    [MenuItem("Tools/I4C/Fix URP Build Settings")]
    public static void Apply()
    {
        const string assetPath = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            SerializedProperty enableRenderGraph = serializedObject.FindProperty("m_EnableRenderGraph");
            if (enableRenderGraph != null)
            {
                enableRenderGraph.boolValue = true;
            }

            SerializedProperty assetVersion = serializedObject.FindProperty("m_AssetVersion");
            if (assetVersion != null)
            {
                assetVersion.intValue = 9;
            }

            SerializedProperty references = serializedObject.FindProperty("references.RefIds");
            if (references != null)
            {
                for (int i = 0; i < references.arraySize; i++)
                {
                    SerializedProperty item = references.GetArrayElementAtIndex(i);
                    SerializedProperty type = item.FindPropertyRelative("type.class");
                    if (type != null && type.stringValue == "RenderGraphSettings")
                    {
                        SerializedProperty compatibilityMode = item.FindPropertyRelative("data.m_EnableRenderCompatibilityMode");
                        if (compatibilityMode != null)
                        {
                            compatibilityMode.boolValue = false;
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        NamedBuildTarget android = NamedBuildTarget.Android;
        string symbols = PlayerSettings.GetScriptingDefineSymbols(android);
        if (!symbols.Contains("URP_COMPATIBILITY_MODE"))
        {
            symbols = string.IsNullOrEmpty(symbols) ? "URP_COMPATIBILITY_MODE" : symbols + ";URP_COMPATIBILITY_MODE";
            PlayerSettings.SetScriptingDefineSymbols(android, symbols);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fixed URP Android build settings.");
    }
}
#endif
