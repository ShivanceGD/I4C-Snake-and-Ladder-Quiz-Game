using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoardManager))]
public class BoardNumbering : MonoBehaviour
{
    [SerializeField] private float zLocationOfText = 1f;

    /// <summary>
    /// Generate numbers one by one on tiles.
    /// </summary>
    /// <param name="NumberPrefab"></param>
    /// <param name="BoardSize"></param>
    public void GenerateAndPlaceTilesNumbers(GameObject NumberPrefab, int BoardSize,Transform SpawnLocation)
    {
        for (int i = 0; i < BoardSize; i++)
        {
            Vector3 worldPos = BoardManager.TilePositions[i];
            worldPos.z = zLocationOfText;

            GameObject label = Instantiate(NumberPrefab, worldPos, Quaternion.identity, SpawnLocation);
            label.GetComponent<TMP_Text>().text = (i+1).ToString();
        }
    }
}