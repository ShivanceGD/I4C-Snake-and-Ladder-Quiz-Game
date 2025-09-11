using UnityEngine;
public class GameModeManager : MonoBehaviour
{
       public static GameModeManager Instance;
       public int NumberOfPlayersToBeSpawned =  1;
       public QuizPackSO QuizPack;
       private void Awake()
       {
              if (Instance != null && Instance != this)
              {
                     Destroy(gameObject);
                     return;
              }
              Instance = this;
              DontDestroyOnLoad(gameObject);
       }
       /*public void ChooseNumberOfPlayersForPassNPlayMode(int NumberOfPlayers)
       {
              NumberOfPlayersToBeSpawned = NumberOfPlayers;
              Debug.Log($"[GameModeManager] NumberOfPlayers set to {NumberOfPlayers}");
       }

       public void ChooseModusOperandiToOfflineLevel(QuizPackSO quizPack)
       {
              QuizPack = quizPack;
              Debug.Log($"[GameModeManager] QuizPack set to {quizPack?.name}");
       }*/


      
}
