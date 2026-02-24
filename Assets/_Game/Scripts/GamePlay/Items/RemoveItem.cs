using UnityEngine;

public class RemoveItem : MonoBehaviour
{
    public void Use()
    {
        var tray = GameContext.Instance?.CurrentTray;
        if (tray == null) return;

        if (tray.CanUseRemoveItem())
            tray.UseRemoveItem();
    }
}