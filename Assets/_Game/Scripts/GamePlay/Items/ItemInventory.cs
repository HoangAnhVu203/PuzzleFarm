using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    public static ItemInventory Instance { get; private set; }

    private readonly Dictionary<ItemType, int> counts = new();
    public event Action<ItemType, int> onChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int Get(ItemType type) => counts.TryGetValue(type, out var v) ? v : 0;

    public void Add(ItemType type, int amount)
    {
        if (amount <= 0) return;
        int nv = Get(type) + amount;
        counts[type] = nv;
        onChanged?.Invoke(type, nv);
    }

    public bool Consume(ItemType type, int amount = 1)
    {
        int cur = Get(type);
        if (cur < amount) return false;

        int nv = cur - amount;
        counts[type] = nv;
        onChanged?.Invoke(type, nv);
        return true;
    }
}
