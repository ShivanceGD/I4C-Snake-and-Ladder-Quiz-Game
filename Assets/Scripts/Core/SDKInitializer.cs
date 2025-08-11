using System;
using Unity.Services.Core;
using UnityEngine;

public static class SDKInitialiizer
{

    public static async void SDKInit()
    {
        try
        {
            await UnityServices.Instance.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}