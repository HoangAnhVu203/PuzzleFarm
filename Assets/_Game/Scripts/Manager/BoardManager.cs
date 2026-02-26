using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private RectTransform[] layerRoots; // 0=back, last=front
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private TrayManager tray;

    [Header("Tile Types")]
    [SerializeField] private TileTypeSO[] tileTypes;    

    [Header("Bag Rule")]
    [SerializeField] private int triplesPerType = 3;    
    [SerializeField] private int matchCount = 3;         

    [Header("Layout (Piles)")]
    [SerializeField] private Vector2 layoutCenter = Vector2.zero;
    [SerializeField] private float pileSpacing = 100f;
    [SerializeField] private float layerJitter = 6f;
    [SerializeField] private int fixedCols = 5;

    [Header("Shuffle FX")]
    [SerializeField] private RectTransform shuffleFlyRoot;
    [SerializeField] private float gatherDuration = 0.18f;
    [SerializeField] private float spreadDuration = 0.22f;
    [SerializeField] private AnimationCurve gatherEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve spreadEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float gatherScale = 0.85f;


    public event Action onBoardChanged;
    public int BoardCount
    {
        get { PruneNullTiles(); return tiles.Count; }
    }

    private readonly List<TileView> tiles = new();
    private List<Vector2> cachedPiles;
    private bool isShuffling;

    void Start()
    {
        GameContext.Instance?.RegisterBoard(this);
        BuildBoard();
    }

    [ContextMenu("Build Board")]
    public void BuildBoard()
    {
        ClearBoard();
        if (!ValidateSetup()) return;

        // Build bag
        var bag = BuildBag(tileTypes, triplesPerType, matchCount);

        // Build piles
        int L = layerRoots.Length;
        int pileCount = Mathf.CeilToInt((float)bag.Count / L);
        var piles = BuildPilePositions(pileCount, fixedCols, layoutCenter, pileSpacing);
        cachedPiles = new List<Vector2>(piles);

        // Spawn by layers (no overlap within same layer)
        SpawnFromBagByLayers(bag, piles);

        RefreshBlockState();
        onBoardChanged?.Invoke();
    }

    bool ValidateSetup()
    {
        if (!boardRoot)
        {
            Debug.LogError("[BoardManager] boardRoot is null.");
            return false;
        }
        if (layerRoots == null || layerRoots.Length == 0)
        {
            Debug.LogError("[BoardManager] layerRoots is empty.");
            return false;
        }
        if (!tilePrefab)
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
            if (!tileTypes[i])
            {
                Debug.LogError($"[BoardManager] tileTypes[{i}] is null.");
                return false;
            }
        }
        return true;
    }

    // =========================
    // Spawn / Bag
    // =========================

    List<TileTypeSO> BuildBag(TileTypeSO[] types, int triples, int mCount)
    {
        int perType = Mathf.Max(1, triples) * Mathf.Max(1, mCount); // 3*3 = 9
        var bag = new List<TileTypeSO>(types.Length * perType);

        for (int i = 0; i < types.Length; i++)
            for (int k = 0; k < perType; k++)
                bag.Add(types[i]);

        Shuffle(bag);
        return bag;
    }

    List<Vector2> BuildPilePositions(int pileCount, int preferredCols, Vector2 center, float spacing)
    {
        int cols = Mathf.Clamp(preferredCols, 1, pileCount);
        int rows = Mathf.CeilToInt(pileCount / (float)cols);

        var list = MakeGridPositions(rows, cols, center, spacing);

        if (list.Count > pileCount)
            list.RemoveRange(pileCount, list.Count - pileCount);

        //Shuffle(list);
        return list;
    }

    void SpawnFromBagByLayers(List<TileTypeSO> bag, List<Vector2> piles)
    {
        int L = layerRoots.Length;

        for (int layer = 0; layer < L; layer++)
        {
            for (int p = 0; p < piles.Count; p++)
            {
                if (bag.Count == 0) return;

                var type = PopFromBag(bag);
                Vector2 pos = piles[p] + LayerJitter(layer, L, layerJitter);

                SpawnTile(pos, layer, type);
            }
        }
    }

    void SpawnTile(Vector2 pos, int layer, TileTypeSO type)
    {
        layer = Mathf.Clamp(layer, 0, layerRoots.Length - 1);

        var parent = layerRoots[layer];
        var t = Instantiate(tilePrefab, parent);

        t.Init(this, type, layer);
        t.rect.anchoredPosition = pos;
        t.rect.localScale = Vector3.one;

        tiles.Add(t);
    }

    Vector2 LayerJitter(int layer, int totalLayers, float jitter)
    {
        if (jitter <= 0f) return Vector2.zero;

        float t = totalLayers <= 1 ? 0f : (layer / (float)(totalLayers - 1)); // 0..1
        float scale = Mathf.Lerp(1f, 0.35f, t);

        return UnityEngine.Random.insideUnitCircle * (jitter * scale);
    }

    TileTypeSO PopFromBag(List<TileTypeSO> bag)
    {
        int last = bag.Count - 1;
        var t = bag[last];
        bag.RemoveAt(last);
        return t;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    static List<Vector2> MakeGridPositions(int rows, int cols, Vector2 center, float spacing)
    {
        var list = new List<Vector2>(rows * cols);

        float totalW = (cols - 1) * spacing;
        float totalH = (rows - 1) * spacing;

        float startX = center.x - totalW * 0.5f;
        float startY = center.y + totalH * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            var rowList = new List<Vector2>();

            for (int c = 0; c < cols; c++)
            {
                float x = startX + c * spacing;
                float y = startY - r * spacing; // từ trên xuống dưới
                rowList.Add(new Vector2(x, y));
            }

            // ✅ random trong từng hàng
            Shuffle(rowList);

            // add vào list chính theo thứ tự hàng
            list.AddRange(rowList);
        }

        return list;
    }

    // =========================
    // Click -> Tray
    // =========================

    public void OnTileClicked(TileView tile)
    {
        if (!tile) return;
        if (isShuffling) return;

        if (!tile.hasOrigin) tile.SaveOrigin();

        if (tray != null && tray.TryAdd(tile))
        {
            RemoveTileFromBoardList(tile);
            RefreshBlockState();
        }
    }

    public void RemoveTileFromBoardList(TileView tile)
    {
        if (!tile) return;
        if (tiles.Remove(tile))
            onBoardChanged?.Invoke();
    }

    public void AddTileBackToBoard(TileView tile)
    {
        if (!tile) return;

        if (!tiles.Contains(tile))
        {
            tiles.Add(tile);
            onBoardChanged?.Invoke();
        }

        RefreshBlockState();
    }

    // =========================
    // Block state
    // =========================

    static bool RectOverlaps(RectTransform a, RectTransform b)
    {
        Vector3[] ca = new Vector3[4];
        Vector3[] cb = new Vector3[4];
        a.GetWorldCorners(ca);
        b.GetWorldCorners(cb);

        Rect ra = WorldCornersToRect(ca);
        Rect rb = WorldCornersToRect(cb);

        return ra.Overlaps(rb);
    }

    static Rect WorldCornersToRect(Vector3[] c)
    {
        float xMin = c[0].x;
        float yMin = c[0].y;
        float xMax = c[2].x;
        float yMax = c[2].y;
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    // =========================
    // Shuffle Item (all layers)
    // =========================

    public bool CanUseShuffleItem()
    {
        if (isShuffling) return false;
        if (tiles.Count <= 1) return false;
        return cachedPiles != null && cachedPiles.Count > 0;
    }

    public void UseShuffleItem()
    {
        if (!CanUseShuffleItem()) return;
        StartCoroutine(ShuffleFX_CR());
    }

    IEnumerator ShuffleFX_CR()
    {
        isShuffling = true;

        // collect remain tiles
        var remain = new List<TileView>(tiles.Count);
        for (int i = 0; i < tiles.Count; i++)
            if (tiles[i]) remain.Add(tiles[i]);

        if (remain.Count <= 1)
        {
            isShuffling = false;
            yield break;
        }

        Vector3 centerWorld = boardRoot.TransformPoint(new Vector3(layoutCenter.x, layoutCenter.y, 0f));

        // cache start world
        var startPos = new Vector3[remain.Count];
        for (int i = 0; i < remain.Count; i++)
            startPos[i] = remain[i].rect.position;

        // detach to fly root
        if (shuffleFlyRoot)
        {
            for (int i = 0; i < remain.Count; i++)
                remain[i].transform.SetParent(shuffleFlyRoot, worldPositionStays: true);
        }

        yield return MoveManyWorldCR(remain, startPos, centerWorld, gatherScale, gatherDuration, gatherEase);

        // build all slots across layers * piles
        int L = layerRoots.Length;
        int P = cachedPiles.Count;

        var slotsAll = new List<(int layer, int pile)>(L * P);
        for (int layer = 0; layer < L; layer++)
            for (int pile = 0; pile < P; pile++)
                slotsAll.Add((layer, pile));

        Shuffle(slotsAll);
        Shuffle(remain);

        var targetParent = new RectTransform[remain.Count];
        var targetAnchored = new Vector2[remain.Count];
        var targetLayer = new int[remain.Count];
        var targetSibling = new int[remain.Count];
        var targetWorld = new Vector3[remain.Count];

        // compute world targets
        for (int i = 0; i < remain.Count; i++)
        {
            var tv = remain[i];
            var s = slotsAll[i];

            int newLayer = Mathf.Clamp(s.layer, 0, L - 1);
            int pileIndex = Mathf.Clamp(s.pile, 0, P - 1);

            targetParent[i] = layerRoots[newLayer];
            targetLayer[i] = newLayer;
            targetAnchored[i] = cachedPiles[pileIndex] + LayerJitter(newLayer, L, layerJitter);
            targetSibling[i] = UnityEngine.Random.Range(0, targetParent[i].childCount + 1);

            // temp parent swap to get accurate world
            tv.transform.SetParent(targetParent[i], worldPositionStays: false);
            tv.rect.anchoredPosition = targetAnchored[i];
            tv.rect.localScale = Vector3.one;
            targetWorld[i] = tv.rect.position;

            if (shuffleFlyRoot)
                tv.transform.SetParent(shuffleFlyRoot, worldPositionStays: true);

            tv.rect.position = centerWorld;
            tv.rect.localScale = Vector3.one * gatherScale;
        }

        yield return MoveManyToTargetsWorldCR(remain, targetWorld, 1f, spreadDuration, spreadEase);

        // finalize
        for (int i = 0; i < remain.Count; i++)
        {
            var tv = remain[i];
            if (!tv) continue;

            tv.transform.SetParent(targetParent[i], worldPositionStays: false);
            tv.layer = targetLayer[i];
            tv.rect.anchoredPosition = targetAnchored[i];
            tv.rect.localScale = Vector3.one;
            tv.rect.SetSiblingIndex(targetSibling[i]);
        }

        RefreshBlockState();
        onBoardChanged?.Invoke();

        isShuffling = false;
    }

    IEnumerator MoveManyWorldCR(List<TileView> list, Vector3[] startWorld, Vector3 endWorld, float endScale, float dur, AnimationCurve ease)
    {
        dur = Mathf.Max(0.01f, dur);
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = ease != null ? ease.Evaluate(k) : k;

            for (int i = 0; i < list.Count; i++)
            {
                var tv = list[i];
                if (!tv) continue;

                tv.rect.position = Vector3.LerpUnclamped(startWorld[i], endWorld, e);
                tv.rect.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * endScale, e);
            }
            yield return null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var tv = list[i];
            if (!tv) continue;

            tv.rect.position = endWorld;
            tv.rect.localScale = Vector3.one * endScale;
        }
    }

    IEnumerator MoveManyToTargetsWorldCR(List<TileView> list, Vector3[] targetWorld, float endScale, float dur, AnimationCurve ease)
    {
        dur = Mathf.Max(0.01f, dur);

        var startWorld = new Vector3[list.Count];
        for (int i = 0; i < list.Count; i++)
            startWorld[i] = list[i] ? list[i].rect.position : Vector3.zero;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = ease != null ? ease.Evaluate(k) : k;

            for (int i = 0; i < list.Count; i++)
            {
                var tv = list[i];
                if (!tv) continue;

                tv.rect.position = Vector3.LerpUnclamped(startWorld[i], targetWorld[i], e);
                tv.rect.localScale = Vector3.LerpUnclamped(tv.rect.localScale, Vector3.one * endScale, e);
            }
            yield return null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var tv = list[i];
            if (!tv) continue;

            tv.rect.position = targetWorld[i];
            tv.rect.localScale = Vector3.one * endScale;
        }
    }

    // =========================
    // Cleanup
    // =========================

    void ClearBoard()
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
            if (tiles[i]) Destroy(tiles[i].gameObject);
        tiles.Clear();
        onBoardChanged?.Invoke();
    }

    void PruneNullTiles()
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
            if (tiles[i] == null) tiles.RemoveAt(i);
    }

    public void RefreshBlockState()
    {
        PruneNullTiles();

        for (int i = 0; i < tiles.Count; i++)
        {
            var a = tiles[i];
            if (!a) continue;

            bool blocked = false;
            for (int j = 0; j < tiles.Count; j++)
            {
                if (i == j) continue;
                var b = tiles[j];
                if (!b) continue;
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
}