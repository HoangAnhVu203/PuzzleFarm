using UnityEngine;

public class PanelRemoveItem : MonoBehaviour
{
    public GameObject root;
    public RemoveItemButtonUI removeButtonUI; 

    public void Show()
    {
        if (root) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
        else gameObject.SetActive(false);
    }

    public void BtnClaim()
    {
        if (!ItemInventory.Instance) return;

        ItemInventory.Instance.Add(ItemType.Remove, 1);

        gameObject.SetActive(false);
        if (removeButtonUI) removeButtonUI.ClosePanel();
    }

    public void BtnClose() => Hide();
}