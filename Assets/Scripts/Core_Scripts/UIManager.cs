using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager :  MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public Transform OfflineLevelsButtonParentTransform;

    private void Start()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
}