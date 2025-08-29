using TMPro;
using UnityEngine;


public class BoardNumbering : MonoBehaviour
{
    [SerializeField] private float zLocationOfText = 1f;

    /// <summary>
    /// Generate numbers one by one on tiles.
    /// </summary>
    /// <param name="NumberPrefab"></param>
    /// <param name="BoardSize"></param>
    public void GenerateAndPlaceTilesNumbers(GameObject NumberPrefab, int BoardSize)
    {
        for (int i = 0; i < BoardSize; i++)
        {
            Vector3 worldPos = BoardLogicManager.TilePositions[i];
            worldPos.z = zLocationOfText;

            GameObject label = Instantiate(NumberPrefab, worldPos, Quaternion.identity, transform);
            label.GetComponent<TMP_Text>().text = (i+1).ToString();
        }
    }
}