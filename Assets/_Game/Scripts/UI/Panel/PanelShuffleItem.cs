using UnityEngine;

public class PanelShuffleItem : UICanvas
{
    public void BtnClaim()
    {
        ItemInventory.Instance?.ClaimShuffle(1);
        UIManager.Instance.CloseUIDirectly<PanelShuffleItem>();
    }

    public void BtnClose()
    {
        UIManager.Instance.CloseUIDirectly<PanelRemoveItem>();
    }
}
