using UnityEngine;

public class PanelDailyMission : UICanvas
{
    public void CloseBTN()
    {
        UIManager.Instance.CloseUIDirectly<PanelDailyMission>();
        UIManager.Instance.OpenUI<PanelHome>();
    }
}
