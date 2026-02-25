using UnityEngine;
using UnityEngine.UI;

public class ItemButtonUI : MonoBehaviour
{
    public enum ItemType { Remove, Undo, Shuffle }

    [Header("Config")]
    [SerializeField] private ItemType type;

    [Header("Refs")]
    [SerializeField] private Button btn;
    [SerializeField] private Image image;
    [SerializeField] private Text valText; // object "Val" trong ảnh của bạn

    [Header("Gray")]
    [SerializeField] private Color enabledColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.4f);

    void OnEnable()
    {
        Refresh();
        if (ItemInventory.Instance) ItemInventory.Instance.onChanged += Refresh;
    }

    void OnDisable()
    {
        if (ItemInventory.Instance) ItemInventory.Instance.onChanged -= Refresh;
    }

    public void OnClick()
    {
        var inv = ItemInventory.Instance;
        if (inv == null) return;

        // 1) chưa claim -> mở panel claim tương ứng
        if (!IsClaimed(inv))
        {
            OpenClaimPanel();
            return;
        }

        // 2) hết lượt -> không làm gì
        if (!CanUse(inv))
        {
            Refresh();
            return;
        }

        // 3) thực thi item thật
        if (!TryExecuteGameplayAction()) return;

        // 4) trừ lượt
        Consume(inv);

        Refresh();
    }

    // =====================
    // Mapping Inventory
    // =====================
    bool IsClaimed(ItemInventory inv) =>
        type switch
        {
            ItemType.Remove  => inv.RemoveClaimed,
            ItemType.Undo    => inv.UndoClaimed,
            ItemType.Shuffle => inv.ShuffleClaimed,
            _ => false
        };

    int GetUses(ItemInventory inv) =>
        type switch
        {
            ItemType.Remove  => inv.RemoveUses,
            ItemType.Undo    => inv.UndoUses,
            ItemType.Shuffle => inv.ShuffleUses,
            _ => 0
        };

    bool CanUse(ItemInventory inv) =>
        type switch
        {
            ItemType.Remove  => inv.CanUseRemove(),
            ItemType.Undo    => inv.CanUseUndo(),
            ItemType.Shuffle => inv.CanUseShuffle(),
            _ => false
        };

    void Consume(ItemInventory inv)
    {
        switch (type)
        {
            case ItemType.Remove:  inv.ConsumeRemove();  break;
            case ItemType.Undo:    inv.ConsumeUndo();    break;
            case ItemType.Shuffle: inv.ConsumeShuffle(); break;
        }
    }

    // =====================
    // Gameplay actions
    // =====================
    bool TryExecuteGameplayAction()
    {
        var tray = GameContext.Instance?.CurrentTray;
        var board = GameContext.Instance?.CurrentBoard;

        switch (type)
        {
            case ItemType.Remove:
                if (tray == null) return false;
                if (!tray.CanUseRemoveItem()) return false;
                tray.UseRemoveItem();
                return true;

            case ItemType.Undo:
                if (tray == null) return false;
                if (!tray.CanUseUndoItem()) return false;
                tray.UseUndoItem();
                return true;

            case ItemType.Shuffle:
                if (board == null) return false;
                if (!board.CanUseShuffleItem()) return false;
                board.UseShuffleItem();
                return true;
        }
        return false;
    }

    // =====================
    // Claim panel
    // =====================
    void OpenClaimPanel()
    {
        // Bạn có sẵn prefab panel: Panel - RemoveItem / Panel - UndoItem / Panel - ShuffleItem
        switch (type)
        {
            case ItemType.Remove:  UIManager.Instance.OpenUI<PanelRemoveItem>();  break;
            case ItemType.Undo:    UIManager.Instance.OpenUI<PanelUndoItem>();    break;
            case ItemType.Shuffle: UIManager.Instance.OpenUI<PanelShuffleItem>(); break;
        }
    }

    // =====================
    // UI render
    // =====================
    void Refresh()
    {
        var inv = ItemInventory.Instance;
        if (inv == null) return;

        if (!IsClaimed(inv))
        {
            valText.text = "+";
            btn.interactable = true;
            image.color = enabledColor;
            return;
        }

        int uses = GetUses(inv);
        valText.text = uses.ToString();

        if (uses > 0)
        {
            btn.interactable = true;
            image.color = enabledColor;
        }
        else
        {
            btn.interactable = false;
            image.color = disabledColor;
        }
    }
}