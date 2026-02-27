using UnityEngine;
using UnityEngine.UI;

public class CharacterCardSlotUI : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Text codeText;

    int cardIndex;
    string charId;

    public void Bind(string characterId, int index01to09)
    {
        charId = characterId;
        cardIndex = index01to09;
        Refresh();
    }

    public void Refresh()
    {
        var sys = CharacterSystem.Instance;
        var def = sys != null ? sys.GetDef(charId) : null;
        if (sys == null || def == null) return;

        bool owned = sys.HasCard(charId, cardIndex);

        if (cardImage && def.cardSprites != null && def.cardSprites.Length >= 9)
            cardImage.sprite = def.cardSprites[cardIndex - 1];

        if (lockedOverlay) lockedOverlay.SetActive(!owned);
        if (cardImage) cardImage.enabled = owned;
    }
}