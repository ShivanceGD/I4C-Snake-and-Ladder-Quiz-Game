using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
    [Header("Reference")]
    public Tilemap tilemap;

    [Header("Height and Width")]
    public int boardWidth = 10;
    public int boardHeight = 10;

    [Header("Tiles")]
    public static List<Vector3> tilePositions = new List<Vector3>();

    private void Awake()
    {
        GenerateTilePositions();
    }

    private void GenerateTilePositions()
    {
        tilePositions.Clear();

        for (int y = 0; y < boardHeight; y++)
        {
            bool leftToRight = (y % 2 == 0);

            for (int x = 0; x < boardWidth; x++)
            {
                int actualX = leftToRight ? x : boardWidth - 1 - x;
                Vector3Int cellPos = new Vector3Int(actualX, y, 0);

                if (!tilemap.HasTile(cellPos)) continue;

                Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                tilePositions.Add(worldPos);
            }
        }
    }
}
