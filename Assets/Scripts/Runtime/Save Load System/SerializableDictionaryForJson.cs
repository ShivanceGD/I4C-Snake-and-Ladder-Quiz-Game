using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Purpose: Workaround for JsonUtility limitations.
/// JsonUtility can’t directly serialize Dictionary<string, object>.
/// This class converts the dictionary into two parallel lists
/// </summary>
[Serializable]
public class SerializableDictionaryForJson
{
    public List<string> keys = new();
    public List<string> values = new();

    public SerializableDictionaryForJson(Dictionary<string, object> dict)
    {
        foreach (var kvp in dict)
        {
            keys.Add(kvp.Key);
            values.Add(JsonUtility.ToJson(new Wrapper(kvp.Value)));
        }
    }

    public Dictionary<string, object> ToDictionary()
    {
        Dictionary<string, object> dict = new();
        for (int i = 0; i < keys.Count; i++)
        {
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(values[i]);
            dict[keys[i]] = wrapper.value;
        }
        return dict;
    }

    [Serializable]
    private class Wrapper
    {
        public object value;
        public Wrapper(object value) => this.value = value;
    }
}