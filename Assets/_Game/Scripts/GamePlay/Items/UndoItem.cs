using UnityEngine;

public class UndoItem : MonoBehaviour
{
    public void Use()
    {
        var tray = GameContext.Instance?.CurrentTray;
        if (tray == null) return;

        if (tray.CanUseUndoItem())
            tray.UseUndoItem();
    }
}
