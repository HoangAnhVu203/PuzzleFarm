using UnityEngine;

public class PanelWarning : UICanvas
{
    public enum ConfirmMode { Replay, ReturnHome }

    private ConfirmMode mode;

    public void Setup(ConfirmMode m)
    {
        mode = m;
    }

    public void KeepGoingbtn()
    {
        UIManager.Instance.CloseUIDirectly<PanelWarning>();
    }

    public void Confirmbtn()
    {
        UIManager.Instance.CloseUIDirectly<PanelWarning>();

        var gm = FindObjectOfType<GameManager>();
        if (gm == null) return;

        if (mode == ConfirmMode.Replay)
        {
            gm.BtnRetry();   
        }
        else 
        {
            gm.ReturnToHome();
        }
    }
}