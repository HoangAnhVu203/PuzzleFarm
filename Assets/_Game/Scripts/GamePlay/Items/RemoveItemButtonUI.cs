// using UnityEngine;
// using UnityEngine.UI;

// public class RemoveItemButtonUI : MonoBehaviour
// {
//     [Header("Refs")]
//     [SerializeField] private Button btn;
//     [SerializeField] private Image image;
//     [SerializeField] private Text valText;

//     private void OnEnable()
//     {
//         Refresh();
//         ItemInventory.Instance.onChanged += Refresh;
//     }

//     private void OnDisable()
//     {
//         if (ItemInventory.Instance != null)
//             ItemInventory.Instance.onChanged -= Refresh;
//     }

//     public void OnClick()
//     {
//         var inv = ItemInventory.Instance;

//         // chưa claim → mở panel claim
//         if (!inv.RemoveClaimed)
//         {
//             UIManager.Instance.OpenUI<PanelRemoveItem>();
//             return;
//         }

//         // đã claim nhưng hết lượt
//         if (!inv.CanUseRemoveItem())
//             return;

//         var tray = GameContext.Instance?.CurrentTray;
//         if (tray == null) return;
//         if (!tray.CanUseRemoveItem()) return;

//         tray.UseRemoveItem();
//         inv.ConsumeRemoveUse();

//         Refresh();
//     }

//     void Refresh()
//     {
//         var inv = ItemInventory.Instance;

//         if (!inv.RemoveClaimed)
//         {
//             valText.text = "+";
//             btn.interactable = true;
//             image.color = Color.white;
//             return;
//         }

//         int uses = inv.RemoveUses;

//         valText.text = uses.ToString();

//         if (uses > 0)
//         {
//             btn.interactable = true;
//             image.color = Color.white;
//         }
//         else
//         {
//             btn.interactable = false;
//             image.color = new Color(1f, 1f, 1f, 0.4f); // xám
//         }
//     }
// }