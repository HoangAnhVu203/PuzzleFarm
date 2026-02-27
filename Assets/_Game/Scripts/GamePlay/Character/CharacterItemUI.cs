using UnityEngine;
using UnityEngine.UI;

public class CharacterItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Text progressText;
    [SerializeField] private Image progressImgae; // Image type Filled

    [SerializeField] private Button btnUse;
    [SerializeField] private Text btnLabel;

    CharacterDefSO def;

    public string Id => def ? def.id : "";

    public void Bind(CharacterDefSO d)
    {
        def = d;

        if (btnUse)
        {
            btnUse.onClick.RemoveAllListeners();
            btnUse.onClick.AddListener(OnClickUse);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (!def) return;

        var sys = CharacterSystem.Instance;
        if (sys == null) return;

        if (icon) icon.sprite = def.icon;

        bool unlocked = sys.IsUnlocked(def.id);
        bool isUsing = (sys.CurrentUseId == def.id);

        // lock overlay: khóa thì bật overlay, mở khóa thì tắt overlay
        if (lockOverlay) lockOverlay.SetActive(unlocked);

        // ===== Progress =====
        int need = Mathf.Max(1, def.unlockNeed);
        int cur = sys.GetCards(def.id);

        bool showProgress = !unlocked;

        if (progressImgae)
        {
            progressImgae.gameObject.SetActive(showProgress);
            if (showProgress)
                progressImgae.fillAmount = Mathf.Clamp01(cur / (float)need);
        }

        if (progressText)
        {
            progressText.gameObject.SetActive(showProgress);
            if (showProgress)
                progressText.text = $"{cur}/{need}";
        }

        // button text/state
        if (btnLabel)
            btnLabel.text = isUsing ? "IN USE" : "USE";

        if (btnUse)
            btnUse.interactable = unlocked && !isUsing;
    }

    void OnClickUse()
    {
        if (!def) return;

        var sys = CharacterSystem.Instance;
        if (sys == null) return;

        if (!sys.IsUnlocked(def.id)) return;
        sys.SetUse(def.id);
    }

    // Gắn event này vào Button nền item (OnClick)
    public void OnClickOpenCards()
    {
        if (!def) return;

        // chỉ mở 1 lần
        var panel = UIManager.Instance.OpenUI<PanelCharacterCard>();
        panel.SetCharacter(def.id);
    }
}