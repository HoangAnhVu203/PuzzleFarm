using UnityEngine;

public class PanelInvite : UICanvas
{
    public void CloseUI()
    {
        UIManager.Instance.CloseUIDirectly<PanelInvite>();
    }
}
