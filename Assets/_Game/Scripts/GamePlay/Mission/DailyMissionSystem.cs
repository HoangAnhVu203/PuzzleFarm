using System;
using System.Collections.Generic;
using UnityEngine;

public class DailyMissionSystem : MonoBehaviour
{
    public static DailyMissionSystem Instance { get; private set; }

    const string KEY_DATE = "DAILY_DATE";
    const string KEY_PREFIX = "DAILY_M_"; 

    [Serializable]
    public class MissionDef
    {
        public string id;           // "watch_ads"
        public string title;
        public int target;
        public int rewardCoins;
        public Sprite icon;
        public bool autoCompleteOnClaim; // optional
    }

    public List<MissionDef> missions = new List<MissionDef>(5);

    public event Action onChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);

        EnsureDailyReset();
    }

    public void EnsureDailyReset()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        string saved = PlayerPrefs.GetString(KEY_DATE, "");

        if (saved != today)
        {
            PlayerPrefs.SetString(KEY_DATE, today);

            for (int i = 0; i < missions.Count; i++)
            {
                PlayerPrefs.SetInt(KeyCur(i), 0);
                PlayerPrefs.SetInt(KeyClaim(i), 0);
            }

            PlayerPrefs.Save();
            onChanged?.Invoke();
        }
    }

    string KeyCur(int i) => $"{KEY_PREFIX}{i}_CUR";
    string KeyClaim(int i) => $"{KEY_PREFIX}{i}_CLAIM";

    public int GetCur(int index) => PlayerPrefs.GetInt(KeyCur(index), 0);
    public bool IsClaimed(int index) => PlayerPrefs.GetInt(KeyClaim(index), 0) == 1;

    public void AddProgress(int index, int add)
    {
        EnsureDailyReset();

        var def = missions[index];
        int cur = Mathf.Clamp(GetCur(index) + add, 0, def.target);
        PlayerPrefs.SetInt(KeyCur(index), cur);
        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    public void SetCompleted(int index)
    {
        EnsureDailyReset();

        var def = missions[index];
        PlayerPrefs.SetInt(KeyCur(index), def.target);
        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    public MissionState GetState(int index)
    {
        EnsureDailyReset();

        var def = missions[index];
        if (IsClaimed(index)) return MissionState.Claimed;

        int cur = GetCur(index);
        if (cur >= def.target) return MissionState.Completed;

        return MissionState.InProgress;
    }

    public bool TryClaim(int index)
    {
        EnsureDailyReset();

        if (GetState(index) != MissionState.Completed) return false;

        PlayerPrefs.SetInt(KeyClaim(index), 1);
        PlayerPrefs.Save();

        // TODO: cộng tiền thưởng cho player
        // PlayerWallet.Instance.AddCoins(missions[index].rewardCoins);

        onChanged?.Invoke();
        return true;
    }

    int FindIndexById(string id)
    {
        if (missions == null) return -1;
        for (int i = 0; i < missions.Count; i++)
            if (missions[i] != null && missions[i].id == id) return i;
        return -1;
    }

    public void AddProgressById(string id, int add)
    {
        int idx = FindIndexById(id);
        if (idx < 0) return;
        AddProgress(idx, add);
    }

    public void SetCompletedById(string id)
    {
        int idx = FindIndexById(id);
        if (idx < 0) return;
        SetCompleted(idx);
    }

    public MissionState GetStateById(string id)
    {
        int idx = FindIndexById(id);
        if (idx < 0) return MissionState.InProgress;
        return GetState(idx);
    }

    public bool TryClaimById(string id)
    {
        int idx = FindIndexById(id);
        if (idx < 0) return false;
        return TryClaim(idx);
    }
}