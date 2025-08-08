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
    public static List<Vector3> TilePositions { get; private set; } = new();
    public static int WinningTileIndex => TilePositions.Count - 1;

    private void Awake()
    {
        GenerateTilePositions();
    }
    public static Vector3 GetTilePosition(int index)
    {
        return TilePositions[index];
    }

    private void GenerateTilePositions()
    {
        TilePositions.Clear();

        for (int y = 0; y < boardHeight; y++)
        {
            bool leftToRight = (y % 2 == 0);

            for (int x = 0; x < boardWidth; x++)
            {
                int actualX = leftToRight ? x : boardWidth - 1 - x;
                Vector3Int cellPos = new(actualX, y, 0);

                if (!tilemap.HasTile(cellPos)) continue;

                Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                TilePositions.Add(worldPos);
            }
        }
    }
}
