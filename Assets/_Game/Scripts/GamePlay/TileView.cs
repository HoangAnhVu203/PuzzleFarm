using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public RectTransform rect;
    public CanvasGroup canvasGroup;
    public Button button;

    [HideInInspector] public TileTypeSO type;
    [HideInInspector] public int layer;
    [HideInInspector] public bool isBlocked;

    // NEW: board ref + click override
    [HideInInspector] public BoardManager board;
    public Action<TileView> onClickOverride;

    // NEW: origin data (board position before moving to tray)
    [HideInInspector] public bool hasOrigin;
    [HideInInspector] public RectTransform originParent;
    [HideInInspector] public Vector2 originAnchoredPos;
    [HideInInspector] public int originLayer;
    [HideInInspector] public int originSiblingIndex;

    [SerializeField] private Image icon;
    [SerializeField] private Image overlayGray;

    public void Init(BoardManager board, TileTypeSO type, int layer)
    {
        this.board = board;
        this.type = type;
        this.layer = layer;

        if (!rect) rect = (RectTransform)transform;

        if (icon && type) icon.sprite = type.sprite;   
        if (icon) icon.enabled = (icon.sprite != null);

        SetBlocked(false);
    }

    public void SaveOrigin()
    {
        // gọi đúng 1 lần khi tile còn đang ở board
        hasOrigin = true;
        originParent = rect.parent as RectTransform;
        originAnchoredPos = rect.anchoredPosition;
        originLayer = layer;
        originSiblingIndex = rect.GetSiblingIndex();
    }

    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;

        if (overlayGray)
            overlayGray.gameObject.SetActive(blocked);

        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = !blocked;
            canvasGroup.interactable = !blocked;
            canvasGroup.alpha = 1f; // không fade nữa
        }

        if (button)
            button.interactable = !blocked;
    }
    public void SetClickable(bool clickable, float alpha = 1f)
    {
        if (overlayGray)
            overlayGray.gameObject.SetActive(!clickable);

        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = clickable;
            canvasGroup.interactable = clickable;
            canvasGroup.alpha = 1f; 
        }

        if (button)
            button.interactable = clickable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBlocked) return;

        if (onClickOverride != null)
        {
            onClickOverride.Invoke(this);
            return;
        }

        board?.OnTileClicked(this);
    }
}