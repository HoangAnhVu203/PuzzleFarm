using System.Collections.Generic;
using UnityEngine;

public class PanelCustomize : UICanvas
{
    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private CharacterItemUI itemPrefab;

    readonly List<CharacterItemUI> items = new();

    public override void Open()
    {
        base.Open();
        BuildIfNeeded();
        RefreshAll();
        CharacterSystem.Instance.onChanged += RefreshAll;
    }

    public override void CloseDirectly()
    {
        if (CharacterSystem.Instance != null)
            CharacterSystem.Instance.onChanged -= RefreshAll;
        base.CloseDirectly();
    }

    void BuildIfNeeded()
    {
        if (items.Count > 0) return;

        var sys = CharacterSystem.Instance;
        if (sys == null) return;

        var list = sys.Characters;
        for (int i = 0; i < list.Count; i++)
        {
            var def = list[i];
            if (!def) continue;

            var it = Instantiate(itemPrefab, contentRoot);
            it.Bind(def);
            items.Add(it);
        }
    }

    void RefreshAll()
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i]) items[i].Refresh();
    }

    public void CloseBtn()
    {
        UIManager.Instance.CloseUIDirectly<PanelCustomize>();
    }
}