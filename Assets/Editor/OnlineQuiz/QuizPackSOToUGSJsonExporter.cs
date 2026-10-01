using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class QuizPackSOToUGSJsonExporter : EditorWindow
{
    private string outputFolder = "Assets/OnlineQuizExport";
    private string defaultCategoryName = "General";
    private string version = "1.0";
    private bool markExportedPacksActive = true;

    [MenuItem("Shivance Tools/QuizPackSO To UGS JSON Exporter")]
    public static void ShowWindow()
    {
        GetWindow<QuizPackSOToUGSJsonExporter>("QuizPack UGS Exporter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Export selected QuizPackSO assets", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        defaultCategoryName = EditorGUILayout.TextField("Default Category", defaultCategoryName);
        version = EditorGUILayout.TextField("Version", version);
        markExportedPacksActive = EditorGUILayout.Toggle("Active In Manifest", markExportedPacksActive);

        if (GUILayout.Button("Export Selected QuizPacks"))
        {
            ExportSelectedQuizPacks();
        }
    }

    private void ExportSelectedQuizPacks()
    {
        QuizPackSO[] selectedPacks = Selection.GetFiltered<QuizPackSO>(SelectionMode.Assets);

        if (selectedPacks == null || selectedPacks.Length == 0)
        {
            EditorUtility.DisplayDialog("No QuizPacks Selected", "Select one or more QuizPackSO assets in the Project window.", "OK");
            return;
        }

        Directory.CreateDirectory(outputFolder);

        var manifest = new QuizPackManifestDTO
        {
            schemaVersion = "1.0",
            updatedAtIso = DateTime.UtcNow.ToString("o"),
            packs = new List<QuizPackManifestEntryDTO>()
        };

        for (int i = 0; i < selectedPacks.Length; i++)
        {
            QuizPackSO pack = selectedPacks[i];
            if (pack == null) continue;

            QuizPackOnlineDTO dto = QuizPackOnlineConverter.FromScriptableObject(pack);
            dto.categoryName = defaultCategoryName;
            dto.version = version;
            dto.updatedAtIso = manifest.updatedAtIso;

            string packId = string.IsNullOrWhiteSpace(dto.packId)
                ? QuizPackOnlineConverter.GeneratePackId(pack.name)
                : dto.packId;
            dto.packId = packId;

            string fullPackJson = QuizPackOnlineConverter.ToJson(dto);
            string fullPackFileName = $"cyberladder_quiz_pack_{packId}.json";
            File.WriteAllText(Path.Combine(outputFolder, fullPackFileName), fullPackJson);

            QuizPackManifestEntryDTO entry = QuizPackOnlineConverter.CreateManifestEntry(dto);
            entry.isActive = markExportedPacksActive;
            entry.sortOrder = i;
            manifest.packs.Add(entry);
        }

        string manifestJson = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(Path.Combine(outputFolder, "cyberladder_quiz_manifest.json"), manifestJson);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Export Complete", $"Exported {manifest.packs.Count} quiz pack(s) to:\n{outputFolder}", "OK");
    }
}
