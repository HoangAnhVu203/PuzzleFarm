using UnityEngine;
using UnityEngine.UI;

public class RemoveItemButtonUI : MonoBehaviour
{
    [Header("Refs")]
    public Button button;
    public CanvasGroup canvasGroup;

    [Header("Visuals")]
    public GameObject plusBadge;   // icon dấu +
    public GameObject countRoot;   // parent của text số (để bật/tắt)
    public Text countText;         // hoặc TMP_Text nếu bạn dùng TMP

    [Header("Panel")]
    public GameObject panelRemoveItem; // PanelRemoveItem

    [Header("Use Logic")]
    public TrayManager tray;       // có thể để null -> auto lấy từ GameContext
    public float disabledAlpha = 0.35f;

    void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (ItemInventory.Instance)
            ItemInventory.Instance.onChanged += OnInvChanged;

        RefreshVisual();
    }

    void OnDisable()
    {
        if (ItemInventory.Instance)
            ItemInventory.Instance.onChanged -= OnInvChanged;
    }

    void OnInvChanged(ItemType type, int newCount)
    {
        if (type != ItemType.Remove) return;
        RefreshVisual();
    }

    public void OnClick()
    {
        int count = ItemInventory.Instance ? ItemInventory.Instance.Get(ItemType.Remove) : 0;

        // CHƯA CLAIM -> mở panel
        if (count <= 0)
        {
            if (panelRemoveItem) panelRemoveItem.SetActive(true);
            return;
        }

        // ĐÃ CÓ 1 -> dùng item remove
        if (!tray) tray = GameContext.Instance ? GameContext.Instance.CurrentTray : null;
        if (!tray) return;

        // consume trước, nếu fail thì thôi
        if (!ItemInventory.Instance.Consume(ItemType.Remove, 1))
            return;

        tray.UseRemoveItem();

        // sau khi consume về 0 -> UI tự xám/disable do RefreshVisual (event)
        RefreshVisual();
    }

    void RefreshVisual()
    {
        int count = ItemInventory.Instance ? ItemInventory.Instance.Get(ItemType.Remove) : 0;

        bool has = count > 0;

        // + khi chưa có
        if (plusBadge) plusBadge.SetActive(!has);

        // số khi đã có
        if (countRoot) countRoot.SetActive(has);
        if (countText) countText.text = count.ToString();

        // disable khi hết
        if (button) button.interactable = true; // vẫn cho click để mở panel khi count=0
        // nhưng nếu bạn muốn: hết thì vẫn click để mở panel -> button phải interactable

        // “xám” khi count=0 nhưng vẫn cho click mở panel:
        if (canvasGroup)
        {
            canvasGroup.alpha = has ? 1f : disabledAlpha;
            canvasGroup.blocksRaycasts = true;  // vẫn nhận click để mở panel
            canvasGroup.interactable = true;
        }

        // Nếu bạn muốn: hết item thì KHÔNG click được luôn (không mở panel),
        // thì đổi blocksRaycasts/interactable theo has.
    }

    // optional: cho button khác gọi để đóng panel
    public void ClosePanel()
    {
        if (panelRemoveItem) panelRemoveItem.SetActive(false);
    }
}