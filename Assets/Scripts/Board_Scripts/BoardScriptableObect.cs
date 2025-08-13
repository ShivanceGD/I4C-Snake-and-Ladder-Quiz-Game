using UnityEngine;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu(fileName = "NewBoardConfig", menuName = "Board/BoardConfig")]
public class BoardScriptableObect : ScriptableObject
{
    [SerializedDictionary("Snake Head", "Snake Tail")]
    public SerializedDictionary<int, int> Snakes;

    [SerializedDictionary("Ladder Start", "Ladder End")]
    public SerializedDictionary<int, int> Ladders;

}
