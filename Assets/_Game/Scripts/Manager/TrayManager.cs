using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrayManager : MonoBehaviour
{
    [Header("Slots")]
    public RectTransform[] slots;

    [Header("Fly")]
    public RectTransform trayFlyRoot;     // NEW: root để bay
    public Canvas canvas;                 // NEW: canvas dùng scaleFactor
    public float flyDuration = 0.25f;
    public AnimationCurve flyEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Rule")]
    public int matchCount = 3;

    [Header("Insert Behavior")]
    public bool smartInsertSameType = true;

    [Header("Clear FX")]
    public float delayBeforeClear = 1.0f;
    public float shrinkDuration = 0.25f;

    readonly List<TileView> inTray = new();
    bool isClearing;

    public bool TryAdd(TileView tile)
    {
        if (isClearing) return false;
        if (inTray.Count >= slots.Length) return false;

        tile.SetBlocked(false);

        int insertIndex;
        if (smartInsertSameType) insertIndex = GetInsertIndex(tile.type);
        else insertIndex = inTray.Count;

        // Insert trước để slot target đã “được đặt chỗ”
        inTray.Insert(insertIndex, tile);

        // Bay xuống đúng slot insertIndex
        StartCoroutine(FlyToSlotAndFinalizeCR(tile, insertIndex));

        return true;
    }

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

    IEnumerator FlyToSlotAndFinalizeCR(TileView tile, int insertIndex)
    {
        // 1) Lấy vị trí start (world) hiện tại của tile trên board
        Vector3 startWorld = tile.rect.position;

        // 2) Đưa tile ra FlyRoot (để nó bay tự do, không bị layout slot ảnh hưởng)
        if (trayFlyRoot) tile.transform.SetParent(trayFlyRoot, worldPositionStays: true);

        // 3) Trước khi bay: dồn tạm các tile khác về đúng slot (trừ tile đang bay)
        LayoutTrayExcept(tile);

        // 4) Tính điểm đến (world) = slot target
        RectTransform targetSlot = slots[insertIndex];
        Vector3 endWorld = targetSlot.position;

        // 5) Tween position theo world
        float t = 0f;
        while (t < flyDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flyDuration);
            float eased = flyEase != null ? flyEase.Evaluate(k) : k;

            tile.rect.position = Vector3.Lerp(startWorld, endWorld, eased);
            yield return null;
        }
        tile.rect.position = endWorld;

        // 6) Snap vào slot cho chuẩn layout
        tile.transform.SetParent(targetSlot, worldPositionStays: false);
        tile.rect.anchoredPosition = Vector2.zero;
        tile.rect.localScale = Vector3.one;

        // 7) Layout lại toàn bộ để đảm bảo thứ tự chuẩn
        LayoutTray();

        // 8) Check match sau khi đã vào tray hoàn chỉnh
        TryTriggerClear(tile.type);
    }

    void LayoutTrayExcept(TileView except)
    {
        for (int i = 0; i < inTray.Count; i++)
        {
            var t = inTray[i];
            if (!t || t == except) continue;

            t.transform.SetParent(slots[i], worldPositionStays: false);
            t.rect.anchoredPosition = Vector2.zero;
            t.rect.localScale = Vector3.one;
        }
    }

    void LayoutTray()
    {
        for (int i = 0; i < inTray.Count; i++)
        {
            var t = inTray[i];
            if (!t) continue;

            t.transform.SetParent(slots[i], worldPositionStays: false);
            t.rect.anchoredPosition = Vector2.zero;
            t.rect.localScale = Vector3.one;
        }
    }

    void TryTriggerClear(TileTypeSO justAddedType)
    {
        if (isClearing) return;

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
        if (isClearing) return;

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
}