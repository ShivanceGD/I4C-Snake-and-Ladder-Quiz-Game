# I4C Snake & Ladder Quiz Game

## Overview
Multiplayer snake-and-ladder board game with quiz mechanics. Unity 6 (6000.3.9f1), URP, Netcode for GameObjects (NGO), Unity Gaming Services (UGS).

## Architecture
```
BootStrapScene -> SignUp_SignIn -> New_Menu -> Level Select -> Game Scene
```
- **Entry**: `GameBootStrapper` (`DefaultExecutionOrder(-100)`) inits UGS, spawns singletons, loads first scene
- **Flow**: `IFlowManager` interface -> `OfflineFlowManager` / `MultiplayerFlowManager` orchestrate game lifecycle
- **Networking**: NGO with `NetworkBehaviour` players, RPCs, `NetworkVariable<>`
- **Save/Load**: UGS CloudSave + local cache fallback (`SaveAndLoadManager`)
- **Quiz**: `QuizManager` loads from `QuizPackSO`, handles questions/timer/hints
- **Tournament**: Custom system using UGS CloudCode + CloudSave + Leaderboards REST API

## Key Patterns
- **Singletons**: Every manager uses `Instance` property; some `DontDestroyOnLoad`
- **ScriptableObjects**: `LevelDataSO`, `BoardDataSO`, `QuizPackSO` drive all config
- **Async/Await**: All UGS calls use async Task with CancellationTokenSource
- **No namespaces**: All ~55 classes in global namespace (legacy constraint)

## Folder Layout
```
Assets/
  Scripts/Runtime/              Primary codebase (modern)
    Global Systems/             (GameBootStrapper, SoundManager, etc.)
    Authentication System/      (AuthManager)
    Board System/               (BoardLogicManager)
    Quiz System/                (QuizManager, QuizUI, QuizTimer, QuizHintSystem)
    Player System/              (Player, PlayerMovement, PlayerHUD)
    Level Systems/              (FlowManagers, MovementManager, GameModeManager)
    Save Load System/           (SaveAndLoadManager, RemoteConfigLoadManager)
    Turn Logics/                (both commented out)
    ScriptableObjects/          (LevelDataSO, BoardDataSO, QuizPackSO)
  Scripts/Tournaments/          (TournamentManager, TournamentUI)
  Scripts/*_Scripts/            Legacy code (being phased out)
  Scenes/                       (12+ scenes)
  Resources/                    (minimal)
```

## Critical Conventions
1. **No spaces** in file/folder names ever
2. **PascalCase** for all classes, filenames match class names
3. **No namespaces** (global namespace only)
4. `SO_` prefix for ScriptableObject assets (e.g. `SO_PlayerStats.asset`)
5. All managers are singletons (`public static ClassName Instance`)
6. LeanTween for movement animation; TextMeshPro for UI text
7. Snake/ladder data in `BoardDataSO` uses `SerializedDictionary`

## Token Optimization Hints
- This file is intentionally minimal. Ask me for details on any system.
- **Use Graphify first**: query `graphify-out/graph.json` before reading files — returns scoped subgraph in ~200 tokens
- For full context after querying, read the relevant manager class.
- MCP tools available via Unity MCP (IvanMurzak). Enable only the category you need.
- Large files (>400 lines): TournamentManager(1166), TournamentUI(896), MultiplayerFlowManager(798), OfflineFlowManager(641), TournamentItem(574), MovementManager(421)
