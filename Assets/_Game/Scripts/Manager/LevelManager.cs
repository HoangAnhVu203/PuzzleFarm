using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Levels (drag prefabs here in order)")]
    [SerializeField] private List<GameObject> levelPrefabs = new();

    [Header("Spawn Root (under Canvas)")]
    [SerializeField] private RectTransform levelRoot;

    [Header("Start")]
    [SerializeField] private int startIndex = 0;
    [SerializeField] private bool autoLoadOnStart = false;

    [Header("Options")]
    [SerializeField] private bool destroyOnUnload = true;
    [SerializeField] private bool clampIndex = true;

    private int currentIndex = -1;
    private GameObject currentInstance;

    private BoardManager currentBoard;
    private TrayManager currentTray;

    public int CurrentIndex => currentIndex;
    public int LevelCount => levelPrefabs != null ? levelPrefabs.Count : 0;

    public BoardManager CurrentBoard => currentBoard;
    public TrayManager CurrentTray => currentTray;

    public event Action<BoardManager, TrayManager> OnLevelLoaded;

    void Start()
    {
        if (autoLoadOnStart)
            LoadLevel(startIndex);
    }

    // ========= PUBLIC API =========
    public void LoadLevel(int index) => StartCoroutine(LoadLevelCR(index));

    public void ReloadLevel()
    {
        if (currentIndex < 0) LoadLevel(startIndex);
        else LoadLevel(currentIndex);
    }

    public void LoadNextLevel()
    {
        if (LevelCount == 0) return;

        int next = currentIndex + 1;
        if (next >= LevelCount) next = 0; // loop
        LoadLevel(next);
    }

    // ========= CORE =========
    IEnumerator LoadLevelCR(int index)
    {
        if (levelPrefabs == null || levelPrefabs.Count == 0)
        {
            Debug.LogError("[LevelManager] levelPrefabs is empty.");
            yield break;
        }

        if (clampIndex) index = Mathf.Clamp(index, 0, levelPrefabs.Count - 1);
        if (index < 0 || index >= levelPrefabs.Count)
        {
            Debug.LogError($"[LevelManager] invalid index {index}.");
            yield break;
        }

        // unload current
        UnloadCurrentLevel();

        // chờ 1 frame để Destroy() xong hẳn (tránh ref/Start timing lộn)
        yield return null;

        currentIndex = index;

        var prefab = levelPrefabs[currentIndex];
        if (!prefab)
        {
            Debug.LogError($"[LevelManager] levelPrefabs[{currentIndex}] is null.");
            yield break;
        }

        var parent = levelRoot ? levelRoot : (RectTransform)transform;
        currentInstance = Instantiate(prefab, parent);

        // find refs (include inactive)
        currentBoard = currentInstance.GetComponentInChildren<BoardManager>(true);
        currentTray  = currentInstance.GetComponentInChildren<TrayManager>(true);

        if (!currentBoard || !currentTray)
        {
            Debug.LogError($"[LevelManager] Missing Board/Tray in prefab {prefab.name}. board={currentBoard} tray={currentTray}");
            yield break;
        }

        // register context ONLY here
        if (GameContext.Instance)
        {
            GameContext.Instance.RegisterBoard(currentBoard);
            GameContext.Instance.RegisterTray(currentTray);
        }

        // nếu BoardManager/TrayManager không build trong Start, bạn có thể chủ động:
        // currentBoard.BuildBoard();

        // chờ thêm 1 frame để mọi Start/Awake trong prefab chạy xong
        yield return null;

        OnLevelLoaded?.Invoke(currentBoard, currentTray);

        Debug.Log($"[LevelManager] Loaded level index {currentIndex} ({prefab.name})");
    }

    public void UnloadCurrentLevel()
    {
        // clear context first
        if (GameContext.Instance)
        {
            GameContext.Instance.RegisterBoard(null);
            GameContext.Instance.RegisterTray(null);
        }

        currentBoard = null;
        currentTray  = null;

        if (currentInstance)
        {
            if (destroyOnUnload) Destroy(currentInstance);
            else currentInstance.SetActive(false);

            currentInstance = null;
        }
    }
}