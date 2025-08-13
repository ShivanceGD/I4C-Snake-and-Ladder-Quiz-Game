using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Mediates server/client RPCs: server requests client to show quiz and receives quiz result (server authoritative).
/// </summary>
public class NetworkFlowManager : NetworkBehaviour
{
    // Server: request client to show quiz for clientId
    [ServerRpc(RequireOwnership = false)]
    public void RequestQuizServerRpc(ulong clientId)
    {
        // server chooses question index
        int idx = QuizManager.Instance.GetRandomQuestionIndex();
        ShowQuizClientRpc(idx, clientId);
    }

    // ClientRpc: targeted
    [ClientRpc]
    private void ShowQuizClientRpc(int questionIndex, ulong targetClientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        var localPlayer = PlayerManager.GetLocalPlayer();
        QuizManager.Instance.ShowQuizFromServerIndex(questionIndex, localPlayer);
    }

    // Client -> Server: send result
    // Inside NetworkFlowManager on the server:
    [ServerRpc(RequireOwnership = false)]
    public void SendQuizResultServerRpc(ulong clientId, bool isCorrect, Difficulty difficulty, float timeTaken)
    {
        var player = PlayerManager.GetByClientId(clientId);
        if (player == null) return;

        // Fire the QuizManager event on the SERVER so GameManager.HandleQuizCompleted runs
        if (QuizManager.Instance != null)
            QuizManager.Instance.OnQuizCompleted?.Invoke(player, isCorrect, difficulty, timeTaken);
    }

}
