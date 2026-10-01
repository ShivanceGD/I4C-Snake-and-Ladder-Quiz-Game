using System;
using System.Collections.Generic;

[Serializable]
public class QuizPackManifestDTO
{
    public string schemaVersion = "1.0";
    public string updatedAtIso;
    public List<QuizPackManifestEntryDTO> packs = new();
}

[Serializable]
public class QuizPackManifestEntryDTO
{
    public string packId;
    public string displayName;
    public string categoryName;
    public string description;
    public string version;
    public bool isActive = true;
    public int sortOrder;
    public int questionCount;
    public int easyCount;
    public int mediumCount;
    public int hardCount;
    public string updatedAtIso;
}
