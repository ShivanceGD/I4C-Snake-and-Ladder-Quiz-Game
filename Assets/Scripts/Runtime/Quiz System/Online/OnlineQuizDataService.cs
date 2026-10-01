using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class OnlineQuizDataService
{
    private readonly OnlineQuizConfigSO config;
    private readonly Dictionary<string, QuizPackSO> runtimePackCache = new();
    private readonly Dictionary<string, QuizPackOnlineDTO> dtoCache = new();
    private QuizPackManifestDTO manifestCache;

    public OnlineQuizDataService(OnlineQuizConfigSO config)
    {
        this.config = config;
    }

    public async Task<QuizPackManifestDTO> LoadManifestAsync(CancellationToken token = default)
    {
        OnlineQuizEvents.QuizManifestLoadStarted();

        try
        {
            token.ThrowIfCancellationRequested();
            await EnsureServicesReadyAsync(token);
            string json = await LoadJsonFromCustomDataAsync(config.manifestCustomId, token);
            manifestCache = JsonUtility.FromJson<QuizPackManifestDTO>(json);

            if (manifestCache == null)
            {
                throw new InvalidOperationException("Manifest JSON could not be parsed.");
            }

            if (manifestCache.packs == null)
            {
                manifestCache.packs = new List<QuizPackManifestEntryDTO>();
            }

            SaveCacheJson(config.manifestCustomId, json);
            OnlineQuizEvents.QuizManifestLoaded(manifestCache);
            return manifestCache;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            string message = $"Online quiz manifest load failed: {ex.Message}";
            Debug.LogWarning(message);
            OnlineQuizEvents.QuizManifestLoadFailed(message);

            if (config != null && config.useLocalJsonCache && TryLoadCachedJson(config.manifestCustomId, out string cachedJson))
            {
                try
                {
                    manifestCache = JsonUtility.FromJson<QuizPackManifestDTO>(cachedJson);
                    if (manifestCache?.packs != null)
                    {
                        Debug.LogWarning("[OnlineQuiz] Using cached manifest JSON.");
                        OnlineQuizEvents.QuizManifestLoaded(manifestCache);
                        return manifestCache;
                    }
                }
                catch (Exception cacheEx)
                {
                    Debug.LogWarning($"Cached online quiz manifest is invalid: {cacheEx.Message}");
                }
            }

            return null;
        }
    }

    public async Task<List<QuizPackManifestEntryDTO>> GetActiveQuizPackEntriesAsync(CancellationToken token = default)
    {
        QuizPackManifestDTO manifest = manifestCache ?? await LoadManifestAsync(token);
        var entries = new List<QuizPackManifestEntryDTO>();

        if (manifest?.packs == null) return entries;

        foreach (var entry in manifest.packs)
        {
            if (entry == null) continue;
            if (config != null && config.hideInactivePacksInGame && !entry.isActive) continue;
            entries.Add(entry);
        }

        return entries;
    }

    public async Task<QuizPackSO> LoadRuntimeQuizPackAsync(string packId, CancellationToken token = default)
    {
        if (TryGetCachedRuntimeQuizPack(packId, out QuizPackSO cachedPack)) return cachedPack;

        OnlineQuizEvents.QuizPackLoadStarted(packId);
        QuizPackOnlineDTO dto = await LoadQuizPackDTOAsync(packId, token);

        if (dto == null)
        {
            OnlineQuizEvents.QuizPackLoadFailed(packId, "Quiz pack DTO was empty.");
            return null;
        }

        try
        {
            QuizPackSO runtimePack = QuizPackOnlineConverter.ToRuntimeScriptableObject(dto);
            if (runtimePack.questions == null || runtimePack.questions.Count == 0)
            {
                throw new InvalidOperationException("Quiz pack has no questions.");
            }

            runtimePackCache[packId] = runtimePack;
            OnlineQuizEvents.QuizPackLoaded(packId, runtimePack);
            return runtimePack;
        }
        catch (Exception ex)
        {
            string message = $"Quiz pack '{packId}' could not map to QuizPackSO: {ex.Message}";
            Debug.LogWarning(message);
            OnlineQuizEvents.QuizPackLoadFailed(packId, message);
            return null;
        }
    }

    public async Task<QuizPackOnlineDTO> LoadQuizPackDTOAsync(string packId, CancellationToken token = default)
    {
        if (TryGetCachedDTO(packId, out QuizPackOnlineDTO cachedDto)) return cachedDto;
        if (string.IsNullOrWhiteSpace(packId)) return null;

        string key = GetPackKey(packId);

        try
        {
            token.ThrowIfCancellationRequested();
            await EnsureServicesReadyAsync(token);
            string json = await LoadJsonFromCustomDataAsync(key, token);
            QuizPackOnlineDTO dto = QuizPackOnlineConverter.FromJson(json);

            if (dto == null)
            {
                throw new InvalidOperationException("Pack JSON could not be parsed.");
            }

            if (string.IsNullOrWhiteSpace(dto.packId)) dto.packId = packId;
            dtoCache[packId] = dto;
            SaveCacheJson(key, json);
            return dto;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            string message = $"Online quiz pack '{packId}' load failed: {ex.Message}";
            Debug.LogWarning(message);
            OnlineQuizEvents.QuizPackLoadFailed(packId, message);

            if (config != null && config.useLocalJsonCache && TryLoadCachedJson(key, out string cachedJson))
            {
                try
                {
                    QuizPackOnlineDTO dto = QuizPackOnlineConverter.FromJson(cachedJson);
                    if (dto != null)
                    {
                        if (string.IsNullOrWhiteSpace(dto.packId)) dto.packId = packId;
                        dtoCache[packId] = dto;
                        Debug.LogWarning($"[OnlineQuiz] Using cached JSON for quiz pack '{packId}'.");
                        return dto;
                    }
                }
                catch (Exception cacheEx)
                {
                    Debug.LogWarning($"Cached online quiz pack '{packId}' is invalid: {cacheEx.Message}");
                }
            }

            return null;
        }
    }

    public async Task RefreshManifestAsync(CancellationToken token = default)
    {
        manifestCache = null;
        await LoadManifestAsync(token);
    }

    public bool TryGetCachedRuntimeQuizPack(string packId, out QuizPackSO pack)
    {
        pack = null;
        return !string.IsNullOrWhiteSpace(packId) && runtimePackCache.TryGetValue(packId, out pack);
    }

    public bool TryGetCachedDTO(string packId, out QuizPackOnlineDTO dto)
    {
        dto = null;
        return !string.IsNullOrWhiteSpace(packId) && dtoCache.TryGetValue(packId, out dto);
    }

    private async Task EnsureServicesReadyAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        token.ThrowIfCancellationRequested();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private async Task<string> LoadJsonFromCustomDataAsync(string key, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var keys = new HashSet<string> { key };
        var result = await CloudSaveService.Instance.Data.Custom.LoadAsync(config.quizCatalogCustomDataId, keys);

        if (!result.TryGetValue(key, out var item))
        {
            throw new KeyNotFoundException($"Custom data key '{key}' was not found in '{config.quizCatalogCustomDataId}'.");
        }

        string json = item.Value.GetAsString();
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException($"Custom data key '{key}' contained no JSON.");
        }

        return json;
    }

    private string GetPackKey(string packId)
    {
        return $"{config.quizPackCustomIdPrefix}{packId}";
    }

    private string CacheFolder
    {
        get
        {
            string folderName = config != null && !string.IsNullOrWhiteSpace(config.localCacheFolderName)
                ? config.localCacheFolderName
                : "OnlineQuizCache";
            return Path.Combine(Application.persistentDataPath, folderName);
        }
    }

    private void SaveCacheJson(string key, string json)
    {
        if (config == null || !config.useLocalJsonCache || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(json)) return;

        try
        {
            Directory.CreateDirectory(CacheFolder);
            File.WriteAllText(Path.Combine(CacheFolder, $"{SanitizeFileName(key)}.json"), json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OnlineQuiz] Failed to cache '{key}': {ex.Message}");
        }
    }

    private bool TryLoadCachedJson(string key, out string json)
    {
        json = null;

        try
        {
            string path = Path.Combine(CacheFolder, $"{SanitizeFileName(key)}.json");
            if (!File.Exists(path)) return false;
            json = File.ReadAllText(path);
            return !string.IsNullOrWhiteSpace(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OnlineQuiz] Failed to read cache '{key}': {ex.Message}");
            return false;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value;
    }
}
