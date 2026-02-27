using System;
using UnityEngine;

public static class DailyBreakProgress
{
    const string KEY_DATE = "LOSE2_DATE";
    const string KEY_CUR  = "LOSE2_CUR";          
        const string KEY_CLAIM50  = "LOSE2_CLAIM_50";
    const string KEY_CLAIM70  = "LOSE2_CLAIM_70";
    const string KEY_CLAIM100 = "LOSE2_CLAIM_100";

    public const int TARGET = 500;

    static string Today => DateTime.Now.ToString("yyyyMMdd");

    public static void EnsureReset()
    {
        var saved = PlayerPrefs.GetString(KEY_DATE, "");
        if (saved == Today) return;

        PlayerPrefs.SetString(KEY_DATE, Today);
        PlayerPrefs.SetInt(KEY_CUR, 0);
        PlayerPrefs.SetInt(KEY_CLAIM50, 0);
        PlayerPrefs.SetInt(KEY_CLAIM70, 0);
        PlayerPrefs.SetInt(KEY_CLAIM100, 0);
        PlayerPrefs.Save();
    }

    public static int GetCur()
    {
        EnsureReset();
        return PlayerPrefs.GetInt(KEY_CUR, 0);
    }

    public static float GetPercent01()
    {
        EnsureReset();
        return Mathf.Clamp01(GetCur() / (float)TARGET);
    }

    public static int Add(int add)
    {
        EnsureReset();
        int cur = Mathf.Clamp(GetCur() + Mathf.Max(0, add), 0, TARGET);
        PlayerPrefs.SetInt(KEY_CUR, cur);
        PlayerPrefs.Save();
        return cur;
    }

    public static bool CanTrigger50()  { EnsureReset(); return PlayerPrefs.GetInt(KEY_CLAIM50, 0) == 0 && GetPercent01() >= 0.5f; }
    public static bool CanTrigger70()  { EnsureReset(); return PlayerPrefs.GetInt(KEY_CLAIM70, 0) == 0 && GetPercent01() >= 0.7f; }
    public static bool CanTrigger100() { EnsureReset(); return PlayerPrefs.GetInt(KEY_CLAIM100,0) == 0 && GetPercent01() >= 1f; }

    public static void MarkTriggered50()  { EnsureReset(); PlayerPrefs.SetInt(KEY_CLAIM50, 1); PlayerPrefs.Save(); }
    public static void MarkTriggered70()  { EnsureReset(); PlayerPrefs.SetInt(KEY_CLAIM70, 1); PlayerPrefs.Save(); }
    public static void MarkTriggered100() { EnsureReset(); PlayerPrefs.SetInt(KEY_CLAIM100, 1); PlayerPrefs.Save(); }
}