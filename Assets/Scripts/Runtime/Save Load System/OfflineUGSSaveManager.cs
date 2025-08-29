using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;

public static class OfflineUGSSaveManager
{
    private const string LocalCacheFile = "localSaveCache.json";
    private static string GetLocalPath() => System.IO.Path.Combine(Application.persistentDataPath, LocalCacheFile);

    public static async Task SaveAsync<T>(string key, T value)
    {
        var data = new Dictionary<string, object> { { key, value } };
        SaveToLocalCache(data);

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log($"[UGS] Saved {key} to cloud.");
        }
        else
        {
            Debug.LogWarning($"[UGS] Offline: Cached {key} locally.");
        }
    }

    public static async Task SaveBatchAsync(Dictionary<string, object> data)
    {
        SaveToLocalCache(data);

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("[UGS] Batch saved to cloud.");
        }
        else
        {
            Debug.LogWarning("[UGS] Offline: Batch cached locally.");
        }
    }

    /*public static async Task<Dictionary<string, object>> LoadBatchAsync(HashSet<string> keys)
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            var data = new Dictionary<string, object>();

            foreach (var kvp in result)
            {
                data[kvp.Key] = kvp.Value.Value;
            }

            SaveToLocalCache(data);
            return data;
        }
        else
        {
            Debug.LogWarning("[UGS] Offline: Loading from local cache.");
            return LoadFromLocalCache();
        }
    }*/
    public static async Task<Dictionary<string, object>> LoadBatchAsync(HashSet<string> keys)
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            var data = new Dictionary<string, object>();

            foreach (var kvp in result)
            {
                // Always store as string so we can deserialize later
                data[kvp.Key] = kvp.Value.Value.GetAsString();
            }

            SaveToLocalCache(data);
            return data;
        }
        else
        {
            Debug.LogWarning("[UGS] Offline: Loading from local cache.");
            return LoadFromLocalCache();
        }
    }

    private static void SaveToLocalCache(Dictionary<string, object> data)
    {
        Dictionary<string, object> cache = LoadFromLocalCache();
        foreach (var kvp in data)
        {
            cache[kvp.Key] = kvp.Value;
        }

        string json = JsonUtility.ToJson(new SerializableDictionaryForJson(cache));
        System.IO.File.WriteAllText(GetLocalPath(), json);
    }

    private static Dictionary<string, object> LoadFromLocalCache()
    {
        string path = GetLocalPath();
        if (!System.IO.File.Exists(path))
        {
            return new Dictionary<string, object>();
        }

        string json = System.IO.File.ReadAllText(path);
        SerializableDictionaryForJson dict = JsonUtility.FromJson<SerializableDictionaryForJson>(json);
        return dict.ToDictionary();
    }
}