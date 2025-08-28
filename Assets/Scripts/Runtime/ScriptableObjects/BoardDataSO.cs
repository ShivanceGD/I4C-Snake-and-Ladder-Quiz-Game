using UnityEngine;
using AYellowpaper.SerializedCollections;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New Board", menuName = "Shivance Games/Create New Board")]
public class BoardDataSO : ScriptableObject
{
    [Header("Board References")]
    public GameObject BoardPrefab;
    public GameObject NumberPrefabToSpawnOnBoard;
    
    [Header("Board Settings")]
    public int BoardHeight;
    public int BoardWidth;
    
    [SerializedDictionary("Snake Head", "Snake Tail")]
    public SerializedDictionary<int, int> Snakes;

    [SerializedDictionary("Ladder Start", "Ladder End")]
    public SerializedDictionary<int, int> Ladders;

}
