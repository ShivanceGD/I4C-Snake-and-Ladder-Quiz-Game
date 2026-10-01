using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class OnlineQuizRepository
{
    private readonly OnlineQuizConfigSO config;
    private readonly OnlineQuizDataService dataService;
    private readonly List<QuizPackSO> localFallbackPacks = new();

    public OnlineQuizRepository(OnlineQuizConfigSO config, IEnumerable<QuizPackSO> localFallbackPacks)
    {
        this.config = config;
        dataService = new OnlineQuizDataService(config);
        SetLocalFallbackPacks(localFallbackPacks);
    }

    public void SetLocalFallbackPacks(IEnumerable<QuizPackSO> packs)
    {
        localFallbackPacks.Clear();
        if (packs == null) return;

        foreach (var pack in packs)
        {
            if (pack != null) localFallbackPacks.Add(pack);
        }
    }

    public async Task<List<QuizPackManifestEntryDTO>> GetSortedActiveEntriesAsync(CancellationToken token = default)
    {
        if (config == null || !config.useOnlineQuizSystem) return new List<QuizPackManifestEntryDTO>();

        var entries = await dataService.GetActiveQuizPackEntriesAsync(token);
        return entries
            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.packId))
            .OrderBy(e => string.IsNullOrWhiteSpace(e.categoryName) ? config.defaultCategoryName : e.categoryName)
            .ThenBy(e => e.sortOrder)
            .ThenBy(e => e.displayName)
            .ToList();
    }

    public Dictionary<string, List<QuizPackManifestEntryDTO>> GroupByCategory(IEnumerable<QuizPackManifestEntryDTO> entries)
    {
        string defaultCategory = config != null && !string.IsNullOrWhiteSpace(config.defaultCategoryName)
            ? config.defaultCategoryName
            : "General";

        return entries?
            .Where(e => e != null)
            .GroupBy(e => string.IsNullOrWhiteSpace(e.categoryName) ? defaultCategory : e.categoryName)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.sortOrder).ThenBy(e => e.displayName).ToList())
            ?? new Dictionary<string, List<QuizPackManifestEntryDTO>>();
    }

    public async Task<QuizPackSO> LoadRuntimeQuizPackAsync(string packId, CancellationToken token = default)
    {
        if (config == null || !config.useOnlineQuizSystem) return null;
        return await dataService.LoadRuntimeQuizPackAsync(packId, token);
    }

    public async Task<QuizPackOnlineDTO> LoadQuizPackDTOAsync(string packId, CancellationToken token = default)
    {
        if (config == null || !config.useOnlineQuizSystem) return null;
        return await dataService.LoadQuizPackDTOAsync(packId, token);
    }

    public bool TryGetCachedRuntimeQuizPack(string packId, out QuizPackSO pack)
    {
        return dataService.TryGetCachedRuntimeQuizPack(packId, out pack);
    }

    public IReadOnlyList<QuizPackSO> GetLocalFallbackPacks()
    {
        if (config == null || !config.useLocalQuizFallback) return Array.Empty<QuizPackSO>();
        OnlineQuizEvents.OnlineQuizFallbackUsed(localFallbackPacks);
        return localFallbackPacks;
    }

    public QuizPackSO FindLocalFallbackByName(string packName)
    {
        if (string.IsNullOrWhiteSpace(packName)) return null;
        return localFallbackPacks.FirstOrDefault(pack => pack != null && pack.name == packName);
    }
}
