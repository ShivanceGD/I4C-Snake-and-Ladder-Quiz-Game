# Graph Report - I4C-Snake-and-Ladder-Quiz-Game  (2026-06-07)

## Corpus Check
- 67 files · ~68,298 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1359 nodes · 2026 edges · 84 communities (76 shown, 8 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `2acfac65`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]
- [[_COMMUNITY_Community 6|Community 6]]
- [[_COMMUNITY_Community 7|Community 7]]
- [[_COMMUNITY_Community 8|Community 8]]
- [[_COMMUNITY_Community 9|Community 9]]
- [[_COMMUNITY_Community 10|Community 10]]
- [[_COMMUNITY_Community 11|Community 11]]
- [[_COMMUNITY_Community 12|Community 12]]
- [[_COMMUNITY_Community 13|Community 13]]
- [[_COMMUNITY_Community 14|Community 14]]
- [[_COMMUNITY_Community 15|Community 15]]
- [[_COMMUNITY_Community 16|Community 16]]
- [[_COMMUNITY_Community 17|Community 17]]
- [[_COMMUNITY_Community 18|Community 18]]
- [[_COMMUNITY_Community 19|Community 19]]
- [[_COMMUNITY_Community 20|Community 20]]
- [[_COMMUNITY_Community 21|Community 21]]
- [[_COMMUNITY_Community 22|Community 22]]
- [[_COMMUNITY_Community 23|Community 23]]
- [[_COMMUNITY_Community 24|Community 24]]
- [[_COMMUNITY_Community 25|Community 25]]
- [[_COMMUNITY_Community 26|Community 26]]
- [[_COMMUNITY_Community 27|Community 27]]
- [[_COMMUNITY_Community 28|Community 28]]
- [[_COMMUNITY_Community 29|Community 29]]
- [[_COMMUNITY_Community 30|Community 30]]
- [[_COMMUNITY_Community 31|Community 31]]
- [[_COMMUNITY_Community 32|Community 32]]
- [[_COMMUNITY_Community 33|Community 33]]
- [[_COMMUNITY_Community 34|Community 34]]
- [[_COMMUNITY_Community 35|Community 35]]
- [[_COMMUNITY_Community 36|Community 36]]
- [[_COMMUNITY_Community 37|Community 37]]
- [[_COMMUNITY_Community 38|Community 38]]
- [[_COMMUNITY_Community 39|Community 39]]
- [[_COMMUNITY_Community 40|Community 40]]
- [[_COMMUNITY_Community 41|Community 41]]
- [[_COMMUNITY_Community 42|Community 42]]
- [[_COMMUNITY_Community 43|Community 43]]
- [[_COMMUNITY_Community 44|Community 44]]
- [[_COMMUNITY_Community 45|Community 45]]
- [[_COMMUNITY_Community 46|Community 46]]
- [[_COMMUNITY_Community 47|Community 47]]
- [[_COMMUNITY_Community 48|Community 48]]
- [[_COMMUNITY_Community 49|Community 49]]
- [[_COMMUNITY_Community 50|Community 50]]
- [[_COMMUNITY_Community 51|Community 51]]
- [[_COMMUNITY_Community 52|Community 52]]
- [[_COMMUNITY_Community 53|Community 53]]
- [[_COMMUNITY_Community 54|Community 54]]
- [[_COMMUNITY_Community 55|Community 55]]
- [[_COMMUNITY_Community 56|Community 56]]
- [[_COMMUNITY_Community 57|Community 57]]
- [[_COMMUNITY_Community 58|Community 58]]
- [[_COMMUNITY_Community 59|Community 59]]
- [[_COMMUNITY_Community 60|Community 60]]
- [[_COMMUNITY_Community 61|Community 61]]
- [[_COMMUNITY_Community 62|Community 62]]
- [[_COMMUNITY_Community 63|Community 63]]
- [[_COMMUNITY_Community 64|Community 64]]
- [[_COMMUNITY_Community 65|Community 65]]
- [[_COMMUNITY_Community 66|Community 66]]
- [[_COMMUNITY_Community 67|Community 67]]
- [[_COMMUNITY_Community 68|Community 68]]
- [[_COMMUNITY_Community 69|Community 69]]
- [[_COMMUNITY_Community 70|Community 70]]
- [[_COMMUNITY_Community 71|Community 71]]
- [[_COMMUNITY_Community 72|Community 72]]
- [[_COMMUNITY_Community 73|Community 73]]
- [[_COMMUNITY_Community 74|Community 74]]
- [[_COMMUNITY_Community 75|Community 75]]
- [[_COMMUNITY_Community 76|Community 76]]
- [[_COMMUNITY_Community 77|Community 77]]
- [[_COMMUNITY_Community 78|Community 78]]
- [[_COMMUNITY_Community 79|Community 79]]
- [[_COMMUNITY_Community 80|Community 80]]

## God Nodes (most connected - your core abstractions)
1. `MultiplayerFlowManager` - 55 edges
2. `TournamentDebugger` - 55 edges
3. `TournamentManager` - 53 edges
4. `OfflineFlowManager` - 51 edges
5. `TournamentUI` - 41 edges
6. `MainMenuUI` - 30 edges
7. `Task` - 28 edges
8. `TournamentItem` - 25 edges
9. `QuizUI` - 24 edges
10. `AuthManager` - 20 edges

## Surprising Connections (you probably didn't know these)
- `MainMenuUI` --inherits--> `MonoBehaviour`  [EXTRACTED]
  Assets/MainMenuUI.cs →   _Bridges community 10 → community 46_
- `AuthManager` --inherits--> `MonoBehaviour`  [EXTRACTED]
  Assets/Scripts/Runtime/Authentication System/AuthManager.cs →   _Bridges community 46 → community 19_
- `BoardManager` --inherits--> `MonoBehaviour`  [EXTRACTED]
  Assets/Scripts/Board_Scripts/BoardManager.cs →   _Bridges community 46 → community 49_
- `BoardLogicManager` --inherits--> `MonoBehaviour`  [EXTRACTED]
  Assets/Scripts/Runtime/Board System/BoardLogicManager.cs →   _Bridges community 46 → community 39_
- `UiLogicManager` --inherits--> `MonoBehaviour`  [EXTRACTED]
  Assets/Scripts/Core_Scripts/UiLogicManager.cs →   _Bridges community 46 → community 29_

## Import Cycles
- None detected.

## Communities (84 total, 8 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.07
Nodes (21): bool, ContextMenu, float, int, List, string, Task, TournamentData (+13 more)

### Community 1 - "Community 1"
Cohesion: 0.06
Nodes (31): BoardDataSO, BoardLogicManager, bool, Color, ContextMenu, Dictionary, GameObject, IEnumerator (+23 more)

### Community 2 - "Community 2"
Cohesion: 0.05
Nodes (30): BoardLogicManager, bool, Button, Color, Dictionary, float, GameObject, IEnumerator (+22 more)

### Community 3 - "Community 3"
Cohesion: 0.03
Nodes (58): dependencies, com.boxqkrtm.ide.cursor, com.coplaydev.unity-mcp, com.unity.collab-proxy, com.unity.feature.2d, com.unity.ide.rider, com.unity.ide.visualstudio, com.unity.inputsystem (+50 more)

### Community 4 - "Community 4"
Cohesion: 0.15
Nodes (7): bool, ContextMenu, float, int, string, Task, TournamentDebugger

### Community 5 - "Community 5"
Cohesion: 0.07
Nodes (17): bool, Button, Coroutine, float, GameObject, IEnumerator, List, MainMenuUI (+9 more)

### Community 6 - "Community 6"
Cohesion: 0.04
Nodes (45): dependencies, depth, source, version, dependencies, depth, source, version (+37 more)

### Community 7 - "Community 7"
Cohesion: 0.06
Nodes (22): bool, Color, GameObject, IEnumerator, int, List, NetworkVariable, ServerRpc (+14 more)

### Community 8 - "Community 8"
Cohesion: 0.05
Nodes (38): List, Sprite, string, GameObject, int, SerializedDictionary, BoardDataSO, bool (+30 more)

### Community 9 - "Community 9"
Cohesion: 0.07
Nodes (34): dependencies, depth, source, url, version, dependencies, depth, source (+26 more)

### Community 10 - "Community 10"
Cohesion: 0.09
Nodes (11): Button, Color, GameObject, QuizPackSO, string, TMP_InputField, TMP_Text, Transform (+3 more)

### Community 11 - "Community 11"
Cohesion: 0.09
Nodes (13): Action, Button, Color, GameObject, Image, List, QuestionsDifficulty, QuizQuestionData (+5 more)

### Community 12 - "Community 12"
Cohesion: 0.11
Nodes (9): Button, float, GameObject, MainMenuUI, TextMeshProUGUI, TMP_InputField, TournamentData, TournamentItem (+1 more)

### Community 13 - "Community 13"
Cohesion: 0.09
Nodes (25): depth, source, version, dependencies, depth, source, version, dependencies (+17 more)

### Community 14 - "Community 14"
Cohesion: 0.09
Nodes (24): dependencies, depth, source, url, version, dependencies, depth, source (+16 more)

### Community 15 - "Community 15"
Cohesion: 0.13
Nodes (11): bool, ContextMenu, float, GameObject, IEnumerator, Image, List, TMP_Text (+3 more)

### Community 16 - "Community 16"
Cohesion: 0.19
Nodes (14): int, List, string, Task, LeaderboardInfo, ResetConfig, AuthResponse, LeaderboardAPIManager (+6 more)

### Community 17 - "Community 17"
Cohesion: 0.10
Nodes (22): dependencies, depth, source, url, version, depth, source, url (+14 more)

### Community 18 - "Community 18"
Cohesion: 0.10
Nodes (22): dependencies, depth, source, version, dependencies, depth, source, version (+14 more)

### Community 19 - "Community 19"
Cohesion: 0.12
Nodes (10): bool, Button, Color, GameObject, string, TMP_InputField, TMP_Text, Toggle (+2 more)

### Community 20 - "Community 20"
Cohesion: 0.10
Nodes (21): dependencies, dependencies, dependencies, depth, source, url, version, dependencies (+13 more)

### Community 21 - "Community 21"
Cohesion: 0.12
Nodes (12): Action, Coroutine, int, List, Player, QuizPackSO, QuizQuestionData, QuizResult (+4 more)

### Community 22 - "Community 22"
Cohesion: 0.14
Nodes (10): bool, Button, ContextMenu, Dictionary, GameObject, int, LevelDataSO, List (+2 more)

### Community 23 - "Community 23"
Cohesion: 0.19
Nodes (12): Action, BoardDataSO, Dictionary, DifficultyStepRange, float, IEnumerator, IFlowManager, MovementResult (+4 more)

### Community 24 - "Community 24"
Cohesion: 0.11
Nodes (9): bool, Color, int, SerializedDictionary, string, PlayerGameData, PlayerGameStateData, PlayerGameStateData (+1 more)

### Community 25 - "Community 25"
Cohesion: 0.11
Nodes (17): dependencies, depth, source, url, version, dependencies, depth, source (+9 more)

### Community 26 - "Community 26"
Cohesion: 0.11
Nodes (18): dependencies, dependencies, depth, source, url, version, dependencies, depth (+10 more)

### Community 27 - "Community 27"
Cohesion: 0.17
Nodes (3): Task, AuthExtensions, UnityAction

### Community 28 - "Community 28"
Cohesion: 0.16
Nodes (10): Analytics_Manager, string, Task, CancellationToken, CancellationTokenSource, GameBootStrapper, LoadingSceneManager, RemoteConfigLoadManager (+2 more)

### Community 29 - "Community 29"
Cohesion: 0.18
Nodes (5): Action, LevelDataSO, Transform, UiLogicManager, LevelDataHolder

### Community 30 - "Community 30"
Cohesion: 0.16
Nodes (15): dependencies, depth, source, url, version, dependencies, dependencies, dependencies (+7 more)

### Community 31 - "Community 31"
Cohesion: 0.18
Nodes (7): Dictionary, LevelDataSO, List, string, Task, LevelData, SaveAndLoadManager

### Community 32 - "Community 32"
Cohesion: 0.32
Nodes (6): Dictionary, HashSet, string, T, Task, OfflineUGSSaveManager

### Community 33 - "Community 33"
Cohesion: 0.15
Nodes (13): dependencies, depth, source, url, version, dependencies, depth, source (+5 more)

### Community 34 - "Community 34"
Cohesion: 0.21
Nodes (6): bool, float, IEnumerator, int, CharacterMovement, PathMaps

### Community 35 - "Community 35"
Cohesion: 0.21
Nodes (6): bool, Color, float, List, Vector2, PathMaps

### Community 36 - "Community 36"
Cohesion: 0.20
Nodes (6): int, string, Task, LeaderboardEntry, LeaderboardScoresPage, Leaderboard

### Community 37 - "Community 37"
Cohesion: 0.29
Nodes (6): bool, float, IEnumerator, Vector3, LeanTweenType, PlayerMovement

### Community 38 - "Community 38"
Cohesion: 0.17
Nodes (12): dependencies, depth, source, url, version, dependencies, depth, source (+4 more)

### Community 39 - "Community 39"
Cohesion: 0.25
Nodes (5): GameObject, Tilemap, Transform, Vector3, BoardLogicManager

### Community 40 - "Community 40"
Cohesion: 0.22
Nodes (5): IEnumerable, int, List, Player, OfflineTurnLogic

### Community 41 - "Community 41"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, depth, source, url (+3 more)

### Community 42 - "Community 42"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, depth, source, url (+3 more)

### Community 43 - "Community 43"
Cohesion: 0.18
Nodes (11): depth, source, url, version, dependencies, depth, source, url (+3 more)

### Community 44 - "Community 44"
Cohesion: 0.18
Nodes (11): dependencies, depth, source, url, version, depth, source, url (+3 more)

### Community 45 - "Community 45"
Cohesion: 0.27
Nodes (6): Action<float>, bool, Coroutine, IEnumerator, UnityEvent, QuizTimer

### Community 46 - "Community 46"
Cohesion: 0.20
Nodes (5): float, GameObject, BoardNumbering, CharacterMoverOnPath, MonoBehaviour

### Community 47 - "Community 47"
Cohesion: 0.31
Nodes (6): List, string, Task, LevelsToBeUnlockedConfigWrapper, RemoteConfigLoadManager, TorunamentAdminsConfigWrapper

### Community 48 - "Community 48"
Cohesion: 0.31
Nodes (3): AudioClip, AudioSource, SoundManager

### Community 49 - "Community 49"
Cohesion: 0.25
Nodes (5): int, Tilemap, Transform, Vector3, BoardManager

### Community 50 - "Community 50"
Cohesion: 0.28
Nodes (8): int, List, string, LevelData, LevelProgress, MultiPlayerData, PlayerCommonData, SinglePlayerData

### Community 51 - "Community 51"
Cohesion: 0.28
Nodes (3): List, TMP_Text, PlayerHUD

### Community 52 - "Community 52"
Cohesion: 0.25
Nodes (7): Available Tools, Before Coding, Code Standards, I4C Snake & Ladder Quiz Game — Agent Instructions, Response Style, Token Budget Rules, Your Role

### Community 53 - "Community 53"
Cohesion: 0.29
Nodes (4): string, ExcelToQuizPackWindow, EditorWindow, MenuItem

### Community 54 - "Community 54"
Cohesion: 0.32
Nodes (7): bool, int, List, string, TournamentStatus, CustomLeaderboardEntry, TournamentData

### Community 55 - "Community 55"
Cohesion: 0.25
Nodes (7): Architecture, Critical Conventions, Folder Layout, I4C Snake & Ladder Quiz Game, Key Patterns, Overview, Token Optimization Hints

### Community 56 - "Community 56"
Cohesion: 0.25
Nodes (7): contextPaths, mcpServers, UnityMCP, $schema, args, command, type

### Community 57 - "Community 57"
Cohesion: 0.25
Nodes (7): defaultDependencyTypeInfo, defaultInstantiationMode, type, userAdded, dependencyTypeInfos, newSceneOverride, templatePinStates

### Community 58 - "Community 58"
Cohesion: 0.29
Nodes (4): Color, HashSet, List, GlobalColourManager

### Community 59 - "Community 59"
Cohesion: 0.29
Nodes (5): Dictionary, List, object, SerializableDictionaryForJson, Wrapper

### Community 60 - "Community 60"
Cohesion: 0.29
Nodes (4): bool, Image, Sprite, SpriteSwapper

### Community 61 - "Community 61"
Cohesion: 0.29
Nodes (7): dependencies, depth, source, url, version, com.unity.modules.unityanalytics, com.unity.remote-config-runtime

### Community 63 - "Community 63"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, version, com.unity.modules.adaptiveperformance, com.unity.modules.subsystems

### Community 64 - "Community 64"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, version, dependencies, com.unity.modules.androidjni

### Community 65 - "Community 65"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, version, com.unity.modules.cloth, com.unity.modules.physics

### Community 66 - "Community 66"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.multiplayer.playmode

### Community 67 - "Community 67"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.remote-config

### Community 68 - "Community 68"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.services.cloudcode

### Community 69 - "Community 69"
Cohesion: 0.33
Nodes (6): dependencies, depth, source, url, version, com.unity.services.wire

### Community 71 - "Community 71"
Cohesion: 0.40
Nodes (3): GameObject, Transform, UIManager

### Community 73 - "Community 73"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.accessibility

### Community 74 - "Community 74"
Cohesion: 0.40
Nodes (5): dependencies, depth, source, version, com.unity.modules.ai

### Community 75 - "Community 75"
Cohesion: 0.50
Nodes (3): LevelDataSO, List, LevelDataHolder

## Knowledge Gaps
- **609 isolated node(s):** `UnityMCP`, `$schema`, `command`, `args`, `type` (+604 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OfflineFlowManager` connect `Community 2` to `Community 1`, `Community 46`?**
  _High betweenness centrality (0.126) - this node is a cross-community bridge._
- **Why does `MultiplayerFlowManager` connect `Community 1` to `Community 2`, `Community 7`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Why does `dependencies` connect `Community 25` to `Community 6`, `Community 9`, `Community 13`, `Community 14`, `Community 17`, `Community 18`, `Community 20`, `Community 26`, `Community 30`, `Community 33`, `Community 38`, `Community 41`, `Community 42`, `Community 43`, `Community 44`, `Community 61`, `Community 63`, `Community 64`, `Community 65`, `Community 66`, `Community 67`, `Community 68`, `Community 69`, `Community 73`, `Community 74`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **What connects `UnityMCP`, `$schema`, `command` to the rest of the system?**
  _609 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.0697980684811238 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.057692307692307696 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.053763440860215055 - nodes in this community are weakly interconnected._