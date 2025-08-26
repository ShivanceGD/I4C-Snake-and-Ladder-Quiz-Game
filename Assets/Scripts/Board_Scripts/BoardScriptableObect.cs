using UnityEngine;
using AYellowpaper.SerializedCollections;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New Board", menuName = "Shivance Games/Create New Board")]
public class BoardScriptableObect : ScriptableObject
{
    [Header("Board Data")] 
    public GameObject BoardPrefab;
    public GameObject NumberToSpawnOnBoard;
    public BoardScriptableObect BoardJointsSCO;
    public int BoardHeight, BoardWidth;
    
    [SerializedDictionary("Snake Head", "Snake Tail")]
    public SerializedDictionary<int, int> Snakes;

    [SerializedDictionary("Ladder Start", "Ladder End")]
    public SerializedDictionary<int, int> Ladders;

}
