using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelCharacterCard : UICanvas
{
    [Header("UI")]
    [SerializeField] private Text titleTxt;

    [Header("Preview")]
    [SerializeField] private Image previewIcon; 

    [Header("Progress")]
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private Image progressFill; 
    [SerializeField] private Text progressTxt;

    [Header("Unlock")]
    [SerializeField] private Button unlockBtn;
    [SerializeField] private Text unlockLabel;

    [Header("Cards")]
    [SerializeField] private Transform cardsRoot;
    [SerializeField] private CharacterCardSlotUI cardPrefab;

    readonly List<CharacterCardSlotUI> slots = new();

    private string currentId;

    public override void SetUp()
    {
        base.SetUp();

        if (unlockBtn)
        {
            unlockBtn.onClick.RemoveAllListeners();
            unlockBtn.onClick.AddListener(OnClickUnlock);
        }
    }

    void OnEnable()
    {
        if (CharacterSystem.Instance != null)
            CharacterSystem.Instance.onChanged += Refresh;

        // nếu panel mở mà chưa SetCharacter thì thử lấy ViewingCharacterId (nếu bạn có)
        if (string.IsNullOrEmpty(currentId) && CharacterSystem.Instance != null)
        {
            // nếu bạn KHÔNG có ViewingCharacterId thì bỏ 2 dòng này
            currentId = CharacterSystem.Instance.ViewingCharacterId;
        }

        BuildIfNeeded();
        Refresh();
    }

    void OnDisable()
    {
        if (CharacterSystem.Instance != null)
            CharacterSystem.Instance.onChanged -= Refresh;
    }

    public void SetCharacter(string id)
    {
        currentId = id;

        // nếu bạn vẫn muốn lưu id đang xem trong system
        if (CharacterSystem.Instance != null)
            CharacterSystem.Instance.SetViewing(id);

        BuildIfNeeded();
        Refresh();
    }

    void BuildIfNeeded()
    {
        if (slots.Count > 0) return;
        if (!cardsRoot || !cardPrefab) return;

        for (int i = 1; i <= 9; i++)
        {
            var it = Instantiate(cardPrefab, cardsRoot);
            slots.Add(it);
        }
    }

    void Refresh()
    {
        var sys = CharacterSystem.Instance;
        if (sys == null) return;

        if (string.IsNullOrEmpty(currentId))
            return;

        var def = sys.GetDef(currentId);
        if (def == null) return;

        // Title + Preview icon
        if (titleTxt) titleTxt.text = def.displayName;
        if (previewIcon) previewIcon.sprite = def.icon;

        int need = Mathf.Max(1, def.unlockNeed);
        int cur = sys.GetCards(currentId);
        bool unlocked = sys.IsUnlocked(currentId);

        // Progress (ẩn nếu unlocked)
        if (progressRoot) progressRoot.SetActive(unlocked);

        if (!unlocked)
        {
            if (progressTxt) progressTxt.text = $"{cur}/{need}";
            if (progressFill) progressFill.fillAmount = Mathf.Clamp01(cur / (float)need);
        }

        // 9 slots
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
                slots[i].Bind(currentId, i + 1);
        }

        // Unlock button
        if (unlockBtn)
        {
            if (unlocked)
            {
                unlockBtn.gameObject.SetActive(true);
            }
            else
            {
                unlockBtn.gameObject.SetActive(true);

                bool canUnlock = (cur >= need);
                unlockBtn.interactable = canUnlock;

                if (unlockLabel)
                    unlockLabel.text = canUnlock ? "UNLOCK" : "LOCKED";
            }
        }
    }

    void OnClickUnlock()
    {
        var sys = CharacterSystem.Instance;
        if (sys == null) return;

        if (string.IsNullOrEmpty(currentId)) return;

        sys.TryUnlock(currentId);
        Refresh();
    }

    public void CloseBtn()
    {
        UIManager.Instance.CloseUIDirectly<PanelCharacterCard>();
    }
}