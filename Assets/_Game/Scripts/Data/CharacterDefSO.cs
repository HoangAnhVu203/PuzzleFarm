using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Def")]
public class CharacterDefSO : ScriptableObject
{
    public string id;              
    public string displayName;
    public Sprite icon;            
    public int unlockNeed = 9;
    public Sprite[] cardSprites = new Sprite[9];
}