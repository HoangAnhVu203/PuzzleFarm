using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PanelAds2 : UICanvas
{
    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Text countdownText;

    [Header("Config")]
    [SerializeField] private float duration = 3f;

    Coroutine countdownCR;

    void OnEnable()
    {
        if (countdownCR != null)
            StopCoroutine(countdownCR);

        countdownCR = StartCoroutine(CountdownCR());
    }

    IEnumerator CountdownCR()
    {
        float timeLeft = duration;

        while (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;

            float normalized = Mathf.Clamp01(timeLeft / duration);

            if (fillImage)
                fillImage.fillAmount = normalized;

            if (countdownText)
                countdownText.text = Mathf.CeilToInt(timeLeft).ToString();

            yield return null;
        }

        if (fillImage) fillImage.fillAmount = 0f;
        if (countdownText) countdownText.text = "0";

        ClosePanel();
    }

    void ClosePanel()
    {
        UIManager.Instance.CloseUIDirectly<PanelAds2>();
    }
}