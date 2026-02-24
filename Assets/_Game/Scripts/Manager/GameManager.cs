using System.Collections;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { Home, Loading, Gameplay, Lose }

    [Header("Refs")]
    [SerializeField] private LevelManager levelManager;

    [Header("Loading")]
    [SerializeField] private float loadingSeconds = 0.15f;

    [Header("Win -> Next Level")]
    [SerializeField] private float winNextDelay = 0.25f;

    private GameState state = GameState.Home;

    private BoardManager board;
    private TrayManager tray;

    private bool isTransitioning;

    void Awake()
    {
        if (levelManager)
            levelManager.OnLevelLoaded += HandleLevelLoaded;
    }

    void OnDestroy()
    {
        if (levelManager)
            levelManager.OnLevelLoaded -= HandleLevelLoaded;
    }

    void HandleLevelLoaded(BoardManager b, TrayManager t)
    {
        // bind lại sau khi load xong
        BindCurrentLevelRefs();
    }

    void Start()
    {
        SetState(GameState.Home);
    }

    void Update()
    {
        if (state != GameState.Gameplay) return;
        EvaluateWin();
    }

    // ===== Buttons (hook từ UI) =====
    public void BtnPlay()  => StartCoroutine(LoadAndStartLevelCR(reloadCurrent: true));
    public void BtnRetry() => StartCoroutine(LoadAndStartLevelCR(reloadCurrent: true));
    public void BtnHome()
    {
        isTransitioning = false;
        Unbind();
        levelManager?.UnloadCurrentLevel();

        SetState(GameState.Home);
    }

    // ===== State via UIManager =====
    void SetState(GameState s)
    {
        state = s;
        UIManager.Instance.CloseAll();

        switch (state)
        {
            case GameState.Home:
                UIManager.Instance.OpenUI<PanelHome>();
                break;

            case GameState.Loading:
                UIManager.Instance.OpenUI<PanelLoading>();
                break;

            case GameState.Gameplay:
                UIManager.Instance.OpenUI<PanelGamePlay>();
                break;

            case GameState.Lose:
                UIManager.Instance.OpenUI<PanelLose_1>();
                break;
        }
    }

    // ===== Core flow =====
    IEnumerator LoadAndStartLevelCR(bool reloadCurrent = false, bool loadNext = false)
    {
        isTransitioning = true;
        SetState(GameState.Loading);

        if (!levelManager)
        {
            Debug.LogError("[GameManager] Missing LevelManager.");
            yield break;
        }

        if (loadNext) levelManager.LoadNextLevel();
        else if (reloadCurrent) levelManager.ReloadLevel();
        else levelManager.ReloadLevel();

        yield return new WaitUntil(() =>
            levelManager.CurrentBoard != null &&
            levelManager.CurrentTray  != null
        );

        BindCurrentLevelRefs();

        if (loadingSeconds > 0f) yield return new WaitForSeconds(loadingSeconds);

        SetState(GameState.Gameplay);
        isTransitioning = false;

        EvaluateWin();
    }

    void BindCurrentLevelRefs()
    {
        Unbind();

        board = levelManager ? levelManager.CurrentBoard : null;
        tray  = levelManager ? levelManager.CurrentTray  : null;

        if (!board || !tray)
        {
            Debug.LogWarning($"[GameManager] Bind refs failed. board={board} tray={tray}");
            return;
        }

        board.onBoardChanged += OnAnyChanged;
        tray.onTrayChanged   += OnAnyChanged;
        tray.onTrayFull      += OnTrayFull;
    }

    void Unbind()
    {
        if (board) board.onBoardChanged -= OnAnyChanged;
        if (tray)
        {
            tray.onTrayChanged -= OnAnyChanged;
            tray.onTrayFull -= OnTrayFull;
        }

        board = null;
        tray  = null;
    }

    void OnAnyChanged()
    {
        if (state != GameState.Gameplay) return;
        EvaluateWin();
    }

    // ===== LOSE rule =====
    void OnTrayFull()
    {
        if (state != GameState.Gameplay) return;
        if (isTransitioning) return;
        if (!tray) return;

        // Nếu đang có clear/anim thì đừng kết luận lose ngay
        if (tray.IsBusy) return;

        // Rule bạn yêu cầu: tray full + trong tray không có 3 giống nhau => Lose
        if (!tray.HasAnyMatchInTray())
            TriggerLose();
    }

    public void TriggerLose()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        SetState(GameState.Lose);
    }

    // ===== WIN rule =====
    void EvaluateWin()
    {
        if (isTransitioning) return;
        if (!board || !tray) return;

        // WIN: board + tray + wait rỗng
        if (board.BoardCount == 0 && tray.TrayCount == 0 && tray.WaitCount == 0)
        {
            TriggerWinImmediateNext();
        }
    }

    void TriggerWinImmediateNext()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(NextLevelAfterDelayCR());
    }

    IEnumerator NextLevelAfterDelayCR()
    {
        if (winNextDelay > 0f) yield return new WaitForSeconds(winNextDelay);
        yield return LoadAndStartLevelCR(loadNext: true);
    }
}