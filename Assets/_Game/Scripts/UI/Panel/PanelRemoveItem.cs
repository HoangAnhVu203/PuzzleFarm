using UnityEngine;

public class PanelRemoveItem : UICanvas
{
    public void BtnClaim()
    {
        ItemInventory.Instance?.ClaimRemove(addUses: 1);

        UIManager.Instance.CloseUIDirectly<PanelRemoveItem>();
    }

    public void BtnClose()
    {
        UIManager.Instance.CloseUIDirectly<PanelRemoveItem>();
    }
}