using UnityEngine;

public class PanelLogin : UICanvas
{
    public void CloseUI()
    {
        UIManager.Instance.CloseUIDirectly<PanelLogin>();
    }
}