using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using ExcelDataReader;

public class ExcelToQuizPackWindow : EditorWindow
{
    public string excelFileName = "QuizData.xlsx";
    public string outputFolder = "Assets/QuizPacks/";
    public string quizPackName = "Job Fraud Quiz Pack";

    [MenuItem("Shivance Tools/Excel → QuizPack Generator")]
    public static void ShowWindow()
    {
        GetWindow<ExcelToQuizPackWindow>("Excel → QuizPack");
    }

    private void OnGUI()
    {
        GUILayout.Label("Excel to QuizPack Converter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool reads an Excel file and converts it into a QuizPack ScriptableObject.", MessageType.Info);

        excelFileName = EditorGUILayout.TextField("Excel File Name", excelFileName);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        quizPackName = EditorGUILayout.TextField("Quiz Pack Name", quizPackName);

        if (GUILayout.Button("Generate QuizPack"))
        {
            GenerateQuizPack();
        }
    }

    private void GenerateQuizPack()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, excelFileName);

        if (!File.Exists(filePath))
        {
            EditorUtility.DisplayDialog("Error", $"Excel file not found:\n{filePath}", "OK");
            return;
        }

        try
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                DataSet result = reader.AsDataSet();
                DataTable table = result.Tables[0];

                QuizPackSO quizPack = ScriptableObject.CreateInstance<QuizPackSO>();
                quizPack.questions = new List<QuizQuestionData>();

                for (int i = 1; i < table.Rows.Count; i++)
                {
                    DataRow row = table.Rows[i];
                    QuizQuestionData question = new QuizQuestionData();

                    question.question = row[2]?.ToString();

                    question.options = new string[4];
                    question.options[0] = row[3]?.ToString();
                    question.options[1] = row[4]?.ToString();
                    question.options[2] = row[5]?.ToString();
                    question.options[3] = row[6]?.ToString();

                    string correctAnswer = row[7]?.ToString();
                    question.correctAnswerIndex = Array.IndexOf(question.options, correctAnswer);
                    if (question.correctAnswerIndex == -1) question.correctAnswerIndex = 0;

                    Enum.TryParse(row[8]?.ToString(), true, out QuestionsDifficulty difficulty);
                    question.questionsDifficulty = difficulty;

                    float.TryParse(row[9]?.ToString(), out float timeLimit);
                    question.timeLimit = timeLimit;

                    bool.TryParse(row[10]?.ToString(), out bool hint);
                    question.isHintAllowed = hint;

                    Enum.TryParse(row[11]?.ToString(), true, out CharacterGender gender);
                    string charName = row[12]?.ToString();
                    string charInfo = row[13]?.ToString();

                    question.characterData = new CharacterData
                    {
                        characterGender = gender,
                        characterName = string.IsNullOrWhiteSpace(charName) ? "Unknown" : charName,
                        characterInfo = string.IsNullOrWhiteSpace(charInfo) ? "Unknown" : charInfo
                    };

                    quizPack.questions.Add(question);
                }

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                string savePath = Path.Combine(outputFolder, $"{quizPackName}.asset");
                AssetDatabase.CreateAsset(quizPack, savePath);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog("✅ Success", $"QuizPack created at:\n{savePath}", "OK");
                Debug.Log($"✅ QuizPackSO created successfully at: {savePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading Excel file: {ex.Message}");
        }
    }
}
