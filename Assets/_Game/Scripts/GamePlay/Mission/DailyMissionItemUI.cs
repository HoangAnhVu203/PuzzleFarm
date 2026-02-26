using UnityEngine;
using UnityEngine.UI;

public class DailyMissionItemUI : MonoBehaviour
{
    [Header("UI")]
    public Text txtTitle;
    public Text txtProgress;
    public Image progressFill;
    public Text txtReward;
    public Button btnAction;
    public Text txtBtnLabel;
    public Image iconImg;

    [Header("Button Visual")]
    [SerializeField] private Image btnBg;              
    [SerializeField] private Color goColor = Color.yellow;
    [SerializeField] private Color getColor = Color.green;
    int index;
    public int Index => index;

    public void Bind(int idx)
    {
        index = idx;
        Refresh();
    }

    public void Refresh()
    {
        var sys = DailyMissionSystem.Instance;
        var def = sys.missions[Index];

        int cur = sys.GetCur(Index);
        int target = Mathf.Max(1, def.target);

        if (txtTitle) txtTitle.text = def.title;
        if (txtProgress) txtProgress.text = $"{cur}/{target}";
        if (progressFill) progressFill.fillAmount = Mathf.Clamp01(cur / (float)target);

        if (iconImg) iconImg.sprite = def.icon;

        if (txtReward) txtReward.text = FormatShort(def.rewardCoins);

        var state = sys.GetState(Index);

        switch (state)
        {
            case MissionState.InProgress:
                SetBtn("Go", show: true, interactable: true, bgColor: goColor);
                break;

            case MissionState.Completed:
                SetBtn("Get", show: true, interactable: true, bgColor: getColor); 
                break;

            case MissionState.Claimed:
                SetBtn("", show: false, interactable: false, bgColor: goColor);   
                break;
        }
    }

    void SetBtn(string label, bool show, bool interactable, Color bgColor)
    {
        if (btnAction) btnAction.gameObject.SetActive(show);

        if (!show) return; // Claimed -> ẩn luôn

        if (txtBtnLabel) txtBtnLabel.text = label;
        if (btnAction) btnAction.interactable = interactable;

        if (btnBg) btnBg.color = bgColor;
    }

    public void OnClickAction()
    {
        var sys = DailyMissionSystem.Instance;
        var def = sys.missions[Index];     
        var state = sys.GetState(Index);

        if (state == MissionState.InProgress)
        {
            if (def.id == DailyMissionId.WatchAds)
            {
                sys.AddProgressById(def.id, 1);
                return;
            }
            return;
        }

        if (state == MissionState.Completed)
        {
            sys.TryClaimById(def.id);
        }
    }

    static string FormatShort(int value)
    {
        if (value < 1000) return value.ToString();

        // K
        if (value < 1_000_000)
        {
            float k = value / 1000f;

            // nếu là số nguyên như 30000 -> 30k
            if (value % 1000 == 0) return $"đ {(int)k}k";

            // 1500 -> 1.5k (1 chữ số thập phân)
            return $"đ {k:0.#}k";
        }

        // M
        float m = value / 1_000_000f;
        if (value % 1_000_000 == 0) return $"đ {(int)m}M";
        return $"đ {m:0.#}M";
    }

    
}