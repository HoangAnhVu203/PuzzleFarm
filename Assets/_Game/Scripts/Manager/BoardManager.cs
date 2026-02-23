using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private RectTransform[] layerRoots; // LayerRoot_0..n (0=back, last=front)
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private TrayManager tray;

    [Header("Tile Types (14)")]
    [SerializeField] private TileTypeSO[] tileTypes;     // 14 loại

    [Header("Bag Rule")]
    [SerializeField] private int triplesPerType = 3;     // mỗi loại có 3 bộ 3 => 9 tile
    [SerializeField] private int matchCount = 3;

    [Header("Layout (Piles)")]
    [SerializeField] private Vector2 layoutCenter = Vector2.zero;
    [SerializeField] private float pileSpacing = 100f;    // khoảng cách giữa các pile (trong 1 layer)
    [SerializeField] private float layerJitter = 6f;      // lệch nhẹ theo layer (nhỏ để vẫn che nhau)
    [SerializeField] private int fixedCols = 5;           // ưu tiên 5 cột, tăng hàng tự động

    private readonly List<TileView> tiles = new();

    private void Start()
    {
        BuildBoard();
    }

    [ContextMenu("Build Board")]
    public void BuildBoard()
    {
        ClearBoard();

        if (!ValidateSetup()) return;

        // 1) Build bag (126)
        var bag = BuildBag(tileTypes, triplesPerType, matchCount);

        // 2) Build pile positions đủ để chứa ceil(total / layers)
        int layerCount = layerRoots.Length;
        int pileCount = Mathf.CeilToInt((float)bag.Count / layerCount);
        var piles = BuildPilePositions(pileCount, fixedCols, layoutCenter, pileSpacing);

        // 3) Spawn đều theo layer, mỗi layer đi qua piles (không overlap trong layer)
        SpawnFromBagByLayers(bag, piles);

        RefreshBlockState();
    }

    private bool ValidateSetup()
    {
        if (layerRoots == null || layerRoots.Length == 0)
        {
            Debug.LogError("[BoardManager] layerRoots is empty.");
            return false;
        }

        if (tilePrefab == null)
        {
            Debug.LogError("[BoardManager] tilePrefab is null.");
            return false;
        }

        if (tileTypes == null || tileTypes.Length == 0)
        {
            Debug.LogError("[BoardManager] tileTypes is empty.");
            return false;
        }

        for (int i = 0; i < tileTypes.Length; i++)
        {
            if (tileTypes[i] == null)
            {
                Debug.LogError($"[BoardManager] tileTypes[{i}] is null.");
                return false;
            }
        }

        return true;
    }

    private List<TileTypeSO> BuildBag(TileTypeSO[] types, int triplesPerType, int matchCount)
    {
        // mỗi loại: triplesPerType * matchCount (vd 3*3=9)
        int perType = Mathf.Max(1, triplesPerType) * Mathf.Max(1, matchCount);

        var bag = new List<TileTypeSO>(types.Length * perType);
        for (int i = 0; i < types.Length; i++)
        {
            for (int k = 0; k < perType; k++)
                bag.Add(types[i]);
        }

        Shuffle(bag);
        return bag;
    }

    private List<Vector2> BuildPilePositions(int pileCount, int preferredCols, Vector2 center, float spacing)
    {
        // Ưu tiên 5 cột (giống game), tăng số hàng cho đủ pileCount
        int cols = Mathf.Clamp(preferredCols, 1, pileCount);
        int rows = Mathf.CeilToInt(pileCount / (float)cols);

        var list = MakeGridPositions(rows, cols, center, spacing);

        // cắt bớt nếu dư
        if (list.Count > pileCount)
            list.RemoveRange(pileCount, list.Count - pileCount);

        Shuffle(list);
        return list;
    }

    private void SpawnFromBagByLayers(List<TileTypeSO> bag, List<Vector2> piles)
    {
        int L = layerRoots.Length;

        for (int layer = 0; layer < L; layer++)
        {
            for (int p = 0; p < piles.Count; p++)
            {
                if (bag.Count == 0) return;

                var type = PopFromBag(bag);

                // jitter nhỏ theo layer để nhìn tự nhiên nhưng vẫn che nhau
                Vector2 pos = piles[p] + LayerJitter(layer, L, layerJitter);

                SpawnTile(pos, layer, type);
            }
        }
    }

    private Vector2 LayerJitter(int layer, int totalLayers, float jitter)
    {
        if (jitter <= 0f) return Vector2.zero;

        // layer càng lên cao => jitter càng nhỏ (đỡ lộn xộn)
        float t = totalLayers <= 1 ? 0f : (layer / (float)(totalLayers - 1)); // 0..1
        float scale = Mathf.Lerp(1f, 0.35f, t);

        return Random.insideUnitCircle * (jitter * scale);
    }

    private void SpawnTile(Vector2 pos, int layer, TileTypeSO type)
    {
        layer = Mathf.Clamp(layer, 0, layerRoots.Length - 1);

        var parent = layerRoots[layer];

        var t = Instantiate(tilePrefab, parent);
        t.Init(this, type, layer);

        t.rect.anchoredPosition = pos;
        t.rect.localScale = Vector3.one;

        tiles.Add(t);
    }

    private TileTypeSO PopFromBag(List<TileTypeSO> bag)
    {
        int last = bag.Count - 1;
        var t = bag[last];
        bag.RemoveAt(last);
        return t;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    private static List<Vector2> MakeGridPositions(int rows, int cols, Vector2 center, float spacing)
    {
        var list = new List<Vector2>(rows * cols);

        float totalW = (cols - 1) * spacing;
        float totalH = (rows - 1) * spacing;

        float startX = center.x - totalW * 0.5f;
        float startY = center.y + totalH * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = startX + c * spacing;
                float y = startY - r * spacing;
                list.Add(new Vector2(x, y));
            }
        }

        return list;
    }

    private void ClearBoard()
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
            if (tiles[i]) Destroy(tiles[i].gameObject);
        tiles.Clear();
    }

    public void OnTileClicked(TileView tile)
    {
        if (tray != null && tray.TryAdd(tile))
        {
            tiles.Remove(tile);
            RefreshBlockState();
        }
    }

    public void RefreshBlockState()
    {
        // blocked nếu bị tile layer cao hơn overlap
        for (int i = 0; i < tiles.Count; i++)
        {
            var a = tiles[i];
            bool blocked = false;

            for (int j = 0; j < tiles.Count; j++)
            {
                if (i == j) continue;
                var b = tiles[j];

                if (b.layer <= a.layer) continue;

                if (RectOverlaps(a.rect, b.rect))
                {
                    blocked = true;
                    break;
                }
            }

            a.SetBlocked(blocked);
        }
    }

    private static bool RectOverlaps(RectTransform a, RectTransform b)
    {
        Vector3[] ca = new Vector3[4];
        Vector3[] cb = new Vector3[4];
        a.GetWorldCorners(ca);
        b.GetWorldCorners(cb);

        Rect ra = WorldCornersToRect(ca);
        Rect rb = WorldCornersToRect(cb);

        return ra.Overlaps(rb);
    }

    private static Rect WorldCornersToRect(Vector3[] c)
    {
        float xMin = c[0].x;
        float yMin = c[0].y;
        float xMax = c[2].x;
        float yMax = c[2].y;
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
}