# CyberLadder Online Quiz Packs

CyberLadder keeps `QuizPackSO` as the gameplay source of truth. Online quiz packs are stored as JSON in Unity Gaming Services Cloud Save Custom Data, loaded at runtime, converted into a runtime `QuizPackSO`, and then passed through the existing Practice, Pass-and-Play, Multiplayer, Tournament, Summary, and Leaderboard flows.

## UGS Storage

Create one Cloud Save Custom Data catalog:

- Custom data ID: `cyberladder_quizzes`
- Manifest key: `cyberladder_quiz_manifest`
- Full pack keys: `cyberladder_quiz_pack_{packId}`

Examples:

- `cyberladder_quiz_pack_investment_scams_basic`
- `cyberladder_quiz_pack_password_safety`
- `cyberladder_quiz_pack_online_fraud_awareness`

`OnlineQuizConfigSO` exposes the catalog ID, manifest key, pack key prefix, fallback toggles, local cache toggle, and cache folder name.

## JSON Formats

The manifest is lightweight metadata for selection UI:

```json
{
  "schemaVersion": "1.0",
  "updatedAtIso": "2026-01-01T00:00:00Z",
  "packs": [
    {
      "packId": "password_safety",
      "displayName": "Password Safety",
      "categoryName": "Security",
      "description": "Basic password safety questions",
      "version": "1.0",
      "isActive": true,
      "sortOrder": 0,
      "questionCount": 10,
      "easyCount": 4,
      "mediumCount": 4,
      "hardCount": 2,
      "updatedAtIso": "2026-01-01T00:00:00Z"
    }
  ]
}
```

Full pack JSON mirrors `QuizPackSO`:

```json
{
  "packId": "password_safety",
  "displayName": "Password Safety",
  "categoryName": "Security",
  "version": "1.0",
  "questions": [
    {
      "question": "Which password is strongest?",
      "options": ["123456", "password", "A long unique phrase", "qwerty"],
      "correctAnswerIndex": 2,
      "questionsDifficulty": 0,
      "isHintAllowed": true,
      "timeLimit": 30,
      "characterData": {
        "characterName": "Guide",
        "characterInfo": "Cyber safety helper",
        "characterGender": 2
      }
    }
  ]
}
```

## Runtime Flow

- Practice and Pass-and-Play call the existing quiz button generator in `MainMenuUI`.
- The UI loads the online manifest, creates buttons from active entries, then loads the full pack only when selected.
- `QuizPackOnlineConverter` converts the JSON DTO into a runtime `QuizPackSO`.
- `GameModeManager` stores online metadata while keeping `GameModeManager.QuizPack` synchronized for existing systems.
- `QuizManager` continues to consume `QuizPackSO`.
- If online loading fails and local fallback is enabled, the game uses the existing inspector-assigned local `QuizPackSO` list.

## Multiplayer And Tournament

- Multiplayer hosts sync the selected online pack ID through a `NetworkVariable`.
- Clients load the same pack ID from Cloud Save and use the same question-index RPC flow.
- Server answer validation recomputes correctness from the host-loaded `QuizPackSO`.
- Tournaments store online metadata: `quizPackId`, display name, category, version, and `usesOnlineQuizPack`.
- Existing `selectedQuizPackName` remains for older local tournament data.

## Exporting Local Packs

Use `Shivance Tools/QuizPackSO To UGS JSON Exporter`:

1. Select one or more `QuizPackSO` assets.
2. Choose output folder, category, version, and active flag.
3. Export full pack JSON files and `cyberladder_quiz_manifest.json`.
4. Upload the manifest JSON to `cyberladder_quiz_manifest`.
5. Upload each full pack JSON to `cyberladder_quiz_pack_{packId}`.

The exporter does not modify source `QuizPackSO` assets.
