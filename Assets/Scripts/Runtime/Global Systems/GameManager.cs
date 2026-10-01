using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// NOTE: This class is deprecated. Offline gameplay is now handled by OfflineFlowManager
// and multiplayer by MultiplayerFlowManager. This file is kept for reference only.
// The commented-out code below was the original GameManager that managed both offline and
// multiplayer flows. It has been superseded by GameModeManager + OfflineFlowManager + MultiplayerFlowManager.
