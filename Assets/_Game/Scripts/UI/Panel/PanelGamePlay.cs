using UnityEngine;

public class PanelGamePlay : UICanvas
{
    public void OpenSetting()
    {
        UIManager.Instance.OpenUI<PanelSetting>();
        
    }
}
