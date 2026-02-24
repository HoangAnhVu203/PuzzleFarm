using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class TrayManager : MonoBehaviour
{
    [Header("Tray Slots")]
    public RectTransform[] slots;

    [Header("WaitBar Slots")]
    public RectTransform[] waitBarSlots;

    [Header("Fly (Animation)")]
    public RectTransform flyRoot;
    public float flyDuration = 0.22f;
    public AnimationCurve flyEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Rule")]
    public int matchCount = 3;

    [Header("Insert Behavior")]
    public bool smartInsertSameType = true;

    [Header("Remove Item")]
    public int removeMoveCount = 3;

    [Header("Undo Item")]
    public int undoCount = 1; 

    [Header("Clear FX")]
    public float delayBeforeClear = 1.0f;
    public float shrinkDuration = 0.25f;

    readonly List<TileView> inTray = new();
    readonly List<TileView> inWait = new();

    bool isClearing;
    bool isAnimating;

    public event Action onTrayChanged;
    public event Action onTrayFull;
    public bool IsBusy => isClearing || isAnimating;

    public bool IsTrayFull => inTray.Count >= slots.Length;

    // helper cho GameManager
    public void NotifyChanged() => onTrayChanged?.Invoke();

    void PruneNull()
    {
        for (int i = inTray.Count - 1; i >= 0; i--) if (inTray[i] == null) inTray.RemoveAt(i);
        for (int i = inWait.Count - 1; i >= 0; i--) if (inWait[i] == null) inWait.RemoveAt(i);
    }

    public int TrayCount { get { PruneNull(); return inTray.Count; } }
    public int WaitCount { get { PruneNull(); return inWait.Count; } }
    void Start()
    {
        GameContext.Instance?.RegisterTray(this);
    }

    // =========================
    // Public API
    // =========================

    // Board -> Tray (bay xuống)
    public bool TryAdd(TileView tile)
    {
        if (isClearing || isAnimating) return false;
        if (inTray.Count >= slots.Length) return false;

        PrepareAsTray(tile);

        int insertIndex = smartInsertSameType ? GetInsertIndex(tile.type) : inTray.Count;
        inTray.Insert(insertIndex, tile);

        LayoutTrayExcept(tile);

        StartCoroutine(FlyIntoSlotThenFinalizeCR(
            tile: tile,
            targetSlot: slots[insertIndex],
            after: () =>
            {
                LayoutTray();
                TryTriggerClear(tile.type);

                NotifyChanged();
                if (IsTrayFull) onTrayFull?.Invoke();
            }
        ));

        return true;
    }

    // RemoveItem: 3 tile ĐẦU TIÊN của Tray -> WaitBar
    public void UseRemoveItem()
    {
        if (isClearing || isAnimating) return;
        if (waitBarSlots == null || waitBarSlots.Length == 0) return;
        if (inTray.Count == 0) return;

        int freeWait = waitBarSlots.Length - inWait.Count;
        int canMove = Mathf.Min(removeMoveCount, inTray.Count, freeWait);
        if (canMove <= 0) return;

        var moving = new List<TileView>(canMove);
        for (int i = 0; i < canMove; i++)
        {
            moving.Add(inTray[0]);
            inTray.RemoveAt(0);
        }

        LayoutTray();

        StartCoroutine(MoveTrayToWaitBarBatchCR(moving));
    }

    // UndoItem: tile CUỐI CÙNG của Tray -> bay về vị trí cũ trên Board
    public void UseUndoItem()
    {
        if (isClearing || isAnimating) return;
        if (inTray.Count == 0) return;

        // Undo 1 cái (nếu bạn muốn undo nhiều thì loop theo undoCount)
        int times = Mathf.Clamp(undoCount, 1, inTray.Count);

        StartCoroutine(UndoBackBatchCR(times));
    }

    // =========================
    // Batch moves
    // =========================

    IEnumerator MoveTrayToWaitBarBatchCR(List<TileView> moving)
    {
        isAnimating = true;

        for (int i = 0; i < moving.Count; i++)
        {
            var tile = moving[i];
            if (!tile) continue;

            int waitIndex = inWait.Count;
            inWait.Add(tile);

            PrepareAsWait(tile);

            LayoutWaitBarExcept(tile);

            yield return FlyToWorldPosCR(tile, waitBarSlots[waitIndex].position);

            SnapToSlot(tile, waitBarSlots[waitIndex]);
            LayoutWaitBar();
        }

        isAnimating = false;
        NotifyChanged();
    }

    IEnumerator UndoBackBatchCR(int times)
    {
        isAnimating = true;

        for (int k = 0; k < times; k++)
        {
            if (inTray.Count == 0) break;

            // lấy tile CUỐI CÙNG
            int last = inTray.Count - 1;
            var tile = inTray[last];
            inTray.RemoveAt(last);

            // dồn tray ngay để lấp slot
            LayoutTray();

            // undo bay về origin
            yield return UndoBackToBoardCR(tile);
        }

        isAnimating = false;
        NotifyChanged();
    }

    void ReturnFromWaitBar(TileView tile)
    {
        if (!tile) return;
        if (isClearing || isAnimating) return;
        if (inTray.Count >= slots.Length) return;

        int oldWaitIndex = inWait.IndexOf(tile);
        if (oldWaitIndex >= 0) inWait.RemoveAt(oldWaitIndex);
        LayoutWaitBar();

        int insertIndex = smartInsertSameType ? GetInsertIndex(tile.type) : inTray.Count;
        inTray.Insert(insertIndex, tile);

        PrepareAsTray(tile);

        LayoutTrayExcept(tile);

        StartCoroutine(FlyIntoSlotThenFinalizeCR(
            tile: tile,
            targetSlot: slots[insertIndex],
            after: () =>
            {
                LayoutTray();
                TryTriggerClear(tile.type);

                NotifyChanged();
                if (IsTrayFull) onTrayFull?.Invoke();
            }
        ));
    }

    IEnumerator UndoBackToBoardCR(TileView tile)
    {
        if (!tile) yield break;

        // phải có origin
        if (!tile.hasOrigin || tile.originParent == null)
        {
            Debug.LogWarning("[UndoItem] Tile has no origin info (originParent null).");
            yield break;
        }

        // trong lúc bay: không click
        tile.onClickOverride = null;
        tile.isBlocked = false;

        if (tile.canvasGroup)
        {
            tile.canvasGroup.alpha = 1f;
            tile.canvasGroup.blocksRaycasts = false;
            tile.canvasGroup.interactable = false;
        }
        if (tile.button) tile.button.interactable = false;

        // bay về world position của originParent, sau đó snap anchoredPos
        Vector3 endWorld = tile.originParent.position;
        yield return FlyToWorldPosCR(tile, endWorld);

        // snap lại parent/pos gốc
        tile.transform.SetParent(tile.originParent, worldPositionStays: false);
        tile.rect.anchoredPosition = tile.originAnchoredPos;
        tile.rect.localScale = Vector3.one;
        tile.rect.SetSiblingIndex(tile.originSiblingIndex);

        // restore layer
        tile.layer = tile.originLayer;

        // add lại board list + refresh
        tile.board?.AddTileBackToBoard(tile);
        NotifyChanged();
    }

    // =========================
    // States
    // =========================

    void PrepareAsTray(TileView tile)
    {
        tile.onClickOverride = null;
        tile.isBlocked = false;

        // Tray: KHÔNG click, KHÔNG mờ
        if (tile.canvasGroup)
        {
            tile.canvasGroup.alpha = 1f;
            tile.canvasGroup.blocksRaycasts = false;
            tile.canvasGroup.interactable = false;
        }
        if (tile.button) tile.button.interactable = false;
    }

    void PrepareAsWait(TileView tile)
    {
        tile.isBlocked = false;

        // WaitBar: click được để trả về tray
        if (tile.canvasGroup)
        {
            tile.canvasGroup.alpha = 1f;
            tile.canvasGroup.blocksRaycasts = true;
            tile.canvasGroup.interactable = true;
        }
        if (tile.button) tile.button.interactable = true;

        tile.onClickOverride = ReturnFromWaitBar;
    }

    // =========================
    // Layout
    // =========================

    void LayoutTray()
    {
        for (int i = 0; i < inTray.Count; i++)
        {
            var t = inTray[i];
            if (!t) continue;
            SnapToSlot(t, slots[i]);
            PrepareAsTray(t);
        }
    }

    void LayoutTrayExcept(TileView except)
    {
        for (int i = 0; i < inTray.Count; i++)
        {
            var t = inTray[i];
            if (!t || t == except) continue;
            SnapToSlot(t, slots[i]);
            PrepareAsTray(t);
        }
    }

    void LayoutWaitBar()
    {
        for (int i = 0; i < inWait.Count; i++)
        {
            var t = inWait[i];
            if (!t) continue;
            SnapToSlot(t, waitBarSlots[i]);
            PrepareAsWait(t);
        }
    }

    void LayoutWaitBarExcept(TileView except)
    {
        for (int i = 0; i < inWait.Count; i++)
        {
            var t = inWait[i];
            if (!t || t == except) continue;
            SnapToSlot(t, waitBarSlots[i]);
            PrepareAsWait(t);
        }
    }

    void SnapToSlot(TileView tile, RectTransform slot)
    {
        tile.transform.SetParent(slot, worldPositionStays: false);
        tile.rect.anchoredPosition = Vector2.zero;
        tile.rect.localScale = Vector3.one;
    }

    // =========================
    // Fly helpers
    // =========================

    IEnumerator FlyIntoSlotThenFinalizeCR(TileView tile, RectTransform targetSlot, System.Action after)
    {
        isAnimating = true;

        yield return FlyToWorldPosCR(tile, targetSlot.position);

        SnapToSlot(tile, targetSlot);

        isAnimating = false;
        after?.Invoke();
    }

    IEnumerator FlyToWorldPosCR(TileView tile, Vector3 endWorld)
    {
        if (!tile) yield break;

        Vector3 start = tile.rect.position;

        // detach khỏi layout khi bay
        if (flyRoot)
            tile.transform.SetParent(flyRoot, worldPositionStays: true);

        float t = 0f;
        float dur = Mathf.Max(0.01f, flyDuration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = flyEase != null ? flyEase.Evaluate(k) : k;

            tile.rect.position = Vector3.LerpUnclamped(start, endWorld, e);
            yield return null;
        }

        tile.rect.position = endWorld;
    }

    // =========================
    // Smart insert
    // =========================

    int GetInsertIndex(TileTypeSO type)
    {
        int idx = inTray.Count;
        for (int i = inTray.Count - 1; i >= 0; i--)
        {
            if (inTray[i] != null && inTray[i].type == type)
            {
                idx = i + 1;
                break;
            }
        }
        return idx;
    }

    // =========================
    // Match / Clear (ONLY inTray)
    // =========================

    void TryTriggerClear(TileTypeSO justAddedType)
    {
        if (isClearing || isAnimating) return;

        var same = inTray.Where(t => t != null && t.type == justAddedType).ToList();
        if (same.Count < matchCount) return;

        var toClear = new List<TileView>(matchCount);
        for (int i = 0; i < inTray.Count && toClear.Count < matchCount; i++)
            if (inTray[i] != null && inTray[i].type == justAddedType)
                toClear.Add(inTray[i]);

        StartCoroutine(ClearMatchedCR(toClear));
    }

    IEnumerator ClearMatchedCR(List<TileView> toClear)
    {
        isClearing = true;

        yield return new WaitForSeconds(delayBeforeClear);
        yield return StartCoroutine(ShrinkTilesCR(toClear, shrinkDuration));

        for (int i = 0; i < toClear.Count; i++)
        {
            var t = toClear[i];
            if (!t) continue;
            inTray.Remove(t);
            Destroy(t.gameObject);
        }

        LayoutTray();

        isClearing = false;
        NotifyChanged();

        TryChainClear();
    }

    IEnumerator ShrinkTilesCR(List<TileView> tiles, float dur)
    {
        if (dur <= 0f)
        {
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i]) tiles[i].rect.localScale = Vector3.zero;
            yield break;
        }

        var startScales = new Vector3[tiles.Count];
        for (int i = 0; i < tiles.Count; i++)
            startScales[i] = tiles[i] ? tiles[i].rect.localScale : Vector3.one;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            for (int i = 0; i < tiles.Count; i++)
            {
                if (!tiles[i]) continue;
                tiles[i].rect.localScale = Vector3.Lerp(startScales[i], Vector3.zero, k);
            }
            yield return null;
        }

        for (int i = 0; i < tiles.Count; i++)
            if (tiles[i]) tiles[i].rect.localScale = Vector3.zero;
    }

    void TryChainClear()
    {
        if (isClearing || isAnimating) return;

        var groups = inTray
            .Where(t => t != null && t.type != null)
            .GroupBy(t => t.type)
            .FirstOrDefault(g => g.Count() >= matchCount);

        if (groups == null) return;

        var type = groups.Key;
        var toClear = new List<TileView>(matchCount);

        for (int i = 0; i < inTray.Count && toClear.Count < matchCount; i++)
            if (inTray[i] != null && inTray[i].type == type)
                toClear.Add(inTray[i]);

        StartCoroutine(ClearMatchedCR(toClear));
    }

    public bool CanUseRemoveItem()
    {
        if (isClearing || isAnimating) return false;
        if (waitBarSlots == null || waitBarSlots.Length == 0) return false;
        if (inTray.Count == 0) return false;

        int freeWait = waitBarSlots.Length - inWait.Count;
        int canMove = Mathf.Min(removeMoveCount, inTray.Count, freeWait);
        return canMove > 0;
    }

    public bool CanUseUndoItem()
    {
        if (isClearing || isAnimating) return false;
        return inTray.Count > 0; // undo last tray tile
    }

    public bool HasAnyMatchInTray()
    {
        PruneNull();
        if (matchCount <= 1) return inTray.Count > 0;

        // chỉ xét TRONG TRAY theo yêu cầu bạn
        for (int i = 0; i < inTray.Count; i++)
        {
            var t = inTray[i];
            if (!t || !t.type) continue;

            int cnt = 0;
            for (int j = 0; j < inTray.Count; j++)
            {
                var x = inTray[j];
                if (x && x.type == t.type) cnt++;
                if (cnt >= matchCount) return true;
            }
        }
        return false;
    }
    }