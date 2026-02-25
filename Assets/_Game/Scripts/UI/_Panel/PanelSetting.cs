using UnityEngine;

public class PanelSetting : UICanvas
{
    public void CloseUI()
    {
        UIManager.Instance.CloseUIDirectly<PanelSetting>();
    }

    public void OnClickReplay()
    {
        UIManager.Instance.CloseUIDirectly<PanelSetting>();

        var w = UIManager.Instance.OpenUI<PanelWarning>();
        w.Setup(PanelWarning.ConfirmMode.Replay);
    }

    public void OnClickReturnHome()
    {
        UIManager.Instance.CloseUIDirectly<PanelSetting>();

        var w = UIManager.Instance.OpenUI<PanelWarning>();
        w.Setup(PanelWarning.ConfirmMode.ReturnHome);
    }
}