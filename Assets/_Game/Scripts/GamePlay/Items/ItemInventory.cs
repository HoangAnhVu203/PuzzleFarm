using System;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    public static ItemInventory Instance { get; private set; }
    public event Action onChanged;

    const string KEY_REMOVE_CLAIMED  = "REMOVE_CLAIMED";
    const string KEY_REMOVE_USES     = "REMOVE_USES";

    const string KEY_UNDO_CLAIMED    = "UNDO_CLAIMED";
    const string KEY_UNDO_USES       = "UNDO_USES";

    const string KEY_SHUFFLE_CLAIMED = "SHUFFLE_CLAIMED";
    const string KEY_SHUFFLE_USES    = "SHUFFLE_USES";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
    }

    // ===== Remove =====
    public bool RemoveClaimed => PlayerPrefs.GetInt(KEY_REMOVE_CLAIMED, 0) == 1;
    public int  RemoveUses    => PlayerPrefs.GetInt(KEY_REMOVE_USES, 0);

    public void ClaimRemove(int addUses = 1)
    {
        PlayerPrefs.SetInt(KEY_REMOVE_CLAIMED, 1);
        PlayerPrefs.SetInt(KEY_REMOVE_USES, Mathf.Max(0, RemoveUses) + Mathf.Max(1, addUses));
        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    public bool CanUseRemove() => RemoveClaimed && RemoveUses > 0;

    public bool ConsumeRemove()
    {
        if (!CanUseRemove()) return false;
        PlayerPrefs.SetInt(KEY_REMOVE_USES, Mathf.Max(0, RemoveUses - 1));
        PlayerPrefs.Save();
        onChanged?.Invoke();
        return true;
    }

    // ===== Undo =====
    public bool UndoClaimed => PlayerPrefs.GetInt(KEY_UNDO_CLAIMED, 0) == 1;
    public int  UndoUses    => PlayerPrefs.GetInt(KEY_UNDO_USES, 0);

    public void ClaimUndo(int addUses = 1)
    {
        PlayerPrefs.SetInt(KEY_UNDO_CLAIMED, 1);
        PlayerPrefs.SetInt(KEY_UNDO_USES, Mathf.Max(0, UndoUses) + Mathf.Max(1, addUses));
        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    public bool CanUseUndo() => UndoClaimed && UndoUses > 0;

    public bool ConsumeUndo()
    {
        if (!CanUseUndo()) return false;
        PlayerPrefs.SetInt(KEY_UNDO_USES, Mathf.Max(0, UndoUses - 1));
        PlayerPrefs.Save();
        onChanged?.Invoke();
        return true;
    }

    // ===== Shuffle =====
    public bool ShuffleClaimed => PlayerPrefs.GetInt(KEY_SHUFFLE_CLAIMED, 0) == 1;
    public int  ShuffleUses    => PlayerPrefs.GetInt(KEY_SHUFFLE_USES, 0);

    public void ClaimShuffle(int addUses = 1)
    {
        PlayerPrefs.SetInt(KEY_SHUFFLE_CLAIMED, 1);
        PlayerPrefs.SetInt(KEY_SHUFFLE_USES, Mathf.Max(0, ShuffleUses) + Mathf.Max(1, addUses));
        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    public bool CanUseShuffle() => ShuffleClaimed && ShuffleUses > 0;

    public bool ConsumeShuffle()
    {
        if (!CanUseShuffle()) return false;
        PlayerPrefs.SetInt(KEY_SHUFFLE_USES, Mathf.Max(0, ShuffleUses - 1));
        PlayerPrefs.Save();
        onChanged?.Invoke();
        return true;
    }

    // ===== Reset all items to default each level =====
    public void ResetAllItemsToDefault()
    {
        PlayerPrefs.SetInt(KEY_REMOVE_CLAIMED, 0);
        PlayerPrefs.SetInt(KEY_REMOVE_USES, 0);

        PlayerPrefs.SetInt(KEY_UNDO_CLAIMED, 0);
        PlayerPrefs.SetInt(KEY_UNDO_USES, 0);

        PlayerPrefs.SetInt(KEY_SHUFFLE_CLAIMED, 0);
        PlayerPrefs.SetInt(KEY_SHUFFLE_USES, 0);

        PlayerPrefs.Save();
        onChanged?.Invoke();
    }
}