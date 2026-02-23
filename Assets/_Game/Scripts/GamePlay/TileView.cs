using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TileView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image icon;
    public Button button;
    public CanvasGroup canvasGroup;

    [HideInInspector] public TileTypeSO type;
    [HideInInspector] public int layer;
    [HideInInspector] public RectTransform rect;
    [HideInInspector] public bool isBlocked;

    BoardManager board;

    public void Init(BoardManager board, TileTypeSO type, int layer)
    {
        this.board = board;
        this.type = type;
        this.layer = layer;

        rect = (RectTransform)transform;
        if (icon) icon.sprite = type.sprite;

        SetBlocked(false);
    }

    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
        if (button) button.interactable = !blocked;

        // "tile click được thì sáng, không click được thì tối"
        if (canvasGroup)
        {
            canvasGroup.alpha = blocked ? 0.35f : 1f;
            canvasGroup.blocksRaycasts = !blocked;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBlocked) return;
        board.OnTileClicked(this);
    }
}