
using System;
using Unity.Services.Analytics;
using UnityEngine;

public class Analytics_Manager : MonoBehaviour
{
    public static Analytics_Manager Instance{get; private set;}

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

   public void StartCollection()
    {
        AnalyticsService.Instance.StartDataCollection();
    }

    public void LogEvent(string EventName)
    { 
        var customEvent = new CustomEvent(EventName);
        AnalyticsService.Instance.RecordEvent(customEvent);
    }

    private void OnApplicationQuit()
    {
        AnalyticsService.Instance.StopDataCollection();
    }
}
