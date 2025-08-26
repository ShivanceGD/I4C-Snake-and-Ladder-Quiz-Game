using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class BoardLogicManager : MonoBehaviour
{
    [Header("Reference")] public static BoardLogicManager Instance;
    [SerializeField] private Tilemap tilemap;

    [Header("Height and Width")] [SerializeField]
    private int boardWidth = 10;

    [SerializeField] private int boardHeight = 10;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

   

    public static List<Vector3> TilePositions { get; } = new();

    /// <summary>
    /// Return the winning tile index
    /// </summary>
    public static int GetWinningTileIndex => TilePositions.Count - 1;

    /// <summary>
    /// Return Vector3 position of winning tile index
    /// </summary>
    public static Vector3 GetWinningTilePosition => TilePositions[GetWinningTileIndex];

    /// <summary>
    /// Get Vector3 position on any index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static Vector3 GetTilePosition(int index) => TilePositions[index];

    /// <summary>
    /// Spawn and Generate Tiles Positions 
    /// </summary>
    /// <param name="Tilemap"></param>
    /// <param name="spawnParent"></param>
    /// <param name="NumberPrefab"></param>
    /// <param name="BoardWidth"></param>
    /// <param name="BoardHeight"></param>
    public void SpawnAndGenerateTilesWithNumbers(GameObject NumberPrefab, int BoardWidth, int BoardHeight)
    {
        
        GenerateTilePositions(boardWidth, boardHeight);
        GetComponent<BoardNumbering>().GenerateAndPlaceTilesNumbers(NumberPrefab, BoardHeight * BoardWidth);
    }


    private void GenerateTilePositions(int BoardWidth, int BoardHeight)
    {
        TilePositions.Clear();

        for (int y = 0; y < BoardHeight; y++)
        {
            bool leftToRight = (y % 2 == 0);

            for (int x = 0; x < BoardWidth; x++)
            {
                int actualX = leftToRight ? x : boardWidth - 1 - x;
                Vector3Int cellPos = new(actualX, y, 0);

                if (!tilemap.HasTile(cellPos)) continue;

                Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                TilePositions.Add(worldPos);
            }
        }
    }

    private void SpawnBoard(Tilemap Tilemap, Transform SpawnParent)
    {
        this.tilemap = Instantiate(Tilemap, SpawnParent);
    }

    
}
