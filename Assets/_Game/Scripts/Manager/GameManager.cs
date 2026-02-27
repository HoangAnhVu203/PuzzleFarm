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

    [Header("Ads Rules")]
    [SerializeField] private int clearsPerAds1 = 6;
    private int clearComboCount;

    [SerializeField] private float ads2IntervalSeconds = 120f;
    private Coroutine ads2CR;

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
        DailyMissionSystem.Instance?.SetCompletedById(DailyMissionId.UsePuzzleFarm);
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
        HandleAds2LoopByState(s);
    }

    // ===== Core flow =====
    IEnumerator LoadAndStartLevelCR(bool reloadCurrent = false, bool loadNext = false)
    {
        isTransitioning = true;

        // 1) Đóng/bỏ bind level cũ để tránh event bắn nhầm
        Unbind();
        clearComboCount = 0;

        // 2) UI Loading
        SetState(GameState.Loading);

        if (!levelManager)
        {
            Debug.LogError("[GameManager] Missing LevelManager.");
            isTransitioning = false;
            yield break;
        }

        // 3) Reset item/inventory mỗi lần vào level (retry hay next đều reset)
        ItemInventory.Instance?.ResetAllItemsToDefault();

        // 4) Kick load level (async)
        if (loadNext) levelManager.LoadNextLevel();
        else levelManager.ReloadLevel();

        // 5) Chờ level load xong thật sự (board/tray != null)
        yield return new WaitUntil(() =>
            levelManager.CurrentBoard != null &&
            levelManager.CurrentTray  != null
        );

        // (khuyến nghị) chờ thêm 1 frame để Start/Awake của level chạy ổn định
        yield return null;

        // 6) Bind refs mới
        BindCurrentLevelRefs();

        // 7) Loading delay
        if (loadingSeconds > 0f)
            yield return new WaitForSeconds(loadingSeconds);

        // 8) Vào gameplay
        SetState(GameState.Gameplay);
        isTransitioning = false;

        // 9) Check win ngay (phòng level rỗng)
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

        tray.onTilesCleared += OnTilesCleared;
    }

    void Unbind()
    {
        if (board) board.onBoardChanged -= OnAnyChanged;
        if (tray)
        {
            tray.onTrayChanged -= OnAnyChanged;
            tray.onTrayFull -= OnTrayFull;
            tray.onTilesCleared -= OnTilesCleared;
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

    public void ReturnToHome()
    {
        isTransitioning = false;

        // 1) Unbind event level cũ
        Unbind();

        // 2) Hủy level hiện tại
        if (levelManager != null)
            levelManager.UnloadCurrentLevel();

        // 3) Reset item nếu cần
        ItemInventory.Instance?.ResetAllItemsToDefault();

        // 4) Mở Home UI
        SetState(GameState.Home);
    }
    
    void OnTilesCleared(int count)
    {
        // mission phá 300 khối
        DailyMissionSystem.Instance?.AddProgressById(DailyMissionId.Break300, count);
        DailyBreakProgress.Add(count);
        
        // Ads1: mỗi khi clear 3 tile => +1 combo
        if (count >= 3)
        {
            clearComboCount++;

            if (clearComboCount % clearsPerAds1 == 0)
            {
                ShowAds1();
            }
        }
    }

    void ShowAds1()
    {
        if (state != GameState.Gameplay) return;
        if (isTransitioning) return;
        if (IsAnyAdsOpen()) return;

        UIManager.Instance.OpenUI<PanelAds1>();
    }

    void HandleAds2LoopByState(GameState s)
    {
        if (s == GameState.Gameplay)
        {
            if (ads2CR == null)
                ads2CR = StartCoroutine(Ads2LoopCR());
        }
        else
        {
            if (ads2CR != null)
            {
                StopCoroutine(ads2CR);
                ads2CR = null;
            }
        }
        if (IsAnyAdsOpen()) return;
    }

    IEnumerator Ads2LoopCR()
    {
        while (state == GameState.Gameplay)
        {
            yield return new WaitForSeconds(ads2IntervalSeconds);

            if (state != GameState.Gameplay) continue;
            if (isTransitioning) continue;
            

            UIManager.Instance.OpenUI<PanelAds2>(); 
        }

        ads2CR = null;
    }

    bool IsAnyAdsOpen()
    {
        return UIManager.Instance.IsUIOpened<PanelAds1>() || UIManager.Instance.IsUIOpened<PanelAds2>();
    }
}