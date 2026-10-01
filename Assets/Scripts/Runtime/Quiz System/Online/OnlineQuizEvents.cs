using System;
using System.Collections.Generic;

public static class OnlineQuizEvents
{
    public static event Action OnQuizManifestLoadStarted;
    public static event Action<QuizPackManifestDTO> OnQuizManifestLoaded;
    public static event Action<string> OnQuizManifestLoadFailed;
    public static event Action<QuizPackManifestEntryDTO> OnQuizPackSelected;
    public static event Action<string> OnQuizPackLoadStarted;
    public static event Action<string, QuizPackSO> OnQuizPackLoaded;
    public static event Action<string, string> OnQuizPackLoadFailed;
    public static event Action<IReadOnlyList<QuizPackSO>> OnOnlineQuizFallbackUsed;
    public static event Action<bool> OnOnlineQuizModeChanged;

    public static void QuizManifestLoadStarted() => OnQuizManifestLoadStarted?.Invoke();
    public static void QuizManifestLoaded(QuizPackManifestDTO manifest) => OnQuizManifestLoaded?.Invoke(manifest);
    public static void QuizManifestLoadFailed(string message) => OnQuizManifestLoadFailed?.Invoke(message);
    public static void QuizPackSelected(QuizPackManifestEntryDTO entry) => OnQuizPackSelected?.Invoke(entry);
    public static void QuizPackLoadStarted(string packId) => OnQuizPackLoadStarted?.Invoke(packId);
    public static void QuizPackLoaded(string packId, QuizPackSO pack) => OnQuizPackLoaded?.Invoke(packId, pack);
    public static void QuizPackLoadFailed(string packId, string message) => OnQuizPackLoadFailed?.Invoke(packId, message);
    public static void OnlineQuizFallbackUsed(IReadOnlyList<QuizPackSO> fallbackPacks) => OnOnlineQuizFallbackUsed?.Invoke(fallbackPacks);
    public static void OnlineQuizModeChanged(bool online) => OnOnlineQuizModeChanged?.Invoke(online);
}
