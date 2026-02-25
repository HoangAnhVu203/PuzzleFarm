using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HomeNotificationLoop : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject notificationRoot;
    [SerializeField] private RectTransform noteTxt;
    [SerializeField] private CanvasGroup noteCanvasGroup;

    [Header("Name Text")]
    [SerializeField] private Text nameTxt;   
    [SerializeField] private string[] randomNames;

    [Header("Timing")]
    [SerializeField] private float offSeconds = 5f;
    [SerializeField] private float onSeconds = 5f;

    [Header("Anim")]
    [SerializeField] private float enterDuration = 0.6f;
    [SerializeField] private float startOffsetX = 600f;

    private Vector2 targetAnchoredPos;

    Coroutine loopCR;

    void OnEnable()
    {
        targetAnchoredPos = noteTxt.anchoredPosition;
        SetHiddenInstant();
        loopCR = StartCoroutine(LoopCR());
    }

    void OnDisable()
    {
        if (loopCR != null) StopCoroutine(loopCR);
    }

    IEnumerator LoopCR()
    {
        while (true)
        {
            SetHiddenInstant();
            yield return new WaitForSeconds(offSeconds);

            // 🔥 RANDOM NAME ở đây
            SetRandomName();

            SetShownInstantAtStartPos();
            yield return EnterAnimCR();

            yield return new WaitForSeconds(onSeconds);

            SetHiddenInstant();
        }
    }

    void SetRandomName()
    {
        if (nameTxt == null) return;
        if (randomNames == null || randomNames.Length == 0) return;

        int r = Random.Range(0, randomNames.Length);
        nameTxt.text = randomNames[r];
    }

    void SetHiddenInstant()
    {
        notificationRoot.SetActive(false);
        noteCanvasGroup.alpha = 0f;
    }

    void SetShownInstantAtStartPos()
    {
        notificationRoot.SetActive(true);

        noteTxt.anchoredPosition = targetAnchoredPos + new Vector2(startOffsetX, 0f);
        noteCanvasGroup.alpha = 0f;
    }

    IEnumerator EnterAnimCR()
    {
        float t = 0f;

        Vector2 startPos = noteTxt.anchoredPosition;

        while (t < enterDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / enterDuration);
            float e = k * k * (3f - 2f * k); // smoothstep

            noteTxt.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, e);
            noteCanvasGroup.alpha = Mathf.Lerp(0f, 1f, e);

            yield return null;
        }

        noteTxt.anchoredPosition = targetAnchoredPos;
        noteCanvasGroup.alpha = 1f;
    }
}