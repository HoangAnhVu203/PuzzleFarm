using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LuckyWheel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform wheel;
    [SerializeField] private Button spinButton;

    [Header("Config")]
    [SerializeField] private int slotCount = 8;
    [SerializeField] private float spinDuration = 4f;
    [SerializeField] private int minRounds = 3;
    [SerializeField] private int maxRounds = 6;

    bool isSpinning;

    public void OnClickSpin()
    {
        if (isSpinning) return;
        StartCoroutine(SpinCR());
    }

    IEnumerator SpinCR()
    {
        isSpinning = true;
        spinButton.interactable = false;

        float anglePerSlot = 360f / slotCount;

        int rewardIndex = Random.Range(0, slotCount);
        int rounds = Random.Range(minRounds, maxRounds);

        float startAngle = wheel.eulerAngles.z;
        float targetAngle = startAngle + 360f * rounds + (360f - rewardIndex * anglePerSlot);

        float t = 0f;

        while (t < spinDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / spinDuration);

            // ease-out cubic (quay nhanh rồi chậm dần)
            float ease = 1f - Mathf.Pow(1f - k, 3f);

            float angle = Mathf.Lerp(startAngle, targetAngle, ease);
            wheel.rotation = Quaternion.Euler(0, 0, angle);

            yield return null;
        }

        wheel.rotation = Quaternion.Euler(0, 0, targetAngle);

        isSpinning = false;
        spinButton.interactable = true;

        Debug.Log("Trúng ô: " + rewardIndex);
        GiveReward(rewardIndex);
    }

    void GiveReward(int index)
    {
        DailyMissionSystem.Instance?.AddProgressById(DailyMissionId.Mission5, 1);
        //TODO: ADD coin
    }
}