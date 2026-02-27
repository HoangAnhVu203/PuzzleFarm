using UnityEngine;

public class PanelHome : UICanvas
{
    public void PlayBTN()
    {
        GameManager.Instance.BtnPlay();
    }

    public void OpenSpinBTN()
    {
        UIManager.Instance.CloseUIDirectly<PanelHome>();
        UIManager.Instance.OpenUI<PanelSpin>();
    }

    public void OpenReverralBTN()
    {
        UIManager.Instance.OpenUI<PanelReferral>();
    }

    public void OpenDailyMissionBTN()
    {
        UIManager.Instance.OpenUI<PanelDailyMission>();
        UIManager.Instance.CloseUIDirectly<PanelHome>();
    }

    public void OpenReferralbtn()
    {
        UIManager.Instance.OpenUI<PanelReferral>();
    }

    public void OpenloginPanel()
    {
        UIManager.Instance.OpenUI<PanelLogin>();
    }

    public void OpenInvitePanel()
    {
        UIManager.Instance.OpenUI<PanelInvite>();
    }

    public void OpenCustomizePanel()
    {
        UIManager.Instance.OpenUI<PanelCustomize>();
    }

    public void OpenCharacterCard()
    {
        UIManager.Instance.OpenUI<PanelCharacterCard>();
    }
}
