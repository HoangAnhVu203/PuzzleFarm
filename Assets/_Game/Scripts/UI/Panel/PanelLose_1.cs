using UnityEngine;

public class PanelLose_1 : UICanvas
{
    public void CloseUI()
    {
        UIManager.Instance.CloseUIDirectly<PanelLose_1>();
        UIManager.Instance.OpenUI<PanelLose_2>();
    }
}
