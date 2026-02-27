using UnityEngine;

public class PanelAds1 : UICanvas
{
    public void CloseUI()
    {
        UIManager.Instance.CloseUIDirectly<PanelAds1>();
    }

    public void ClaimCoinandCash()
    {
        UIManager.Instance.CloseUIDirectly<PanelAds1>();
        //TODO: Add Coin and Cash
    }

    public void ClaimCoin()
    {
        UIManager.Instance.CloseUIDirectly<PanelAds1>();
        //TODO: Add Coin
    }
}
