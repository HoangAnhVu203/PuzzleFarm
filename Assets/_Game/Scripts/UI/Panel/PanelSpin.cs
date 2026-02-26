using UnityEngine;

public class PanelSpin : UICanvas
{
    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<PanelSpin>();
        UIManager.Instance.OpenUI<PanelHome>();
    }
}
