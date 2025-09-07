using UnityEngine;
public class GameModeManager : MonoBehaviour
{
       public static GameModeManager Instance;
       public int NumberOfPlayersToBeSpawned { get; private set; }
       public QuizPackSO QuizPack { get; private set; }
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
       public void ChooseNumberOfPlayersForPassNPlayMode(int NumberOfPlayers)
       {
              NumberOfPlayersToBeSpawned = NumberOfPlayers;
              //Offline Flow Manager Player to Spawn Set To NumberOfPlayers
       }

       public void ChooseModusOperandiToOfflineLevel(QuizPackSO quizPack)
       {
              QuizPack = quizPack;
              //Offline Flow Manager QuizPack Set To level for the level 
       }

      
}
