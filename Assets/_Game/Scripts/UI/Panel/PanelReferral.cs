using UnityEngine;

public class PanelReferral : UICanvas
{
    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<PanelReferral>();
        UIManager.Instance.OpenUI<PanelHome>();
    }
}