using System;
using UnityEngine;
using UnityEngine.Events;

public class TurnHandler : MonoBehaviour
    {
        //public UnityEvent OnTurnStarted;
        private int CurrentTurnIndex {get; set;}
        public static TurnHandler Instance;
        
        private void Start()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public int GetCurrentTurnIndex() =>  CurrentTurnIndex;
        
        public int TurnIncrementInLoop(int ClampValue,int CurrentTurnIndex,bool HasPlayerFinished)
        {
            if (HasPlayerFinished) { CurrentTurnIndex++;}
            CurrentTurnIndex = (CurrentTurnIndex + 1) % ClampValue;
            return CurrentTurnIndex;
        }
    }