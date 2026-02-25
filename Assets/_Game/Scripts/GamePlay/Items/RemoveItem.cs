using UnityEngine;

public class RemoveItem : MonoBehaviour, IGameItem
{
    public bool CanUse()
    {
        var inv = ItemInventory.Instance;
        var tray = GameContext.Instance?.CurrentTray;
        if (inv == null || tray == null) return false;

        return inv.CanUseRemove() && tray.CanUseRemoveItem();
    }

    public void Use()
    {
        if (!CanUse()) return;

        var tray = GameContext.Instance.CurrentTray;
        tray.UseRemoveItem();

        ItemInventory.Instance.ConsumeRemove();
    }
}