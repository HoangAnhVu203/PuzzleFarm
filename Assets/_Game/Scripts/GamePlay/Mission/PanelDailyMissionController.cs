using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PanelDailyMissionController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private DailyMissionItemUI itemPrefab;

    private readonly List<DailyMissionItemUI> items = new();

    void OnEnable()
    {
        var sys = DailyMissionSystem.Instance;
        if (sys == null)
        {
            Debug.LogError("[PanelDailyMissionController] Missing DailyMissionSystem in scene.");
            return;
        }

        sys.EnsureDailyReset();
        sys.onChanged += RefreshAll;

        BuildIfNeeded();
        RefreshAll();
    }

    void OnDisable()
    {
        if (DailyMissionSystem.Instance != null)
            DailyMissionSystem.Instance.onChanged -= RefreshAll;
    }

    void BuildIfNeeded()
    {
        if (items.Count > 0) return;

        var sys = DailyMissionSystem.Instance;

        for (int i = 0; i < sys.missions.Count; i++)
        {
            var it = Instantiate(itemPrefab, contentRoot);
            it.Bind(i);
            items.Add(it);
        }
    }

    void RefreshAll()
    {
        var sys = DailyMissionSystem.Instance;
        if (sys == null) return;

        // 1) Refresh từng item
        foreach (var it in items)
            it.Refresh();

        // 2) Sort: InProgress -> Completed -> Claimed, rồi theo index gốc
        var sorted = items
            .OrderBy(it => (int)sys.GetState(it.Index))
            .ThenBy(it => it.Index)
            .ToList();

        // 3) Apply sibling index để “đẩy xuống dưới”
        for (int i = 0; i < sorted.Count; i++)
            sorted[i].transform.SetSiblingIndex(i);
    }
}