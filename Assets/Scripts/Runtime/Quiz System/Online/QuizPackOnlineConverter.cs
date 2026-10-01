using System;
using System.Collections.Generic;
using UnityEngine;

public static class QuizPackOnlineConverter
{
    public static QuizPackOnlineDTO FromScriptableObject(QuizPackSO source)
    {
        var dto = new QuizPackOnlineDTO
        {
            packId = GeneratePackId(source != null ? source.name : string.Empty),
            displayName = source != null ? source.name : string.Empty,
            updatedAtIso = DateTime.UtcNow.ToString("o"),
            questions = new List<QuestionOnlineDTO>()
        };

        if (source?.questions == null) return dto;

        foreach (var question in source.questions)
        {
            dto.questions.Add(FromQuestion(question));
        }

        return dto;
    }

    public static QuizPackSO ToRuntimeScriptableObject(QuizPackOnlineDTO dto)
    {
        var runtimePack = ScriptableObject.CreateInstance<QuizPackSO>();
        ApplyToScriptableObject(dto, runtimePack);
        return runtimePack;
    }

    public static void ApplyToScriptableObject(QuizPackOnlineDTO dto, QuizPackSO target)
    {
        if (target == null) return;

        target.name = !string.IsNullOrWhiteSpace(dto?.displayName)
            ? dto.displayName
            : !string.IsNullOrWhiteSpace(dto?.packId) ? dto.packId : "RuntimeOnlineQuizPack";
        target.questions = new List<QuizQuestionData>();

        if (dto?.questions == null) return;

        foreach (var question in dto.questions)
        {
            target.questions.Add(ToQuestion(question));
        }
    }

    public static string ToJson(QuizPackOnlineDTO dto)
    {
        return JsonUtility.ToJson(dto, true);
    }

    public static QuizPackOnlineDTO FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonUtility.FromJson<QuizPackOnlineDTO>(json);
    }

    public static QuizPackManifestEntryDTO CreateManifestEntry(QuizPackOnlineDTO dto)
    {
        var entry = new QuizPackManifestEntryDTO
        {
            packId = !string.IsNullOrWhiteSpace(dto?.packId) ? dto.packId : GeneratePackId(dto?.displayName),
            displayName = !string.IsNullOrWhiteSpace(dto?.displayName) ? dto.displayName : dto?.packId,
            categoryName = !string.IsNullOrWhiteSpace(dto?.categoryName) ? dto.categoryName : "General",
            description = dto?.description ?? string.Empty,
            version = !string.IsNullOrWhiteSpace(dto?.version) ? dto.version : "1.0",
            isActive = true,
            updatedAtIso = !string.IsNullOrWhiteSpace(dto?.updatedAtIso) ? dto.updatedAtIso : DateTime.UtcNow.ToString("o")
        };

        if (dto?.questions == null) return entry;

        entry.questionCount = dto.questions.Count;
        foreach (var question in dto.questions)
        {
            switch (question.questionsDifficulty)
            {
                case QuestionsDifficulty.Easy:
                    entry.easyCount++;
                    break;
                case QuestionsDifficulty.Medium:
                    entry.mediumCount++;
                    break;
                case QuestionsDifficulty.Hard:
                    entry.hardCount++;
                    break;
            }
        }

        return entry;
    }

    public static string GeneratePackId(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName)) return "quiz_pack";

        var chars = new List<char>();
        foreach (char c in sourceName.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) chars.Add(c);
            else if (chars.Count > 0 && chars[^1] != '_') chars.Add('_');
        }

        while (chars.Count > 0 && chars[^1] == '_') chars.RemoveAt(chars.Count - 1);
        return chars.Count == 0 ? "quiz_pack" : new string(chars.ToArray());
    }

    private static QuestionOnlineDTO FromQuestion(QuizQuestionData source)
    {
        return new QuestionOnlineDTO
        {
            question = source?.question ?? string.Empty,
            options = NormalizeOptions(source?.options),
            correctAnswerIndex = source != null ? source.correctAnswerIndex : 0,
            questionsDifficulty = source != null ? source.questionsDifficulty : QuestionsDifficulty.Easy,
            isHintAllowed = source != null && source.isHintAllowed,
            timeLimit = source != null ? source.timeLimit : 0f,
            characterData = FromCharacter(source?.characterData)
        };
    }

    private static QuizQuestionData ToQuestion(QuestionOnlineDTO source)
    {
        return new QuizQuestionData
        {
            question = source?.question ?? string.Empty,
            options = NormalizeOptions(source?.options),
            correctAnswerIndex = Mathf.Clamp(source?.correctAnswerIndex ?? 0, 0, 3),
            questionsDifficulty = source?.questionsDifficulty ?? QuestionsDifficulty.Easy,
            isHintAllowed = source != null && source.isHintAllowed,
            timeLimit = source != null && source.timeLimit > 0f ? source.timeLimit : 30f,
            characterData = ToCharacter(source?.characterData)
        };
    }

    private static CharacterOnlineDTO FromCharacter(CharacterData source)
    {
        return new CharacterOnlineDTO
        {
            characterName = source?.characterName ?? string.Empty,
            characterInfo = source?.characterInfo ?? string.Empty,
            characterGender = source?.characterGender ?? CharacterGender.Anonymous
        };
    }

    private static CharacterData ToCharacter(CharacterOnlineDTO source)
    {
        return new CharacterData
        {
            characterName = source?.characterName ?? string.Empty,
            characterInfo = source?.characterInfo ?? string.Empty,
            characterGender = source?.characterGender ?? CharacterGender.Anonymous
        };
    }

    private static string[] NormalizeOptions(string[] options)
    {
        var normalized = new string[4];
        for (int i = 0; i < normalized.Length; i++)
        {
            normalized[i] = options != null && i < options.Length ? options[i] ?? string.Empty : string.Empty;
        }

        return normalized;
    }
}
