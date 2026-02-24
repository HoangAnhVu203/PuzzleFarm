using UnityEngine;

public class GameContext : MonoBehaviour
{
    public static GameContext Instance { get; private set; }

    public BoardManager CurrentBoard { get; private set; }
    public TrayManager CurrentTray { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterBoard(BoardManager board)
    {
        CurrentBoard = board;
    }

    public void RegisterTray(TrayManager tray)
    {
        CurrentTray = tray;
    }
}