using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Vector3 = UnityEngine.Vector3;

public class BoardLogicManager : MonoBehaviour
{
    [Header("Reference")]
    public static BoardLogicManager Instance;
    [SerializeField] private Tilemap tilemap;
    public Transform playerHouseLocation;
    public static List<Vector3> TilePositions { get; } = new();
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static int GetWinningTileIndex => TilePositions.Count == 0 ? 0 : TilePositions.Count - 1;
    public static Vector3 GetWinningTilePosition => TilePositions.Count == 0 ? Vector3.zero : TilePositions[GetWinningTileIndex];

    public static Vector3 GetTilePosition(int index)
    {
        if (TilePositions == null || TilePositions.Count == 0) return Vector3.zero;
        index = Mathf.Clamp(index, 0, TilePositions.Count - 1);
        return TilePositions[index];
    }

   

    public void GenerateTilesPositionWithNumbers(GameObject NumberPrefab, int BoardWidth, int BoardHeight)
    {
        GenerateTilePositions(BoardWidth, BoardHeight);
        var bn = GetComponent<BoardNumbering>();
        if (bn != null)
            bn.GenerateAndPlaceTilesNumbers(NumberPrefab, BoardHeight * BoardWidth);
    }

    private void GenerateTilePositions(int BoardWidth, int BoardHeight)
    {
        TilePositions.Clear();

        for (int y = 0; y < BoardHeight; y++)
        {
            bool leftToRight = (y % 2 == 0);

            for (int x = 0; x < BoardWidth; x++)
            {
                int actualX = leftToRight ? x : BoardWidth - 1 - x;
                Vector3Int cellPos = new Vector3Int(actualX, y, 0);

                if (!tilemap.HasTile(cellPos)) continue;

                Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                TilePositions.Add(worldPos);
            }
        }

        Debug.Log($"[BoardLogicManager] Generated {TilePositions.Count} tile positions.");
    }

    // nearest tile index for a world position (used by movement)
    public static int GetTileIndexFromPosition(Vector3 worldPos)
    {
        if (TilePositions == null || TilePositions.Count == 0) return 0;
        int best = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < TilePositions.Count; i++)
        {
            float sq = (TilePositions[i] - worldPos).sqrMagnitude;
            if (sq < bestSqr) { bestSqr = sq; best = i; }
        }
        return best;
    }
}
