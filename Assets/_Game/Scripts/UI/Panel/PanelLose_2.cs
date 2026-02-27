using UnityEngine;
using UnityEngine.UI;

public class PanelLose_2 : UICanvas
{
    [Header("UI")]
    [SerializeField] private Image progressFill; 
    [SerializeField] private Text percentText;   

    // Optional: mốc hiển thị
    [SerializeField] private GameObject mark50On;
    [SerializeField] private GameObject mark70On;
    [SerializeField] private GameObject mark100On;

    [Header("Runner Icon")]
    [SerializeField] private RectTransform runnerIcon;
    [SerializeField] private RectTransform markerStart;
    [SerializeField] private RectTransform markerEnd;

    void OnEnable()
    {
        Refresh();
        TryOpenCardIfReached();
    }

    public void Refresh()
    {
        DailyBreakProgress.EnsureReset();

        float p = DailyBreakProgress.GetPercent01();

        if (progressFill) progressFill.fillAmount = p;
        if (percentText)  percentText.text = Mathf.RoundToInt(p * 100f) + "%";

        if (mark50On)  mark50On.SetActive(p >= 0.5f);
        if (mark70On)  mark70On.SetActive(p >= 0.7f);
        if (mark100On) mark100On.SetActive(p >= 1f);

        UpdateRunnerIcon(p);
    }

    void TryOpenCardIfReached()
    {
        // ưu tiên mở 100 trước nếu đã đủ, rồi 70, rồi 50
        if (DailyBreakProgress.CanTrigger100())
        {
            DailyBreakProgress.MarkTriggered100();
            UIManager.Instance.OpenUI<PanelCard>();
            return;
        }

        if (DailyBreakProgress.CanTrigger70())
        {
            DailyBreakProgress.MarkTriggered70();
            UIManager.Instance.OpenUI<PanelCard>();
            return;
        }

        if (DailyBreakProgress.CanTrigger50())
        {
            DailyBreakProgress.MarkTriggered50();
            UIManager.Instance.OpenUI<PanelCard>();
            return;
        }
    }
    
    void UpdateRunnerIcon(float p01)
    {
        if (!runnerIcon || !markerStart || !markerEnd) return;

        // Lerp theo world position để không bị lệch do anchor/layout
        Vector3 a = markerStart.position;
        Vector3 b = markerEnd.position;

        runnerIcon.position = Vector3.LerpUnclamped(a, b, Mathf.Clamp01(p01));
    }

    // gọi từ GameManager khi clear để panel update + check mốc realtime
    public void OnProgressChangedRealtime()
    {
        Refresh();
        TryOpenCardIfReached();
    }

    public void RestartBtn()
    {
        UIManager.Instance.CloseUIDirectly<PanelLose_2>();
        GameManager.Instance?.BtnRetry();
    }

    public void ReturnHomeBtn()
    {
        UIManager.Instance.CloseUIDirectly<PanelLose_2>();
        GameManager.Instance?.ReturnToHome();
    }
}