using UnityEngine;

public class ShuffleItem : MonoBehaviour
{
    public void Use()
    {
        var board = GameContext.Instance?.CurrentBoard;
        if (board == null) return;

        if (board.CanUseShuffleItem())
            board.UseShuffleItem();
    }
}