using UnityEngine;

public static class CharacterCardSave
{
    const string KEY_MASK_PREFIX = "CHAR_CARD_MASK_"; // + id
    const string KEY_UNLOCK_PREFIX = "CHAR_UNLOCK_";  // + id
    const string KEY_USE = "CHAR_USE_ID";

    static string KeyMask(string id) => KEY_MASK_PREFIX + id;
    static string KeyUnlock(string id) => KEY_UNLOCK_PREFIX + id;

    public static int GetMask(string id) => PlayerPrefs.GetInt(KeyMask(id), 0);

    public static bool HasCard(string id, int cardIndex01to09)
    {
        int i = Mathf.Clamp(cardIndex01to09, 1, 9) - 1;
        int mask = GetMask(id);
        return (mask & (1 << i)) != 0;
    }

    public static void AddCard(string id, int cardIndex01to09)
    {
        int i = Mathf.Clamp(cardIndex01to09, 1, 9) - 1;
        int mask = GetMask(id);
        mask |= (1 << i);
        PlayerPrefs.SetInt(KeyMask(id), mask);
        PlayerPrefs.Save();
    }

    public static int CountCards(string id)
    {
        int mask = GetMask(id);
        int cnt = 0;
        for (int i = 0; i < 9; i++)
            if ((mask & (1 << i)) != 0) cnt++;
        return cnt;
    }

    public static bool IsUnlocked(string id) => PlayerPrefs.GetInt(KeyUnlock(id), 0) == 1;
    public static void SetUnlocked(string id, bool v)
    {
        PlayerPrefs.SetInt(KeyUnlock(id), v ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static string GetUsing() => PlayerPrefs.GetString(KEY_USE, "");
    public static void SetUsing(string id)
    {
        PlayerPrefs.SetString(KEY_USE, id);
        PlayerPrefs.Save();
    }
}