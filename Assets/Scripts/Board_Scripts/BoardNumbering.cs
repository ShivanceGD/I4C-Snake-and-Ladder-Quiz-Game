using UnityEngine;
using TMPro;

[RequireComponent(typeof(BoardManager))]
public class BoardNumbering : MonoBehaviour
{
    [Header("Prefab Reference")]
    [SerializeField] private TMP_Text numberPrefab;

    private float zLocationOfText; //Text to show above board as text is 3d elemnent here.

    private void Start()
    {
        if (!numberPrefab)
        {
            Debug.LogError("NumberPrefab not assigned.");
            return;
        }

        zLocationOfText = numberPrefab.transform.position.z;

        GenerateAndPlaceTilesNumbers();
    }

    private void GenerateAndPlaceTilesNumbers()
    {
        for (int i = 0; i < BoardManager.TilePositions.Count; i++)
        {
            Vector3 worldPos = BoardManager.TilePositions[i];
            worldPos.z = zLocationOfText;

            GameObject label = Instantiate(numberPrefab.gameObject, worldPos, Quaternion.identity, transform);
            label.GetComponent<TMP_Text>().text = (i + 1).ToString();
        }
    }
}