using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSystem : MonoBehaviour
{
    public static CharacterSystem Instance { get; private set; }

    [Header("Defs")]
    [SerializeField] private List<CharacterDefSO> characters = new();
    [SerializeField] private int defaultIndex = 0;

    // ===== PlayerPrefs Keys =====
    const string KEY_USE = "CHAR_USE_ID";

    // NEW (bitmask for 9 cards): 0..511
    const string KEY_CARD_MASK_PREFIX = "CHAR_CARD_MASK_";   // + id

    // Unlock flag
    const string KEY_UNLOCK_PREFIX = "CHAR_UNLOCK_";         // + id

    // OPTIONAL: legacy total cards (your old system)
    const string KEY_CARD_PREFIX_LEGACY = "CHAR_CARD_";      // + id

    public event Action onChanged;

    public IReadOnlyList<CharacterDefSO> Characters => characters;
    public string CurrentUseId => PlayerPrefs.GetString(KEY_USE, GetDefaultIdSafe());

    /// Character đang được xem trong PanelCharacterCard
    public string ViewingCharacterId { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureDefaultUnlocked();
        // Nếu bạn muốn migrate dữ liệu cũ -> mới, bật dòng dưới:
        // MigrateLegacyTotalsToMaskIfNeeded();
    }

    // =========================
    // Default
    // =========================
    string GetDefaultIdSafe()
    {
        if (characters == null || characters.Count == 0) return "";
        int idx = Mathf.Clamp(defaultIndex, 0, characters.Count - 1);
        return characters[idx] ? characters[idx].id : "";
    }

    void EnsureDefaultUnlocked()
    {
        if (characters == null || characters.Count == 0) return;

        int idx = Mathf.Clamp(defaultIndex, 0, characters.Count - 1);
        var def0 = characters[idx];
        if (!def0) return;

        SetUnlocked(def0.id, true);

        if (string.IsNullOrEmpty(PlayerPrefs.GetString(KEY_USE, "")))
            PlayerPrefs.SetString(KEY_USE, def0.id);

        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    // =========================
    // Keys
    // =========================
    string KeyMask(string id) => $"{KEY_CARD_MASK_PREFIX}{id}";
    string KeyUnlock(string id) => $"{KEY_UNLOCK_PREFIX}{id}";
    string KeyLegacyTotal(string id) => $"{KEY_CARD_PREFIX_LEGACY}{id}";

    // =========================
    // Public Query
    // =========================
    public CharacterDefSO GetDef(string id)
    {
        if (characters == null) return null;
        for (int i = 0; i < characters.Count; i++)
            if (characters[i] && characters[i].id == id) return characters[i];
        return null;
    }

    public bool IsUnlocked(string id) => PlayerPrefs.GetInt(KeyUnlock(id), 0) == 1;

    void SetUnlocked(string id, bool unlocked)
    {
        PlayerPrefs.SetInt(KeyUnlock(id), unlocked ? 1 : 0);
    }

    /// Bitmask 9 cards (0..511)
    public int GetCardMask(string id) => PlayerPrefs.GetInt(KeyMask(id), 0);

    public bool HasCard(string id, int cardIndex01to09)
    {
        int i = Mathf.Clamp(cardIndex01to09, 1, 9) - 1;
        int mask = GetCardMask(id);
        return (mask & (1 << i)) != 0;
    }

    public int GetCards(string id)
    {
        // trả về số lượng thẻ đã có (0..9)
        int mask = GetCardMask(id);
        int cnt = 0;
        for (int i = 0; i < 9; i++)
            if ((mask & (1 << i)) != 0) cnt++;
        return cnt;
    }

    // =========================
    // Viewing (PanelCharacterCard)
    // =========================
    public void SetViewing(string id)
    {
        ViewingCharacterId = id;
        // không cần invoke onChanged ở đây
    }

    // =========================
    // Use Character
    // =========================
    public void SetUse(string id)
    {
        if (!IsUnlocked(id)) return;
        PlayerPrefs.SetString(KEY_USE, id);
        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    // =========================
    // Cards Add / Unlock
    // =========================

    /// Add 1 card cụ thể (1..9). Tự unlock khi đủ.
    public void AddCard(string id, int cardIndex01to09)
    {
        var def = GetDef(id);
        if (!def) return;

        // char default luôn unlocked, nhưng vẫn cho lưu mask nếu bạn muốn (không bắt buộc)
        int i = Mathf.Clamp(cardIndex01to09, 1, 9) - 1;

        int mask = GetCardMask(id);
        mask |= (1 << i);
        PlayerPrefs.SetInt(KeyMask(id), mask);

        AutoUnlockIfEnough(id, def);

        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    /// Add N thẻ random (không trùng), clamp theo slot còn thiếu.
    public void AddRandomCards(string id, int count)
    {
        var def = GetDef(id);
        if (!def) return;

        int mask = GetCardMask(id);

        // danh sách slot chưa có
        var remain = new List<int>(9);
        for (int i = 0; i < 9; i++)
            if ((mask & (1 << i)) == 0) remain.Add(i);

        if (remain.Count == 0) return;

        int times = Mathf.Clamp(count, 1, remain.Count);
        for (int k = 0; k < times; k++)
        {
            int pickIdx = UnityEngine.Random.Range(0, remain.Count);
            int slot = remain[pickIdx];
            remain.RemoveAt(pickIdx);
            mask |= (1 << slot);
        }

        PlayerPrefs.SetInt(KeyMask(id), mask);

        AutoUnlockIfEnough(id, def);

        PlayerPrefs.Save();
        onChanged?.Invoke();
    }

    /// Giữ API cũ: +cards (tổng), nhưng map sang random cards.
    /// Ví dụ add=2 => cộng 2 thẻ random chưa có.
    public void AddCards(string id, int add)
    {
        if (add <= 0) return;

        var def = GetDef(id);
        if (!def) return;

        AddRandomCards(id, add);
    }

    void AutoUnlockIfEnough(string id, CharacterDefSO def)
    {
        if (!def) return;

        // char0 luôn unlock
        if (id == GetDefaultIdSafe())
        {
            SetUnlocked(id, true);
            return;
        }

        int need = Mathf.Clamp(def.unlockNeed, 1, 9); // với hệ 9 thẻ thì max 9
        int cur = GetCards(id);

        if (cur >= need)
            SetUnlocked(id, true);
    }

    /// Unlock bằng nút (chỉ unlock khi đủ thẻ). Không auto SetUse.
    public bool TryUnlock(string id)
    {
        var def = GetDef(id);
        if (!def) return false;
        if (IsUnlocked(id)) return false;

        int need = Mathf.Clamp(def.unlockNeed, 1, 9);
        int cur = GetCards(id);

        if (cur < need) return false;

        SetUnlocked(id, true);
        PlayerPrefs.Save();
        onChanged?.Invoke();
        return true;
    }

    // =========================
    // Optional Migration
    // =========================
    /// Nếu trước đây bạn lưu tổng cards (0..9) ở KEY_CARD_PREFIX_LEGACY,
    /// hàm này sẽ chuyển sang mask bằng cách bật ngẫu nhiên N thẻ đầu tiên (hoặc random).
    public void MigrateLegacyTotalsToMaskIfNeeded()
    {
        if (characters == null) return;

        for (int i = 0; i < characters.Count; i++)
        {
            var def = characters[i];
            if (!def) continue;

            // nếu đã có mask rồi thì skip
            if (PlayerPrefs.HasKey(KeyMask(def.id))) continue;

            int legacy = PlayerPrefs.GetInt(KeyLegacyTotal(def.id), 0);
            legacy = Mathf.Clamp(legacy, 0, 9);

            int mask = 0;
            // đơn giản: set 1..legacy
            for (int k = 0; k < legacy; k++)
                mask |= (1 << k);

            PlayerPrefs.SetInt(KeyMask(def.id), mask);
        }

        PlayerPrefs.Save();
    }
}