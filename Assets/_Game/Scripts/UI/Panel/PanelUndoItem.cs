using UnityEngine;

public class PanelUndoItem : UICanvas
{
    public void BtnClaim()
    {
        ItemInventory.Instance?.ClaimUndo(1);
        UIManager.Instance.CloseUIDirectly<PanelUndoItem>();
    }

    public void BtnClose()
    {
        UIManager.Instance.CloseUIDirectly<PanelRemoveItem>();
    }
}
